using AutoMapper;
using DoorCEModel.Infrastructure;
using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEModel.Infrastructure.DataModel.Applications;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEModel.Infrastructure.DataModel.Datasets;
using DoorCEServer.Application.AppGenProxy.Interfaces;
using DoorCEServer.Application.CataloguingManager.Common;
using DoorCEServer.Application.CataloguingManager.Domain;
using DoorCEServer.Application.DataTemplateManager.Common;
using DoorCEServer.Application.DataTemplateManager.Dtos;
using DoorCEServer.Application.DataTemplateManager.Interfaces;
using DoorCEServer.Common.Exceptions;
using DoorCEServer.Infrastructure.DataModel.DataSchemas;
using Microsoft.EntityFrameworkCore;
using DoorCEModel.Utils;
using Microsoft.EntityFrameworkCore.Storage;

namespace DoorCEServer.Application.DataTemplateManager.Domain;

class MAppTemplates(IMapper mapper, ApplicationDbContext dbContext, MCataloguingManagerCommon cataloguing,
    MAgentsCommon agents, MSchemaManagerCommon common, IAppGen generator) : AppTemplateAPI, IAppTemplates
{
    private static readonly SemaphoreSlim TplSemaphore = new (1, 1);
    private static readonly SemaphoreSlim AppSemaphore = new (1, 1);
    
    public IEnumerable<XAppTemplate> GetAppTemplateList(string userId)
    {
        UserAccount account = agents.GetUserAccount(userId)!;
        
        ICollection<AppTemplate> unfilteredTemplates = dbContext.AppTemplates
            .Include(t => t.Schema)
            .Include(t => t.AuxiliaryConcepts)
                .ThenInclude(c => c.Properties)
                    .ThenInclude(p => ((Reference)p).Type)
            .Include(t => t.DataSpecification)
            .Include(t => t.UseCases)
            .Include(t => t.Versions)
            .Include(c => c.EditorshipsLink)
            .Include(c => c.EditorshipsLink.Editors)
            .Include(c => c.EditorshipsLink.EditorRoles)
            .AsSplitQuery()
            .ToList();
        
        List<AppTemplate> filteredTemplates = [];
        // Iterate over all resources and filter them by the user's roles and agent's roles
        foreach (var tmpl in unfilteredTemplates) {
            tmpl.UserRoles = agents.GetUserRoles(tmpl, account);
            if (0 != tmpl.UserRoles!.Count || agents.IsGloballyVisible(tmpl))
                filteredTemplates.Add(tmpl);
        }
        
        return mapper.Map<ICollection<AppTemplate>, ICollection<XAppTemplate>>(filteredTemplates);
    }

    public XAppTemplate GetAppTemplate(string templateUri, string userId)
    {
        AppTemplate? template = dbContext.AppTemplates
            .Include(t => t.Schema)
            .Include(t => t.AuxiliaryConcepts)
                .ThenInclude(c => c.Properties)
                    .ThenInclude(p => ((Reference)p).Type)
            .Include(t => t.DataSpecification)
            .Include(t => t.UseCases)
            .Include(t => t.Versions)
            .Include(c => c.EditorshipsLink)
            .Include(c => c.EditorshipsLink.Editors)
            .Include(c => c.EditorshipsLink.EditorRoles)
            .AsSplitQuery()
            .SingleOrDefault(t => t.Uri == templateUri);

        if (null == template|| !agents.CheckVisibilityAndSetRoles(template, userId))
            throw new NotFoundOrVisibleException($"Template [{templateUri}] not found or not visible to the user");

        XAppTemplate xTemplate = mapper.Map<XAppTemplate>(template);
        return xTemplate;
    }

    public string UpsertAppTemplate(XAppTemplate xTemplate, string userId)
    {
        TplSemaphore.Wait();
        
        bool generatedUri = cataloguing.CheckAndGenerateUri(xTemplate);
        
        AppTemplate requestedVersion = mapper.Map<AppTemplate>(xTemplate);
        
        using var transaction = dbContext.Database.BeginTransaction();
        try {
            UserAccount account = agents.GetUserAccount(userId)!;
            // Get the old version if it exists in the database
            AppTemplate? existingVersion = null;
            if (!generatedUri) existingVersion = dbContext.AppTemplates
                .Include(t => t.Schema)
                .Include(t => t.AuxiliaryConcepts)
                    .ThenInclude(c => c.Properties)
                        .ThenInclude(p => ((Reference)p).Type)
                .Include(t => t.DataSpecification)
                .Include(t => t.UseCases)
                .Include(t => t.Versions)
                .Include(c => c.EditorshipsLink)
                .Include(c => c.EditorshipsLink.Editors)
                .Include(c => c.EditorshipsLink.EditorRoles)
                .Include(c => c.Apps)
                .AsSplitQuery()
                .SingleOrDefault(t => t.Uri == requestedVersion.Uri);
            bool isUpdate = null != existingVersion;
            
            // ===> Authorisation
            agents.CheckAuthorisation(requestedVersion, existingVersion, account);

            if (requestedVersion.HasMetadataRole()) {
                if (string.IsNullOrEmpty(xTemplate.SchemaUri))
                    throw new ArgumentException("App template must have a schema defined");
                
                if (isUpdate && existingVersion!.Apps.Any() && existingVersion.Schema.Uri != xTemplate.SchemaUri)
                    throw new ArgumentException("Cannot change schema of an app template that is already in use by applications");

                DataSchema? schema = dbContext.DataSchemas
                    .Include(s => s.InSeries)
                    .Include(s => s.Concepts)
                    .AsSplitQuery()
                    .SingleOrDefault(s => s.Uri == xTemplate.SchemaUri);
                if (null == schema || !agents.CheckVisibilityAndSetRoles(schema.InSeries, userId))
                    throw new ArgumentException($"App template schema not found or not visible");

                // Map auxiliary concepts when Schema is loaded
                requestedVersion.AuxiliaryConcepts =
                    XConceptToConcept.Convert(xTemplate.AuxiliaryConcepts, schema.Concepts);

                // TODO - prevent circular references
                AppTemplate? versionOf = dbContext.AppTemplates.
                    SingleOrDefault(t => t.Uri == xTemplate.VersionOfUri);
                (existingVersion ?? requestedVersion).VersionOf = versionOf;
                
                //TODO check if anything changed for update
                requestedVersion.IsReady = false;
                
                if (!isUpdate) {
                    dbContext.AppTemplates.Add(requestedVersion);
                    requestedVersion.Schema = schema;
                } else {
                    requestedVersion.Id = existingVersion!.Id; // make sure to preserve the unique ID
                    dbContext.Entry(existingVersion).CurrentValues.SetValues(requestedVersion);
                    dbContext.Entry(existingVersion).State = EntityState.Modified;
                    
                    // Update Use Case Scenarios
                    foreach (UseCaseScenarios uc in existingVersion.UseCases)
                        if (requestedVersion.UseCases.All(ucr => ucr.UseCaseName != uc.UseCaseName))
                            dbContext.Entry(uc).State = EntityState.Deleted;
                    List<UseCaseScenarios> newUCS = new();
                    foreach (UseCaseScenarios uc in requestedVersion.UseCases) {
                        UseCaseScenarios? existingUCS = existingVersion.UseCases
                            .SingleOrDefault(u => u.UseCaseName == uc.UseCaseName);
                        if (null != existingUCS)
                            existingUCS.ScenariosContents = uc.ScenariosContents;
                        else 
                            newUCS.Add(uc);
                    }
                    existingVersion.UseCases = existingVersion.UseCases.Concat(newUCS).ToList();
                    
                    // Update Auxiliary Concepts
                    existingVersion.AuxiliaryConcepts = common.PrepareConceptUpdate(
                        requestedVersion.AuxiliaryConcepts, existingVersion.AuxiliaryConcepts);
                    
                    existingVersion.DataSpecification.Contents = requestedVersion.DataSpecification.Contents;
                    existingVersion.Schema = schema;
                }
            }

            if (requestedVersion.HasOwnershipRole())
                cataloguing.HandleResourceEditors(existingVersion ?? requestedVersion, isUpdate, xTemplate);
            else if (!requestedVersion.HasMetadataRole())
                throw new ArgumentException("No change to the app template was requested");
            
            if (!(existingVersion ?? requestedVersion).Validate(account.Roles.Contains(GlobalRole.DataAdmin)))
                throw new ArgumentException("App template did not pass validation");
            
            dbContext.SaveChanges();
            transaction.Commit();
            TplSemaphore.Release();
            
            return existingVersion?.Uri ?? requestedVersion.Uri; // return the URI of the added or updated template
        } catch (Exception) {
            dbContext.ChangeTracker.Clear(); transaction.Rollback(); TplSemaphore.Release(); throw;
        }
    }

    public void DeleteAppTemplate(string templateUri, string userId)
    {
        using var transaction = dbContext.Database.BeginTransaction();
        try {
            AppTemplate? template = dbContext.AppTemplates
                .Include(t => t.Schema)
                .Include(t => t.AuxiliaryConcepts)
                    .ThenInclude(c => c.Properties)
                        .ThenInclude(p => ((Reference)p).Type)
                .Include(t => t.DataSpecification)
                .Include(t => t.UseCases)
                .Include(t => t.Versions)
                .Include(c => c.EditorshipsLink)
                .Include(c => c.EditorshipsLink.Editors)
                .Include(c => c.EditorshipsLink.EditorRoles)
                .AsSplitQuery()
                .SingleOrDefault(t => t.Uri == templateUri);

            if (null == template|| !agents.CheckVisibilityAndSetRoles(template, userId))
                throw new NotFoundOrVisibleException($"Template [{templateUri}] not found or not visible to the user");
            if (!template.HasMetadataRole())
                throw new UnauthorizedAccessException("User does not have permissions to delete this application");
            
            if (dbContext.AcquisitionApps.Include(a => a.Template)
                .Any(a => a.Template.Id == template.Id))
                throw new InvalidOperationException("Cannot delete template as it is in use by one or more applications");
            
            dbContext.Entry(template).State = EntityState.Deleted;
            dbContext.Entry(template.EditorshipsLink).State = EntityState.Deleted;
            dbContext.SaveChanges();
            transaction.Commit();
        } catch (Exception) { dbContext.ChangeTracker.Clear(); transaction.Rollback(); throw; }
    }
    
    public bool CheckAppTemplateId(string identifier)
    {
        return !dbContext.AppTemplates.Any(d => d.Uri == identifier);
    }

    public string GenerateAppTemplate(string templateUri,  string userId)
    {
        AppTemplate? template = dbContext.AppTemplates
            .Include(c => c.EditorshipsLink)
            .Include(c => c.EditorshipsLink.Editors)
            .Include(c => c.EditorshipsLink.EditorRoles)
            .AsSplitQuery()
            .SingleOrDefault(t => t.Uri == templateUri);
        if (null == template|| !agents.CheckVisibilityAndSetRoles(template, userId))
            throw new NotFoundOrVisibleException($"Template [{templateUri}] not found or not visible to the user");
        if (!template.HasMetadataRole())
            throw new UnauthorizedAccessException("User does not have permissions to generate this template");
        return generator.GenerateCodeFromTemplate(templateUri, "FLT");
    }

    public IEnumerable<XAcquisitionApp> GetAppList(string userId, bool forUsage)
    {
        UserAccount account = agents.GetUserAccount(userId)!;

        ICollection<AcquisitionApp> unfilteredApps = dbContext.AcquisitionApps
            .Include(a => a.ActiveResource)
            .Include(a => a.Template)
            .Include(a => a.SourceResources)
            .Include(c => c.EditorshipsLink)
            .Include(c => c.EditorshipsLink.Editors)
            .Include(c => c.EditorshipsLink.EditorRoles)
            .AsSplitQuery()
            .ToList();

        if (forUsage) {
            ICollection<XAcquisitionApp> xAcquisitionApps = new List<XAcquisitionApp>();

            XAcquisitionApp? xApp;
            foreach (var app in unfilteredApps)
                if (null != (xApp = GetPlatformApp(app, userId)))
                    xAcquisitionApps.Add(xApp);

            return xAcquisitionApps;
        }
        
        List<AcquisitionApp> filteredApps = [];
        // Iterate over all resources and filter them by the user's roles and agent's roles
        foreach (var app in unfilteredApps) {
            app.UserRoles = agents.GetUserRoles(app, account);
            if (0 != app.UserRoles!.Count || agents.IsGloballyVisible(app))
                filteredApps.Add(app);
        }

        return mapper.Map<List<AcquisitionApp>, List<XAcquisitionApp>>(filteredApps);
    }

    public XAcquisitionApp GetApp(string appUri, string userId, bool forUsage)
    {
        AcquisitionApp? app = dbContext.AcquisitionApps
            .Include(a => a.ActiveResource)
            .Include(a => a.Template)
            .Include(a => a.SourceResources)
            .Include(c => c.EditorshipsLink)
            .Include(c => c.EditorshipsLink.Editors)
            .Include(c => c.EditorshipsLink.EditorRoles)
            .AsSplitQuery()
            .SingleOrDefault(a => a.Uri == appUri);

        XAcquisitionApp? xApp = null;
        if (forUsage && null != app && null != (xApp = GetPlatformApp(app, userId, true)))
            return xApp;
        
        if (null == app || !agents.CheckVisibilityAndSetRoles(app, userId) || (forUsage && null == xApp))
            throw new NotFoundOrVisibleException($"Application [{appUri}] not found or not visible to the user");

        xApp = mapper.Map<XAcquisitionApp>(app);
        return xApp;
    }

    public string UpsertApp(XAppCreationRequest xApp, string userId)
    {
        AppSemaphore.Wait();
        
        bool generatedUri = cataloguing.CheckAndGenerateUri(xApp);
        
        AcquisitionApp requestedVersion = mapper.Map<AcquisitionApp>(xApp);
        
        using var transaction = dbContext.Database.BeginTransaction();
        try {
            UserAccount account = agents.GetUserAccount(userId)!;
            
            AcquisitionApp? existingVersion = null;
            // Get the old version if it exists in the database
            if (!generatedUri) existingVersion = dbContext.AcquisitionApps
                .Include(a => a.ActiveResource)
                .Include(a => a.Template)
                .Include(a => a.SourceResources)
                .Include(c => c.EditorshipsLink)
                .Include(c => c.EditorshipsLink.Editors)
                .Include(c => c.EditorshipsLink.EditorRoles)
                .AsSplitQuery()
                .SingleOrDefault(a => a.Uri == requestedVersion.Uri);
            bool isUpdate = null != existingVersion;
            
            // ===> Authorisation
            agents.CheckAuthorisation(requestedVersion, existingVersion, account);

            if (requestedVersion.HasMetadataRole()) {
                if (string.IsNullOrEmpty(xApp.TemplateUri))
                    throw new ArgumentException("Application must have a template defined");
                
                AppTemplate? template = dbContext.AppTemplates
                    .Include(t => t.Schema)
                    .SingleOrDefault(t => t.Uri == xApp.TemplateUri);
                if (null == template || !agents.CheckVisibilityAndSetRoles(template, userId))
                    throw new ArgumentException($"Template not found or not visible");
                
                Dataset? activeResource = dbContext.Datasets
                    .Include(d => d.Schema)
                    .SingleOrDefault(d => d.Uri == xApp.ActiveResourceUri);
                if (null == activeResource || !agents.CheckVisibilityAndSetRoles(activeResource, userId))
                    throw new ArgumentException($"Active resource not found or not visible");
                if (DatasetStatus.Independent == activeResource.Status)
                    throw new ArgumentException($"Active resource [{activeResource.Uri}] is marked as independent and cannot be used in an application");
                if (!activeResource.HasDistributionRole())
                    throw new ArgumentException($"No distribution role for active resource");
                if (activeResource.Schema?.Uri != template.Schema.Uri)
                    throw new ArgumentException("Active resource schema does not match template schema");
                (existingVersion ?? requestedVersion).ActiveResource = activeResource;
                
                if (!xApp.SourceResourceUris.Any() && !xApp.NewResources.Any())
                    throw new ArgumentException("App must have at least one source resource");
                
                //TODO expand to resources
                IEnumerable<DataResource> sourceResources = dbContext.Datasets
                    .Include(d => d.Schema)
                    .Include(d => d.Target)
                    .Where(d => xApp.SourceResourceUris.Contains(d.Uri))
                    .ToList();
                
                if (sourceResources.Any(d => d.Uri == activeResource.Uri))
                    throw new ArgumentException("Active resource cannot be also a source resource");
                if (sourceResources.Any(d => d is Dataset ds && DatasetStatus.Source != ds.Status))
                    throw new ArgumentException("One or more source resources is not marked as source dataset");
                if (sourceResources.Any(d => 
                        d is Dataset ds && ds.Schema?.Uri != template.Schema.Uri))
                    throw new ArgumentException("One or more source resources have schema not matching template schema");

                foreach (Dataset resource in sourceResources.OfType<Dataset>()) {
                    if (null == resource.Target) resource.Target = activeResource;
                    else if (resource.Target.Uri != activeResource.Uri)
                        throw new ArgumentException("Resource (" + resource.Uri + 
                                                    ") must have the app active resource set as target to be used as source");
                }
                
                // Add newly created source datasets to the source dataset list
                (existingVersion ?? requestedVersion).SourceResources = sourceResources.Concat(
                    CreateSourceDatasets(xApp, activeResource, template.Language, userId)).ToList();
                
                if (!isUpdate) {
                    dbContext.AcquisitionApps.Add(requestedVersion);
                    requestedVersion.Template = template;
                } else {
                    requestedVersion.Id = existingVersion!.Id; // make sure to preserve the unique ID
                    dbContext.Entry(existingVersion).CurrentValues.SetValues(requestedVersion);
                    dbContext.Entry(existingVersion).State = EntityState.Modified;
                    existingVersion.Template = template;
                }
            }

            if (requestedVersion.HasOwnershipRole())
                cataloguing.HandleResourceEditors(existingVersion ?? requestedVersion, isUpdate, xApp);
            else if (!requestedVersion.HasMetadataRole())
                throw new ArgumentException("No change to the catalogue was requested");
            
            if (!(existingVersion ?? requestedVersion).Validate(account.Roles.Contains(GlobalRole.DataAdmin)))
                throw new ArgumentException("Catalogue did not pass validation");
            
            dbContext.SaveChanges();
            transaction.Commit();
            AppSemaphore.Release();
            
            return existingVersion?.Uri ?? requestedVersion.Uri; // return the URI of the added or updated application
        } catch (Exception) {
            dbContext.ChangeTracker.Clear(); transaction.Rollback(); AppSemaphore.Release(); throw;
        }
    }

    private IEnumerable<Dataset> CreateSourceDatasets(XAppCreationRequest xApp, Dataset activeResource,
        string language, string userId)
    {
        List<Dataset> newDatasets = [];
        if (!xApp.NewResources.Any()) return newDatasets;
        
        Catalogue? catalogue = null != xApp.SourceCatalogueUri ? dbContext.Catalogues
            .SingleOrDefault(c => c.Uri == xApp.SourceCatalogueUri) : null;
        if (null == catalogue || !agents.CheckVisibilityAndSetRoles(catalogue, userId))
            throw new ArgumentException("Source catalogue not found or not visible");
        
        foreach (var scr in xApp.NewResources)
            newDatasets.Add(CreateSourceDataset(scr, activeResource, catalogue, language, userId));
        return newDatasets;
    }
    
    private Dataset CreateSourceDataset(XSourceCreationRequest scr, Dataset activeResource, Catalogue catalogue,
        string language, string userId)
    {
        // Create new source dataset based on the creation request and the active resource
        Dataset newDataset = new Dataset
        {
            Type = new List<DatasetType>(activeResource.Type),
            Version = "0.01",
            Uri = cataloguing.GenerateUri("dat", scr.SourceDatasetName, dbContext.Datasets),
            EditorshipsLink = new ResourceEditorshipsLink(),
            Title = new Dictionary<string, string>() { { language, scr.SourceDatasetName } },
            Description = new Dictionary<string, string>() {
                {
                    "en", "Dataset created as part of application source resources based on " + 
                          $"dataset: {(activeResource.Title.ContainsKey("en") ? 
                              activeResource.Title["en"] : activeResource.Title.First().Value)}"
                }
            },
            AccessRights = AccessRightsType.Restricted,
            Status = DatasetStatus.Source,
            Catalogue = catalogue,
            Schema = activeResource.Schema
        };
        
        // Assign distribution editors to the dataset, based on the agent URIs in the creation request
        bool distributorsContainsOwner = false;
        foreach (string au in scr.AgentUris)
        {
            Agent? agent = dbContext.Agents
                .Include(a => a.EditorRoles)
                .Include(person => ((Person)person).Account)
                .SingleOrDefault(a => a.Uri == au);
            if (null == agent)
                throw new ArgumentException($"Agent URI {au} not found");
            Editorship editorship = new Editorship()
            {
                Roles = new List<EditorRole> { EditorRole.DistributionEditor },
                ResourceLink = newDataset.EditorshipsLink
            };
            // If the distributor is also an owner, assign full roles
            if (agent is Person p && p.Account?.UserId == userId) {
                editorship.Roles = new List<EditorRole>() {
                    EditorRole.OwnershipEditor, EditorRole.MetadataEditor, EditorRole.DistributionEditor
                };
                distributorsContainsOwner = true;
            }
            agent.EditorRoles.Add(editorship);
        }

        // If the owner is not among requested dataset distributors, assign ownership and metadata roles
        Person? owner;
        if (!distributorsContainsOwner && null != (owner = agents.GetUserAccount(userId)!.Person)) {
            Editorship ownerEditorship = new Editorship() {
                Roles = new List<EditorRole> { EditorRole.OwnershipEditor, EditorRole.MetadataEditor },
                ResourceLink = newDataset.EditorshipsLink
            };
            dbContext.Entry(owner).Collection(p => p.EditorRoles).Load();
            owner.EditorRoles.Add(ownerEditorship);
        }
        
        dbContext.Datasets.Add(newDataset);
        return newDataset;
    }

    public void DeleteApp(string appUri, string userId)
    {
        using var transaction = dbContext.Database.BeginTransaction();
        try {
            AcquisitionApp? app = dbContext.AcquisitionApps
                .Include(a => a.ActiveResource)
                .Include(a => a.Template)
                .Include(a => a.SourceResources)
                .Include(c => c.EditorshipsLink)
                .Include(c => c.EditorshipsLink.Editors)
                .Include(c => c.EditorshipsLink.EditorRoles)
                .AsSplitQuery()
                .SingleOrDefault(a => a.Uri == appUri);
        
            if (null == app || !agents.CheckVisibilityAndSetRoles(app, userId))
                throw new NotFoundOrVisibleException($"Application [{appUri}] not found or not visible to the user");
            if (!app.HasMetadataRole())
                throw new UnauthorizedAccessException("User does not have permissions to delete this application");
            
            dbContext.Entry(app).State = EntityState.Deleted;
            dbContext.Entry(app.EditorshipsLink).State = EntityState.Deleted;
            dbContext.SaveChanges();
            transaction.Commit();
        } catch (Exception) { dbContext.ChangeTracker.Clear(); transaction.Rollback(); throw; }
    }
    
    private XAcquisitionApp? GetPlatformApp(AcquisitionApp app, string userId, bool ignoreVisibility = false)
    {
        if (!app.IsVisible && !ignoreVisibility) return null;
        ICollection<DataResource> filteredDatasets = app.SourceResources
            .Where(d => agents.CheckVisibilityAndSetRoles(d, userId) && d.HasDistributionRole())
            .ToList();
        if (filteredDatasets.Count == 0) return null;
        XAcquisitionApp xApp = mapper.Map<XAcquisitionApp>(app);
        xApp.SourceResourceUris = filteredDatasets.Select(e => e.Uri);
        xApp.SourceResourceTitles = new Dictionary<string, Dictionary<string,string>>(
            filteredDatasets.Select(d =>
                new KeyValuePair<string, Dictionary<string,string>>(d.Uri, d.Title)));
        xApp.EditorsRoles = new();
        xApp.EditorsUris = new List<string>();
        if (!agents.CheckVisibilityAndSetRoles(app.ActiveResource, userId)) {
            xApp.ActiveResourceTitle = null;
            xApp.ActiveResourceUri = null;
        }
        if (!agents.CheckVisibilityAndSetRoles(app.Template, userId)) {
            xApp.TemplateTitle = null;
            xApp.TemplateUri = null;
        }
        return xApp;
    }
    
    public bool CheckAppId(string identifier)
    {
        return !dbContext.AcquisitionApps.Any(d => d.Uri == identifier);
    }

    public void DeployApp(string appUri, string userId)
    {
        using var transaction = dbContext.Database.BeginTransaction();
        try {
            AcquisitionApp? app = dbContext.AcquisitionApps
                .Include(a => a.ActiveResource)
                .Include(a => a.Template)
                .Include(a => a.SourceResources)
                .Include(c => c.EditorshipsLink)
                .Include(c => c.EditorshipsLink.Editors)
                .Include(c => c.EditorshipsLink.EditorRoles)
                .AsSplitQuery()
                .SingleOrDefault(a => a.Uri == appUri);

            if (null == app || !agents.CheckVisibilityAndSetRoles(app, userId))
                throw new NotFoundOrVisibleException($"Application [{appUri}] not found or not visible to the user");
            if (!app.HasMetadataRole())
                throw new UnauthorizedAccessException("User does not have permissions to generate this template");
            if (!app.Template.IsReady)
                throw new InvalidOperationException("Application template is not ready for deployment");
            generator.DeployApp(app);
            app.Status = AppStatus.InProcessing;
            dbContext.SaveChanges();
            transaction.Commit();
        } catch (Exception) { dbContext.ChangeTracker.Clear(); transaction.Rollback(); throw; }
    }

    public XDefaultAppTemplate GetDefaultAppTemplate(string schemaId, string? conceptName, string? language)
    {
        DataSchema? schema = dbContext.DataSchemas
            .Include(d => d.Concepts)
            .AsSplitQuery()
            .SingleOrDefault(d => d.Uri == schemaId);
        if (null == schema) throw new ArgumentException($"Schema [{schemaId}] not found");
        if (null != conceptName) {
            Concept? concept = schema.Concepts.SingleOrDefault(c => c.Name == conceptName);
            if (null == concept) throw new ArgumentException($"Concept [{conceptName}] not found");    
        } else if (null != schema.MainConcept) conceptName = schema.MainConcept.Name;
        else
            throw new ArgumentException($"No main concept defined for schema [{schemaId}]");
        return XDefaultAppTemplateFactory.Get(conceptName, language ?? "en");
    }

    public IEnumerable<XAcquisitionApp> GetDatasetApps(string datasetUri, string userId)
    {
        CheckDatasetDistributionRole(datasetUri, userId);

        ICollection<AcquisitionApp> apps = dbContext.AcquisitionApps
            .Include(a => a.ActiveResource)
            .Include(a => a.Template)
            .Include(a => a.SourceResources)
            .Include(c => c.EditorshipsLink)
            .Include(c => c.EditorshipsLink.Editors)
            .Include(c => c.EditorshipsLink.EditorRoles)
            .AsSplitQuery()
            .Where(a => a.SourceResources.Any(d => d.Uri == datasetUri))
            .ToList();

        List<XAcquisitionApp> mappedApps = [];
        foreach (AcquisitionApp app in apps) {
            XAcquisitionApp xApp = mapper.Map<AcquisitionApp, XAcquisitionApp>(app);
            if (!agents.CheckVisibilityAndSetRoles(app.ActiveResource, userId)) {
                xApp.ActiveResourceTitle = null;
                xApp.ActiveResourceUri = null;
            }
            if (!agents.CheckVisibilityAndSetRoles(app.Template, userId)) {
                xApp.TemplateTitle = null;
                xApp.TemplateUri = null;
            }
            mappedApps.Add(xApp);
        }

        return mappedApps;
    }

    public XAcquisitionApp? GetDefaultDatasetApp(string datasetUri, string userId)
    {
        CheckDatasetDistributionRole(datasetUri, userId);

        AcquisitionApp? app = dbContext.AcquisitionApps
            .Include(a => a.ActiveResource)
            .Include(a => a.Template)
            .Include(a => a.SourceResources)
            .Include(c => c.EditorshipsLink)
            .Include(c => c.EditorshipsLink.Editors)
            .Include(c => c.EditorshipsLink.EditorRoles)
            .AsSplitQuery()
            .SingleOrDefault(a => 1 == a.SourceResources.Count() 
                                  && a.SourceResources.First().Uri == datasetUri
                                  && a.ActiveResource.Uri == datasetUri); // This is a default app because SourceResource (one only) == ActiveResource
        if (null == app) return null;
        
        XAcquisitionApp xApp = mapper.Map<XAcquisitionApp>(app);
        if (!agents.CheckVisibilityAndSetRoles(app.Template, userId)) {
            xApp.TemplateTitle = null;
            xApp.TemplateUri = null;
        }

        return xApp;
    }

    private void CheckDatasetDistributionRole(string datasetUri, string userId)
    {
        Dataset? dataset = dbContext.Datasets
            .Include(c => c.EditorshipsLink)
            .Include(c => c.EditorshipsLink.Editors)
            .Include(c => c.EditorshipsLink.EditorRoles)
            .AsSplitQuery()
            .SingleOrDefault(d => d.Uri == datasetUri);
        
        // ===> Authorisation
        if (null == dataset || !agents.CheckVisibilityAndSetRoles(dataset, userId))
            throw new NotFoundOrVisibleException($"Dataset [{datasetUri}] not found or not visible to the user");

        if (!dataset.HasDistributionRole())
            throw new ArgumentException($"The user has no rights to run applications for dataset [{datasetUri}] ");

    }
    
    // ************ Methods for IAppTemplates ********************************************
    // ***********************************************************************************

    public AppTemplate CreateDefaultAppTemplate(Dataset dataset, string language = "en")
    {
        if (null == dataset.Schema) 
            throw new ArgumentException($"Dataset [{dataset.Uri}] has no schema defined: cannot create default app");
        dbContext.Entry(dataset.Schema).Collection(s => s.Concepts).Load();
        Concept concept = dataset.Schema.MainConcept ?? (1 == dataset.Schema.Concepts.Count ? dataset.Schema.Concepts.First() : 
                          throw new ArgumentException($"Dataset [{dataset.Uri}] schema has no main concept defined: cannot create default app"));
        
        AppTemplate? existingDefaultTemplate = dbContext.AppTemplates
            .Include(t => t.Apps)
            .ThenInclude(a => a.SourceResources)
            .Include(t => t.Apps)
            .ThenInclude(a => a.ActiveResource)
            .AsSplitQuery()
            .Where(t => t.Schema.Uri == dataset.Schema.Uri).ToList()
            .SingleOrDefault(t => t.IsDefault());
        if (null != existingDefaultTemplate) return existingDefaultTemplate; // Already exists, return it without creating a new one!
        
        XDefaultAppTemplate defaultAppTemplate = XDefaultAppTemplateFactory.Get(concept.Name, "en", false);

        XAppTemplate xTemplate = new XAppTemplate {
                Title = "Default App Template for schema " + dataset.Schema.Title,
                Description = "Automatically created during dataset creation",
                Language = language,
                IsReady = false,
                SchemaUri = dataset.Schema.Uri,
            };

        try {
            TplSemaphore.Wait(); // Just in case, we don't want to have duplicate template Uris

            cataloguing.CheckAndGenerateUri(xTemplate);
            
            dbContext.Entry(dataset.Schema).Reference(s => s.InSeries).Load();
            dbContext.Entry(dataset.Schema.InSeries).Reference(s => s.EditorshipsLink).Load();
            dbContext.Entry(dataset.Schema.InSeries.EditorshipsLink).Collection(e => e.Editors).Load();
            dbContext.Entry(dataset.Schema.InSeries.EditorshipsLink).Collection(e => e.EditorRoles).Load();

            AppTemplate appTemplate = new AppTemplate
            {
                Uri = xTemplate.Uri!,
                Title = xTemplate.Title,
                Description = xTemplate.Description,
                Schema = dataset.Schema,
                UseCases = defaultAppTemplate.UseCaseScenarios.Select(ucs => new UseCaseScenarios()
                {
                    UseCaseName = ucs.Key,
                    ScenariosContents = ucs.Value
                }).ToList(),
                AuxiliaryConcepts = XConceptToConcept.Convert(defaultAppTemplate.AuxiliaryConcepts, dataset.Schema.Concepts),
                EditorshipsLink = CopyResourceEditorshipsLink(dataset.Schema.InSeries.EditorshipsLink),
                DataSpecification = new AppDataSpecification { Contents = "" }
            };
            dbContext.AppTemplates.Add(appTemplate);
            dbContext.Entry(appTemplate).Reference(r => r.EditorshipsLink).Load();
            dbContext.Entry(appTemplate.EditorshipsLink).Collection(r => r.Editors).Load();
            dbContext.SaveChanges();
            return appTemplate;
        } finally {
            TplSemaphore.Release();
        }
    }

    public string DeployDefaultApp(Dataset dataset, AppTemplate template)
    {
        string packageUri = generator.GenerateCodeFromTemplate(template.Uri, "FLT");

        XAcquisitionApp xApp = new XAcquisitionApp {
            Title = "Default App for " + IdentifiableElementUtils.SelectByLanguage(dataset.Title, "en"),
            Description = "Automatically created during dataset creation",
            TemplateUri = template.Uri,
            ActiveResourceUri = dataset.Uri,
            SourceResourceUris = new List<string> { dataset.Uri },
            IsVisible = false,
            IsEnabled = true
        };

        AcquisitionApp app;

        using IDbContextTransaction transaction = dbContext.Database.BeginTransaction();
        try {
            AppSemaphore.Wait(); // Just in case, we don't want to have duplicate app Uris
            cataloguing.CheckAndGenerateUri(xApp);

            app = new AcquisitionApp {
                Uri = xApp.Uri!,
                EditorshipsLink = CopyResourceEditorshipsLink(dataset.EditorshipsLink),
                Title = xApp.Title,
                Description = xApp.Description,
                Template = template,
                ActiveResource = dataset,
                SourceResources = new List<DataResource> { dataset },
                IsVisible = false,
                IsEnabled = true
            };

            dbContext.AcquisitionApps.Add(app);
            dbContext.SaveChanges();
            transaction.Commit();
        } catch {
            dbContext.ChangeTracker.Clear(); transaction.Rollback();
            throw;
        } finally {
            AppSemaphore.Release();
        }

        generator.DeployApp(app);
        
        return app.Uri;
    }

    ResourceEditorshipsLink CopyResourceEditorshipsLink(ResourceEditorshipsLink link)
    {
        ResourceEditorshipsLink appEditorships = new ResourceEditorshipsLink();
        appEditorships.EditorRoles = link.EditorRoles
            .Select(er => new Editorship
            {
                Agent = er.Agent, // same agent reference
                Roles = new List<EditorRole>(er.Roles), // copy role values
                ResourceLink = appEditorships // new link for new resource
            })
            .ToList();
        return appEditorships;
    }
}

using AutoMapper;
using DoorCEModel.Infrastructure;
using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEModel.Infrastructure.DataModel.Applications;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEModel.Infrastructure.DataModel.Datasets;
using DoorCEServer.Application.CataloguingManager.Common;
using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Application.CataloguingManager.Interfaces;
using DoorCEServer.Application.CkanProxy.Common;
using DoorCEServer.Application.CkanProxy.Dtos;
using DoorCEServer.Application.CkanProxy.Interfaces;
using DoorCEServer.Application.DataTemplateManager.Interfaces;
using DoorCEServer.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DoorCEServer.Application.CataloguingManager.Domain;

public class MDatasetManagement(IMapper mapper, ApplicationDbContext context, 
    MCataloguingManagerCommon common, MAgentsCommon agents,
    ICkanActions ckanActions, IAppTemplates appTemplates) : DatasetManagementAPI
{
    private static readonly SemaphoreSlim CatSemaphore = new(1, 1);
    private static readonly SemaphoreSlim DtsSemaphore = new(1, 1);
    private static readonly SemaphoreSlim SrsSemaphore = new(1, 1);
    
    public IEnumerable<XCatalogueElement> GetAllowedCatalogues(string? resourceId, string userId)
    {
        ICollection<OwnableResource> resources =
            GetAgentResources(userId, [ResourceType.Catalogue], [EditorRole.MetadataEditor]);
        
        if (!string.IsNullOrEmpty(resourceId)) {
            // TODO - include ResponsiblePerson and ResponsibleOrganisation if ever needed
            OwnableResource? resource = context.OwnableResources
                .Include(r => ((Catalogue)r).PartOf)
                .Include(r => ((CataloguedResource)r).Catalogue)
                .AsSplitQuery()
                .SingleOrDefault(r => r.Uri == resourceId);
            if (null == resource || !agents.CheckVisibilityAndSetRoles(resource, userId))
                throw new ArgumentException($"Resource [{resourceId}] not found or not visible to the user");

            if (resource is Catalogue cat)
                resources = resources
                    .Where(r => !common.IsSameOrChildCatalogue((Catalogue)r, cat)).ToList();

            Catalogue? parent = resource.GetParent();
            if (null != parent && resources.All(r => r.Uri != parent.Uri)) resources.Add(parent);
        }

        ICollection<XCatalogueElement> xResources =
            mapper.Map<ICollection<OwnableResource>, ICollection<XCatalogueElement>>(resources);
        return xResources;
    }

    public IEnumerable<XCatalogueElement> GetAllowedSeries(string? datasetId, string userId)
    {
        ICollection<OwnableResource> resources =
            GetAgentResources(userId, [ResourceType.Series], [EditorRole.MetadataEditor]);

        if (!string.IsNullOrEmpty(datasetId)) {
            // TODO - include ResponsiblePerson and ResponsibleOrganisation if ever needed
            Dataset? dataset = context.Datasets
                .Include(d => d.Series)
                .AsSplitQuery()
                .SingleOrDefault(d => d.Uri == datasetId);
            if (null == dataset || !agents.CheckVisibilityAndSetRoles(dataset, userId))
                throw new ArgumentException($"Dataset [{datasetId}] not found or not visible to the user");

            // Add the series of the dataset if not already in the list
            foreach (DatasetSeries series in dataset.Series)
                if (resources.All(r => r.Uri != series.Uri))
                    resources.Add(series);
        }

        ICollection<XCatalogueElement> xResources =
            mapper.Map<ICollection<OwnableResource>, ICollection<XCatalogueElement>>(resources);
        return xResources;
    }

    public IEnumerable<XCatalogueElement> GetAllowedDatasets(string? distributionId, string userId)
    {
        ICollection<OwnableResource> resources =
            GetAgentResources(userId, [ResourceType.Dataset], [EditorRole.DistributionEditor]);
        
        if (!string.IsNullOrEmpty(distributionId)) {
            // TODO - include ResponsiblePerson and ResponsibleOrganisation if ever needed
            Distribution? distribution = context.Distributions
                .Include(d => d.Dataset)
                .Include(d => d.DataService)
                .AsSplitQuery()
                .SingleOrDefault(d => d.Uri == distributionId);
            if (null == distribution || !agents.CheckVisibilityAndSetRoles(distribution.Dataset, userId)
                && (null == distribution.DataService ||
                    !agents.CheckVisibilityAndSetRoles(distribution.DataService, userId)))
                throw new ArgumentException($"Distribution [{distributionId}] not found or not visible to the user");

            Dataset parent = distribution.Dataset;
            if (resources.All(r => r.Uri != parent.Uri)) resources.Add(parent);
        }

        ICollection<XCatalogueElement> xResources =
            mapper.Map<ICollection<OwnableResource>, ICollection<XCatalogueElement>>(resources);
        return xResources;
    }

    public IEnumerable<XCatalogueElement> GetAllowedServices(string? distributionId, string userId)
    {
        ICollection<OwnableResource> resources =
            GetAgentResources(userId, [ResourceType.Service], [EditorRole.DistributionEditor]);
        
        if (!string.IsNullOrEmpty(distributionId)) {
            // TODO - include ResponsiblePerson and ResponsibleOrganisation if ever needed
            Distribution? distribution = context.Distributions
                .Include(d => d.Dataset)
                .Include(d => d.DataService)
                .AsSplitQuery()
                .SingleOrDefault(d => d.Uri == distributionId);
            if (null == distribution || !agents.CheckVisibilityAndSetRoles(distribution.Dataset, userId)
                && (null == distribution.DataService ||
                    !agents.CheckVisibilityAndSetRoles(distribution.DataService, userId)))
                throw new ArgumentException($"Distribution [{distributionId}] not found or not visible to the user");

            DataService? parent = distribution.DataService;
            if (null != parent && resources.All(r => r.Uri != parent.Uri)) resources.Add(parent);
        }

        ICollection<XCatalogueElement> xResources =
            mapper.Map<ICollection<OwnableResource>, ICollection<XCatalogueElement>>(resources);
        return xResources;
    }

    public IEnumerable<XCatalogueElement> GetSchemaDatasets(string? schemaUri, bool onlySourceDatasets, string? userId)
    {
        UserAccount? account = agents.GetUserAccount(userId);
        
        List<Dataset> unfilteredResources = context.Datasets
            .Include(d => d.ResponsiblePerson)
            .Include(d => d.ResponsibleOrganisation)
            .Include(d => d.EditorshipsLink)
            .Include(d => d.EditorshipsLink.Editors)
            .Include(d => d.EditorshipsLink.EditorRoles)
            .Include(d => d.Series)
            .Include(d => d.Schema)
            .Include(d => d.Catalogue)
            .AsSplitQuery()
            .Where(d => null!= d.Schema && (string.IsNullOrEmpty(schemaUri) || d.Schema.Uri == schemaUri) 
                        && (!onlySourceDatasets || DatasetStatus.Source == d.Status))
            .ToList();
        
        List<Dataset> filteredResources = [];
        // Iterate over all resources and filter them by the user's roles and agent's roles
        foreach (var resource in unfilteredResources) {
            resource.UserRoles = agents.GetUserRoles(resource, account);
            if (0 != resource.UserRoles!.Count || agents.IsGloballyVisible(resource))
                filteredResources.Add(resource);
        }
        ICollection<XCatalogueElement> xResources =
            mapper.Map<ICollection<Dataset>, ICollection<XCatalogueElement>>(filteredResources);
        return xResources;
    }

    public XDataResources GetManagedDataResources(string userId)
    {
        var datasets = GetAgentResources(userId, [ResourceType.Dataset],
            [EditorRole.DistributionEditor]);
        var series = GetAgentResources(userId, [ResourceType.Series],
            [EditorRole.DistributionEditor]);
        return new XDataResources {
            Datasets = mapper.Map<ICollection<OwnableResource>, ICollection<XCatalogueElement>>(datasets),
            Series = mapper.Map<ICollection<OwnableResource>, ICollection<XCatalogueElement>>(series)
        };
    }
    
    // ==== Catalogue management ========================================================================
    
    public XCatalogue GetCatalogue(string identifier, string? userId)
    {
        Catalogue? catalogue = context.Catalogues
            .Include(c => c.PartOf)
            .Include(c => c.ResponsiblePerson)
            .Include(c => c.ResponsibleOrganisation)
            .Include(c => c.Contacts)
            .Include(c => c.EditorshipsLink)
            .Include(c => c.EditorshipsLink.Editors)
            .Include((c => c.EditorshipsLink.EditorRoles))
            .AsSplitQuery()
            .SingleOrDefault(c => c.Uri == identifier);
        if (null == catalogue) throw new NotFoundOrVisibleException($"Catalogue [{identifier}] not found");
        
        // ===> Authorisation - just set roles (catalogue is always visible)
        agents.SetUserRoles(catalogue, userId);
        
        XCatalogue xCatalogue = mapper.Map<XCatalogue>(catalogue);
        return xCatalogue;
    }

    public bool CheckCatalogueId(string identifier)
    {
        return !context.Catalogues.Any(c => c.Uri == identifier);
    }
    
    public string UpsertCatalogue(XCatalogue xCatalogue, IEnumerable<XContactData> newContacts, string userId)
    {
        // Set a semaphore to prevent from multiple threads to generate the same URI
        CatSemaphore.Wait();
        
        // Generate unique identifier if needed
        bool generatedUri = common.CheckAndGenerateUri(xCatalogue);
            
        Catalogue requestedVersion = mapper.Map<Catalogue>(xCatalogue);

        using var transaction = context.Database.BeginTransaction();
        try {
            UserAccount account = agents.GetUserAccount(userId)!;
            
            Catalogue? existingVersion = null;
            // Get the old version if it exists in the database
            if (!generatedUri) existingVersion = context.Catalogues
                .Include(c => c.PartOf)
                .Include(c => c.EditorshipsLink)
                .Include(c => c.EditorshipsLink.Editors)
                .Include(c => c.EditorshipsLink.EditorRoles)
                .Include(c => c.ResponsibleOrganisation)
                .Include(c => c.ResponsiblePerson)
                .Include(c => c.ResponsiblePerson!.Contacts)
                .Include(d => d.Contacts).ThenInclude(c => c.Agent)
                .AsSplitQuery()
                .SingleOrDefault(c => c.Uri == requestedVersion.Uri);
            bool isUpdate = null != existingVersion;
            
            // ===> Authorisation
            agents.CheckAuthorisation(requestedVersion, existingVersion, account);
            
            if (requestedVersion.HasMetadataRole()) {
                // Get the (updated) parent catalogue (if exists) and determine which roles the user has for it
                Catalogue? designatedParent = null;
                if (!string.IsNullOrEmpty(xCatalogue.PartOfUri)) {
                    designatedParent = context.Catalogues
                        .SingleOrDefault(c => c.Uri == xCatalogue.PartOfUri);
                    if (null == designatedParent) throw new Exception("Parent catalogue not found");
                    if (isUpdate && common.IsSameOrChildCatalogue(designatedParent, existingVersion!))
                        throw new ArgumentException("Cannot set the parent catalogue to itself or one of its children");
                    // set the user roles for the designated parent catalogue
                    designatedParent.UserRoles = agents.GetUserRoles(designatedParent, account);
                }
            
                // Prevent from inserting a new catalogue into the parent catalogue if the user does not have the right roles
                if (!isUpdate || existingVersion!.PartOf?.Uri != designatedParent?.Uri){ // parent is to be changed?
                    if (null == designatedParent) {
                        if (!account.Roles.Contains(GlobalRole.DataAdmin))
                            throw new ArgumentException("User not permitted to set this catalogue as root catalogue");
                    } else if (!designatedParent.HasMetadataRole())
                        throw new UnauthorizedAccessException($"User not permitted to add a new catalogue in this catalogue [{xCatalogue.Uri}]");
                }
                
                // Update contact data for this catalogue
                common.HandleResourceContacts(existingVersion ?? requestedVersion, isUpdate, xCatalogue, newContacts);
                
                common.CheckIcon(xCatalogue);
                
                if (!isUpdate) {
                    // Old version does not exist? - Add the new catalogue and insert to the parent catalogue (if needed)
                    context.Catalogues.Add(requestedVersion);
                    if (null != designatedParent) designatedParent.Parts.Add(requestedVersion);
                } else {
                    // Otherwise - Update the old catalogue with new data (possibly change the parent catalogue)
                    requestedVersion.Id = existingVersion!.Id; // make sure to preserve the unique ID
                    context.Entry(existingVersion).CurrentValues.SetValues(requestedVersion);
                    context.Entry(existingVersion).State = EntityState.Modified;
                    if (null != designatedParent) designatedParent.Parts.Add(existingVersion);
                    else if (null == xCatalogue.PartOfUri)
                        existingVersion.PartOf = null;
                }
            }

            if (requestedVersion.HasOwnershipRole())
                common.HandleResourceEditors(existingVersion ?? requestedVersion, isUpdate, xCatalogue);
            else if (!requestedVersion.HasMetadataRole())
                throw new ArgumentException("No change to the catalogue was requested");
            
            if (!(existingVersion ?? requestedVersion).Validate(account.Roles.Contains(GlobalRole.DataAdmin)))
                throw new ArgumentException("Catalogue did not pass validation");
            
            context.SaveChanges();
            transaction.Commit();
            CatSemaphore.Release();
            
            return existingVersion?.Uri ?? requestedVersion.Uri; // return the URI of the added or updated catalogue
        } catch (Exception) {
            context.ChangeTracker.Clear(); transaction.Rollback(); CatSemaphore.Release(); throw;
        }
    }

    public void DeleteCatalogue(string identifier, string userId)
    {
        using IDbContextTransaction transaction = context.Database.BeginTransaction();
        try {
            // To delete, make sure that all the related objects are loaded (included into the context)
            Catalogue? catalogue = context.Catalogues
                .Include(c => c.PartOf)
                .Include(c => c.Resources)
                .Include(c => c.Parts)
                .Include(c => c.EditorshipsLink)
                .Include(c => c.EditorshipsLink.Editors)
                .Include(c => c.EditorshipsLink.EditorRoles)
                .Include(c => c.Contacts).ThenInclude(c => c.Agent)
                .AsSplitQuery()
                .SingleOrDefault(c => c.Uri == identifier);
            
            // ===> Authorisation
            if (null == catalogue || !agents.CheckVisibilityAndSetRoles(catalogue, userId))
                throw new ArgumentException("Catalogue not found or not visible to the user");
            if (null != catalogue.PartOf) {
                agents.SetUserRoles(catalogue.PartOf, userId);
                if (!catalogue.PartOf.HasMetadataRole())
                    throw new UnauthorizedAccessException("User not permitted to delete the catalogue");
            } else if (!agents.GetUserAccount(userId)!.Roles.Contains(GlobalRole.DataAdmin))
                throw new UnauthorizedAccessException("User not permitted to delete the root catalogue");
            
            // Cannot delete if it contains some resources or catalogues
            if (0 != catalogue.Resources.Count || 0 != catalogue.Parts.Count)
                throw new InvalidOperationException("Cannot delete the catalogue with resources or sub-catalogues");
            
            // When deleting the catalogue, also all the related "orphaned" (without an Agent) contacts need to be removed 
            foreach (ContactData contactData in catalogue.Contacts)
                if (null == contactData.Agent)
                    context.Entry(contactData).State = EntityState.Deleted;
            
            context.Entry(catalogue).State = EntityState.Deleted;
            context.Entry(catalogue.EditorshipsLink).State = EntityState.Deleted;
            context.SaveChanges();
            transaction.Commit();
        } catch (Exception) { context.ChangeTracker.Clear(); transaction.Rollback(); throw; }
    }

    public IEnumerable<XCatalogueElement> GetCatalogueContents(string identifier, string? userId,
        XCatalogueElementType? type = null)
    {
        Catalogue? catalogue = context.Catalogues
            .Include(c => c.Resources)
            .ThenInclude(r => ((Dataset)r).Series)
            .Include(c => c.Resources)
            .ThenInclude(r => ((Dataset)r).Schema)
            .Include(c => c.Resources)
            .ThenInclude(r => ((CataloguedResource)r).ResponsiblePerson)
            .Include(c => c.Resources)
            .ThenInclude(r => ((CataloguedResource)r).ResponsibleOrganisation)
            .Include(c => c.Parts)
            .AsSplitQuery()
            .SingleOrDefault(c => c.Uri == identifier);
        if (null == catalogue)
            throw new ArgumentException($"Catalogue [{identifier}] not found");

        ICollection<XCatalogueElement>? resources = null;
        if (XCatalogueElementType.Catalogue != type) {
            ICollection<CataloguedResource> filteredResources;
            if (null != type) {
                filteredResources = catalogue.Resources.Where(r =>
                    XCatalogueElementType.Dataset == type && r is Dataset ||
                    XCatalogueElementType.DatasetSeries == type && r is DatasetSeries ||
                    XCatalogueElementType.DataService == type && r is DataService).ToList();
            } else filteredResources = catalogue.Resources;
            
            // ===> Authorisation
            filteredResources = filteredResources
                .Where(r => agents.CheckVisibilityAndSetRoles(r, userId)).ToList();

            resources =
                mapper.Map<ICollection<CataloguedResource>, ICollection<XCatalogueElement>>(filteredResources);
            if (null != type) return resources;
        }
        
        ICollection<XCatalogueElement> catalogues = 
            mapper.Map<ICollection<Catalogue>, ICollection<XCatalogueElement>>(catalogue.Parts
                .Where(c => agents.CheckVisibilityAndSetRoles(c, userId)).ToList()); // Authorisation

        return null == resources ? catalogues : catalogues.Concat(resources);
    }
    
    public XCatalogue GetMainCatalogue(string? userId)
    {
        Catalogue? catalogue = context.Catalogues
            .Include(c => c.PartOf)
            .Include(c => c.ResponsiblePerson)
            .Include(c => c.ResponsibleOrganisation)
            .Include(c => c.Contacts)
            .Include(c => c.EditorshipsLink)
            .Include(c => c.EditorshipsLink.Editors)
            .Include(c => c.EditorshipsLink.EditorRoles)
            // TODO - find also the main catalogues of organisations where the Person is a manager/member 
            .AsSplitQuery()
            .FirstOrDefault(c => null == c.PartOf);
        if (null == catalogue) throw new SystemException("Main catalogue not found");
        
        // ===> Authorisation
        agents.SetUserRoles(catalogue, userId);
        
        XCatalogue xCatalogue = mapper.Map<XCatalogue>(catalogue);
        return xCatalogue;
    }
    
    // ==== Dataset series management ===================================================================

    public XDatasetSeries GetSeries(string identifier, string? userId)
    {
        DatasetSeries? series = context.DatasetSeries
            .Include(d => d.Catalogue)
            .Include(c => c.ResponsiblePerson)
            .Include(c => c.ResponsibleOrganisation)
            .Include(c => c.Contacts)
            .Include(c => c.EditorshipsLink)
            .Include(c => c.EditorshipsLink.Editors)
            .Include(c => c.EditorshipsLink.EditorRoles)
            .AsSplitQuery()
            .SingleOrDefault(d => d.Uri == identifier);
        
        // ===> Authorisation
        if (null == series || !agents.CheckVisibilityAndSetRoles(series, userId))
            throw new NotFoundOrVisibleException($"Dataset series [{identifier}] not found or not visible to the user");
        
        XDatasetSeries xSeries = mapper.Map<XDatasetSeries>(series);
        return xSeries;
    }

    public bool CheckSeriesId(string identifier)
    {
        return !context.DatasetSeries.Any(d => d.Uri == identifier);
    }

    public string UpsertSeries(XDatasetSeries xSeries, IEnumerable<XContactData> newContacts, string userId)
    {
        // Set a semaphore to prevent from multiple threads to generate the same URI
        SrsSemaphore.Wait();
            
        // Generate unique identifier if needed
        bool generatedUri = common.CheckAndGenerateUri(xSeries);
            
        DatasetSeries requestedVersion = mapper.Map<DatasetSeries>(xSeries);

        using var transaction = context.Database.BeginTransaction();
        try {
            UserAccount account = agents.GetUserAccount(userId)!;
            
            DatasetSeries? existingVersion = null;
            // Get the old version if it exists in the database
            if (!generatedUri) existingVersion = context.DatasetSeries
                .Include(d => d.EditorshipsLink)
                .Include(d => d.EditorshipsLink.Editors)
                .Include(d => d.EditorshipsLink.EditorRoles)
                .Include(d => d.ResponsibleOrganisation)
                .Include(d => d.ResponsiblePerson)
                .Include(d => d.ResponsiblePerson!.Contacts)
                .Include(d => d.Contacts).ThenInclude(c => c.Agent)
                .AsSplitQuery()
                .SingleOrDefault(d => d.Uri == requestedVersion.Uri);
            bool isUpdate = null != existingVersion;
            
            // ===> Authorisation
            agents.CheckAuthorisation(requestedVersion, existingVersion, account);
            
            if (requestedVersion.HasMetadataRole()) {
                var designatedParent = common.HandleParentAndContacts(xSeries, account, 
                    existingVersion ?? requestedVersion, isUpdate, newContacts);
                
                common.CheckIcon(xSeries);
                
                if (!isUpdate) {
                    // Old version does not exist? - Add the new dataset series and insert to the catalogue
                    context.DatasetSeries.Add(requestedVersion);
                    designatedParent.Resources.Add(requestedVersion);
                } else {
                    // Otherwise - Update the old dataset series with new data (possibly change the catalogue)
                    requestedVersion.Id = existingVersion!.Id; // make sure to preserve the unique ID
                    context.Entry(existingVersion).CurrentValues.SetValues(requestedVersion);
                    context.Entry(existingVersion).State = EntityState.Modified;
                    designatedParent.Resources.Add(existingVersion);
                }
            }
            
            if (requestedVersion.HasOwnershipRole())
                common.HandleResourceEditors(existingVersion ?? requestedVersion, isUpdate, xSeries);
            else if (!requestedVersion.HasMetadataRole())
                throw new ArgumentException("No change to the dataset series was requested");
            
            if (!(existingVersion ?? requestedVersion).Validate(account.Roles.Contains(GlobalRole.DataAdmin))) 
                throw new ArgumentException("Dataset series did not pass validation");
            
            context.SaveChanges();
            transaction.Commit();
            SrsSemaphore.Release();
            
            return existingVersion?.Uri ?? requestedVersion.Uri; // return the URI of the added or updated series
        } catch (Exception) {
            context.ChangeTracker.Clear(); transaction.Rollback(); SrsSemaphore.Release(); throw;
        }
    }

    public void DeleteSeries(string identifier, string userId)
    {
        using IDbContextTransaction transaction = context.Database.BeginTransaction();
        try {
            // To delete, make sure that all the related objects are loaded (included into the context)
            DatasetSeries? series = context.DatasetSeries
                .Include(d => d.Catalogue)
                .Include(d => d.Datasets)
                .Include(d => d.EditorshipsLink)
                .Include(d => d.EditorshipsLink.Editors)
                .Include(c => c.EditorshipsLink.EditorRoles)
                .Include(d => d.Contacts).ThenInclude(c => c.Agent)
                .AsSplitQuery()
                .SingleOrDefault(d => d.Uri == identifier);
            
            // ===> Authorisation
            if (null == series || !agents.CheckVisibilityAndSetRoles(series, userId))
                throw new ArgumentException("Dataset series not found or not visible to the user");
            agents.SetUserRoles(series.Catalogue, userId);
            if (!series.Catalogue.HasMetadataRole())
                throw new UnauthorizedAccessException("User not permitted to delete the dataset series");
            
            // When deleting the dataset series, also all the related "orphaned" (without an Agent) contacts need to be removed 
            foreach (ContactData contactData in series.Contacts)
                if (null == contactData.Agent)
                    context.Entry(contactData).State = EntityState.Deleted;
            
            context.Entry(series).State = EntityState.Deleted;
            context.Entry(series.EditorshipsLink).State = EntityState.Deleted;
            context.SaveChanges();
            transaction.Commit();
        } catch (Exception) { context.ChangeTracker.Clear(); transaction.Rollback(); throw; }
    }

    public IEnumerable<XCatalogueElement> GetSeriesDatasetList(string identifier, string? userId)
    {
        DatasetSeries? series = context.DatasetSeries
            .Include(d => d.Datasets)
            .ThenInclude(d => d.Schema)
            .Include(d => d.Datasets)
            .ThenInclude(d => d.ResponsiblePerson)
            .Include(d => d.Datasets)
            .ThenInclude(d => d.ResponsibleOrganisation)
            .AsSplitQuery()
            .SingleOrDefault(c => c.Uri == identifier);
        
        // ===> Authorisation
        if (null == series || !agents.CheckVisibilityAndSetRoles(series, userId))
            throw new ArgumentException($"DatasetSeries [{identifier}] not found");
        
        agents.SetUserRoles(series.Datasets, userId); // set roles for all datasets in the series
        
        ICollection<XCatalogueElement> datasets =
            mapper.Map<ICollection<Dataset>, ICollection<XCatalogueElement>>(series.Datasets);
        return datasets;
    }
    
    // ==== Dataset management ==========================================================================

    public XDataset GetDataset(string identifier, string? userId)
    {
        Dataset? dataset = context.Datasets
            .Include(d => d.Catalogue)
            .Include(c => c.ResponsiblePerson)
            .Include(c => c.ResponsibleOrganisation)
            .Include(c => c.Contacts)
            .Include(c => c.EditorshipsLink)
            .Include(c => c.EditorshipsLink.Editors)
            .Include(c => c.EditorshipsLink.EditorRoles)
            .Include(d => d.Series)
            .Include(d => d.Schema)
            .Include(d => d.Target)
            .AsSplitQuery()
            .SingleOrDefault(d => d.Uri == identifier);
        
        // ===> Authorisation
        if (null == dataset || !agents.CheckVisibilityAndSetRoles(dataset, userId))
            throw new NotFoundOrVisibleException($"Dataset [{identifier}] not found or not visible to the user");

        XDataset xDataset = mapper.Map<XDataset>(dataset);
        return xDataset;
    }

    public bool CheckDatasetId(string identifier)
    {
        return !context.Datasets.Any(d => d.Uri == identifier);
    }

    public string UpsertDataset(XDataset xDataset, IEnumerable<XContactData> newContacts, string userId, bool addApp = false)
    {
        // Set a semaphore to prevent from multiple threads to generate the same URI
        DtsSemaphore.Wait();
            
        // Generate unique identifier if needed
        bool generatedUri = common.CheckAndGenerateUri(xDataset);
        
        Dataset requestedVersion = mapper.Map<Dataset>(xDataset);

        using IDbContextTransaction transaction = context.Database.BeginTransaction();
        try {
            UserAccount account = agents.GetUserAccount(userId)!;

            Dataset? existingVersion = null;
            // Get the old version if it exists in the database
            if(!generatedUri) existingVersion = context.Datasets
                .Include(d => d.Series)
                .Include(d => d.EditorshipsLink)
                .Include(d => d.EditorshipsLink.Editors)
                .Include(d => d.EditorshipsLink.EditorRoles)
                .Include(d => d.ResponsibleOrganisation)
                .Include(d => d.ResponsiblePerson)
                .Include(d => d.ResponsiblePerson!.Contacts)
                .Include(d => d.Contacts).ThenInclude(c => c.Agent)
                .Include(d => d.Schema)
                .Include(d => d.Target)
                .AsSplitQuery()
                .SingleOrDefault(d => d.Uri == requestedVersion.Uri);
            bool isUpdate = null != existingVersion;
            
            // ===> Authorisation
            agents.CheckAuthorisation(requestedVersion, existingVersion, account);

            if (requestedVersion.HasMetadataRole())
            {
                var designatedParent = common.HandleParentAndContacts(xDataset, account,
                    existingVersion ?? requestedVersion, isUpdate, newContacts);

                // Update the dataset series membership
                ICollection<DatasetSeries> series = new List<DatasetSeries>();
                if (0 < xDataset.SeriesUris.Count)
                {
                    series = context.DatasetSeries.Where(d => xDataset.SeriesUris.Contains(d.Uri)).ToList();
                }

                // Update the data schema for this dataset
                DataSchema? schema = null;
                if (!string.IsNullOrEmpty(xDataset.SchemaUri))
                {
                    schema = context.DataSchemas
                        .SingleOrDefault(s => s.Uri == xDataset.SchemaUri);
                    if (null == schema) throw new ArgumentException("The schema declared for this dataset not found");
                }
                
                // Update target dataset for this dataset
                Dataset? targetDataset = null;
                if (!string.IsNullOrEmpty(xDataset.TargetDatasetUri))
                {
                    targetDataset = context.Datasets.SingleOrDefault(d => xDataset.TargetDatasetUri == d.Uri);
                    if (null == targetDataset) 
                        throw new ArgumentException("The target dataset declared for this dataset not found");
                }
                
                common.CheckIcon(xDataset);

                if (!isUpdate) {
                    requestedVersion.Series = series;
                    requestedVersion.Schema = schema;
                    requestedVersion.Target = targetDataset;
                    context.Datasets.Add(requestedVersion);
                    designatedParent.Resources.Add(requestedVersion);
                } else {
                    // Otherwise - Update the old dataset with new data (possibly change the catalogue)\
                    requestedVersion.Id = existingVersion!.Id; // make sure to preserve the unique ID
                    context.Entry(existingVersion).CurrentValues.SetValues(requestedVersion);
                    existingVersion.Series = series;
                    existingVersion.Schema = schema;
                    existingVersion.Target = targetDataset;
                    context.Entry(existingVersion).State = EntityState.Modified;
                    designatedParent.Resources.Add(existingVersion);
                }
            }

            if (requestedVersion.HasOwnershipRole())
                    common.HandleResourceEditors(existingVersion ?? requestedVersion, isUpdate, xDataset);
            else if (!requestedVersion.HasMetadataRole())
                throw new ArgumentException("No change to the dataset was requested");
            
            if (!(existingVersion ?? requestedVersion).Validate(account.Roles.Contains(GlobalRole.DataAdmin)))
                throw new ArgumentException("Dataset did not pass validation");
            
            context.SaveChanges();

            AppTemplate? appTemplate = null;
            if (addApp && !isUpdate) appTemplate = appTemplates.CreateDefaultAppTemplate(requestedVersion);
            
            transaction.Commit();
            
            if (null != appTemplate) appTemplates.DeployDefaultApp(requestedVersion, appTemplate);
            
            DtsSemaphore.Release();
            
            return existingVersion?.Uri ?? requestedVersion.Uri; // return the URI of the added or updated dataset
        } catch (Exception) { context.ChangeTracker.Clear(); transaction.Rollback();
            DtsSemaphore.Release(); throw; }
    }

    public void DeleteDataset(string identifier, string userId, bool force = false)
    {
        using IDbContextTransaction transaction = context.Database.BeginTransaction();
        try {
            // To delete, make sure that all the related objects are loaded (included into the context)
            Dataset? dataset = context.Datasets
                .Include(d => d.Catalogue)
                .Include(d => d.Distributions)
                .Include(d => d.Series)
                .Include(d => d.EditorshipsLink)
                .Include(d => d.EditorshipsLink.Editors)
                .Include(d => d.EditorshipsLink.EditorRoles)
                .Include(d => d.Contacts).ThenInclude(c => c.Agent)
                .AsSplitQuery()
                .SingleOrDefault(d => d.Uri == identifier);
            
            // ===> Authorisation
            if (null == dataset || !agents.CheckVisibilityAndSetRoles(dataset, userId))
                throw new ArgumentException($"Dataset not found or not visible to the user");
            agents.SetUserRoles(dataset.Catalogue, userId);
            if (!dataset.Catalogue.HasMetadataRole())
                throw new UnauthorizedAccessException("User not permitted to delete the dataset");
            if (!force && 0 < dataset.Distributions.Count)
                throw new InvalidOperationException("Cannot delete the dataset with distributions");
            
            // When deleting the dataset, also all the related "orphaned" (without an Agent) contacts need to be removed 
            foreach (ContactData contactData in dataset.Contacts)
                if (null == contactData.Agent)
                    context.Entry(contactData).State = EntityState.Deleted;
            context.Entry(dataset).State = EntityState.Deleted;
            context.Entry(dataset.EditorshipsLink).State = EntityState.Deleted;
            context.SaveChanges();
            transaction.Commit();
        } catch (Exception) { context.ChangeTracker.Clear(); transaction.Rollback(); throw; }
    }

    public XCkanResponse PublishDataset(string identifier, string userId, 
        bool withData = false, bool createEmptyDatastore = false)
    {
        Dataset? dataset = context.Datasets
            .Include(d => d.Catalogue)
            .Include(d => d.ResponsiblePerson)
            .Include(d => d.ResponsibleOrganisation)
            .Include(d => d.Contacts)
            .Include(d => d.EditorshipsLink)
            .Include(d => d.EditorshipsLink.Editors)
            .Include(d => d.EditorshipsLink.EditorRoles)
            .Include(d => d.Series)
            .Include(d => d.Distributions)
            .Include(d => d.Items)
                .ThenInclude(i => i.Concept)
                    .ThenInclude(c => c.Properties)
            .Include(d => d.Items)
                .ThenInclude(i => i.Concept)
                    .ThenInclude(c => c.Schema)
                        .ThenInclude(s => s!.UsedNamespaces)
            .Include(d => d.Items)
                .ThenInclude(i => i.Concept)
                    .ThenInclude(c => c.Namespace)
            .Include(d => d.Schema)
                .ThenInclude(s => s!.Concepts)
                    .ThenInclude(c => c.Properties)
            .Include(d => d.Schema)
                .ThenInclude(s => s!.UsedNamespaces)
            .Include(d => d.Schema)
                .ThenInclude(s => s!.Concepts)
                    .ThenInclude(c => c.Namespace)
            .AsSplitQuery()
            .SingleOrDefault(d => d.Uri == identifier);
        
        if (null == dataset || !agents.CheckVisibilityAndSetRoles(dataset, userId))
            throw new NotFoundOrVisibleException($"Dataset [{identifier}] not found or not visible to the user");
        
        if (!dataset.HasMetadataRole())
            throw new UnauthorizedAccessException($"User not permitted to publish this dataset [{identifier}]");

        Organisation organisation;
        if (null == dataset.ResponsibleOrganisation) {
            Catalogue rootCatalogue = GetRootCatalogue(dataset)!;
            if (null != rootCatalogue.ResponsibleOrganisation)
                organisation = rootCatalogue.ResponsibleOrganisation;
            else organisation = new Organisation {
                Name = MCkanProxyCommon.GetPreferredTranslation(rootCatalogue.Title),
                Uri = rootCatalogue.Uri
            };
        } else organisation = dataset.ResponsibleOrganisation;

        bool ensureDatastoreExists = dataset.Status == DatasetStatus.Independent;

        if (withData) {
            if (ensureDatastoreExists) 
                throw new ArgumentException("Cannot publish data for independent dataset");
            
            return ckanActions.PublishMetadataWithData(dataset, organisation).GetAwaiter().GetResult();
        }
        return ckanActions.PublishMetadata(dataset, organisation, ensureDatastoreExists).GetAwaiter().GetResult();
    }

    public XCkanResponse UnpublishDataset(string identifier, string userId) {
        Dataset? dataset = context.Datasets
            .AsSplitQuery()
            .SingleOrDefault(d => d.Uri == identifier);
        
        if (null == dataset || !agents.CheckVisibilityAndSetRoles(dataset, userId))
            throw new NotFoundOrVisibleException($"Dataset [{identifier}] not found or not visible to the user");
        
        if (!dataset.HasMetadataRole())
            throw new UnauthorizedAccessException($"User not permitted to publish this dataset [{identifier}]");
        
        return ckanActions.UnpublishDataset(dataset);
    }
        
    private Catalogue? GetRootCatalogue(Dataset dataset)
    {
        Catalogue currentCatalogue = dataset.Catalogue;
        for (int i = 0; i<10; i++)
        {
            if (null == currentCatalogue.PartOf) return currentCatalogue;
            context.Attach(currentCatalogue.PartOf);
            currentCatalogue = currentCatalogue.PartOf;
        }
        return null;
    }
    
    //==== Utilities ======================================================================================
    //=====================================================================================================

    private List<OwnableResource> GetAgentResources(string userId, ICollection<ResourceType> types,
        ICollection<EditorRole>? role, string? agentId = null)
    {
        UserAccount account = agents.GetUserAccount(userId)!;
        // If agentId is provided, get the agent
        Agent? agent = null != agentId ? agents.GetAgent(agentId) : null;
        
        // Get all resources from the database depending on their type(s)
        List<OwnableResource> unfilteredResources = context.OwnableResources
            .Include(r => r.ResponsiblePerson)
            .Include(r => r.ResponsibleOrganisation)
            .Include(r => r.EditorshipsLink)
            .Include(r => r.EditorshipsLink.Editors)
            .Include(r => r.EditorshipsLink.EditorRoles)
            .Include(r => ((Dataset)r).Series)
            .Include(r => ((Dataset)r).Schema)
            .Include(r => ((CataloguedResource)r).Catalogue)
            .Include(r => ((Catalogue)r).PartOf)
            .AsSplitQuery()
            .Where(c => (c is Dataset && types.Contains(ResourceType.Dataset))
                        || (c is DatasetSeries && types.Contains(ResourceType.Series))
                        || (c is DataService && types.Contains(ResourceType.Service))
                        || (c is Catalogue && types.Contains(ResourceType.Catalogue))).ToList();
        
        List<OwnableResource> filteredResources = [];
        // Iterate over all resources and filter them by the user's roles and agent's roles
        foreach (var resource in unfilteredResources) {
            var userRoles = agents.GetUserRoles(resource, account);
            resource.UserRoles = null == agent
                ? userRoles
                : agents.GetAgentRolesForResource(resource, agent)
                    .Where(r => userRoles.Contains(r)).ToList();
            if (null == role && (0 != resource.UserRoles.Count || agents.IsGloballyVisible(resource))
                || null != role && role.All(r => resource.UserRoles.Contains(r)))
                filteredResources.Add(resource);
        }
        return filteredResources;
    }
}

public enum ResourceType
{
    Dataset,
    Series,
    Service,
    Catalogue
}
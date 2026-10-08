using AutoMapper;
using DoorCEModel.Infrastructure;
using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEServer.Application.CataloguingManager.Common;
using DoorCEServer.Application.CataloguingManager.Domain;
using DoorCEServer.Application.DataTemplateManager.Common;
using DoorCEServer.Application.DataTemplateManager.Dtos;
using DoorCEServer.Application.DataTemplateManager.Interfaces;
using DoorCEServer.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DoorCEServer.Application.DataTemplateManager.Domain;

class MSchemas(IMapper mapper, ApplicationDbContext dbContext, MCataloguingManagerCommon cataloguing,
    MAgentsCommon agents, MSchemaManagerCommon common) : SchemaAPI
{
    private static readonly SemaphoreSlim SchSemaphore = new (1, 1);
    private static readonly SemaphoreSlim SrsSemaphore = new (1, 1);

    public IEnumerable<XSchemaSeries> GetAllowedSchemaSeries(string? schemaId, string userId)
    {
        UserAccount account = agents.GetUserAccount(userId)!;
        
        // Get all the schema series from the database
        List<SchemaSeries> unfilteredSeries = dbContext.SchemaSeries
            .Include(s => s.Schemas)
            .Include(s => s.Current)
            .Include(s => s.EditorshipsLink)
            .Include(s => s.EditorshipsLink.Editors)
            .Include(s => s.EditorshipsLink.EditorRoles)
            .AsSplitQuery().ToList();
        
        List<SchemaSeries> filteredSeries = [];
        // Iterate over all schema series and filter those that are allowed for the user
        foreach (var series in unfilteredSeries) {
            series.UserRoles = agents.GetUserRoles(series, account);
            if (series.UserRoles.Contains(EditorRole.MetadataEditor))
                filteredSeries.Add(series);
        }
        
        // If a person ID is provided, get the schema series that this schema is a member of
        if (!string.IsNullOrEmpty(schemaId)) {
            DataSchema? schema = dbContext.DataSchemas
                .Include(d => d.InSeries)
                .AsSplitQuery()
                .SingleOrDefault(d => d.Uri == schemaId);
            if (null == schema || !agents.CheckVisibilityAndSetRoles(schema.InSeries, userId))
                throw new ArgumentException($"Schema [{schemaId}] not found or not visible to the user");

            // Add schema series that the schema is a member of, but not already in the list
            SchemaSeries parent = schema.InSeries;
            if (filteredSeries.All(s => s.Uri != parent.Uri)) filteredSeries.Add(parent);
        }

        return mapper.Map<ICollection<SchemaSeries>, ICollection<XSchemaSeries>>(filteredSeries);
    }

    public XSchemaSeries GetSchemaSeries(string identifier, string? userId)
    {
        SchemaSeries? series = dbContext.SchemaSeries
            .Include(s => s.Schemas)
            .Include(s => s.Current)
            .Include(s => s.EditorshipsLink)
            .Include(s => s.EditorshipsLink.Editors)
            .Include(s => s.EditorshipsLink.EditorRoles)
            .AsSplitQuery()
            .SingleOrDefault(s => s.Uri == identifier);
        
        // ===> Authorisation
        if (null == series || !agents.CheckVisibilityAndSetRoles(series, userId))
            throw new NotFoundOrVisibleException($"Schema series [{identifier}] not found or not visible to the user");
        
        XSchemaSeries xSeries = mapper.Map<XSchemaSeries>(series);
        return xSeries;
    }

    public IEnumerable<XSchemaSeries> GetSchemaSeriesList(string? query, string? userId)
    {
        query = query?.ToLower();
        
        ICollection<SchemaSeries> series = dbContext.SchemaSeries
            .Include(s => s.Schemas)
            .Include(s => s.Current)
            .Include(s => s.EditorshipsLink)
            .Include(s => s.EditorshipsLink.Editors)
            .Include(s => s.EditorshipsLink.EditorRoles)
            .AsSplitQuery()
            .Where(s => string.IsNullOrEmpty(query) || query.Length < 3 ||
                        s.Title.ToLower().Contains(query)).ToList();
        
        // ===> Authorisation
        series = series.Where(s => agents.CheckVisibilityAndSetRoles(s, userId)).ToList();

        ICollection<XSchemaSeries> xSchemaSeriesList =
            mapper.Map<ICollection<SchemaSeries>, ICollection<XSchemaSeries>>(series);

        return xSchemaSeriesList;
    }

    public string UpsertSchemaSeries(XSchemaSeries xSeries, string userId)
    {
        // Set a semaphore to prevent from multiple threads to generate the same URI
        SrsSemaphore.Wait();
        
        // Generate unique identifier if needed
        bool generatedUri = cataloguing.CheckAndGenerateUri(xSeries);
        
        SchemaSeries requestedVersion = mapper.Map<SchemaSeries>(xSeries);

        using IDbContextTransaction transaction = dbContext.Database.BeginTransaction();
        try {
            UserAccount account = agents.GetUserAccount(userId)!;
            
            SchemaSeries? existingVersion = null;
            // Get the old version if it exists in the database
            if (!generatedUri) existingVersion = dbContext.SchemaSeries
                .Include(s => s.EditorshipsLink)
                .Include(s => s.EditorshipsLink.Editors)
                .Include(s => s.EditorshipsLink.EditorRoles)
                .Include(s => s.Current)
                .Include(s => s.Schemas)
                .AsSplitQuery()
                .SingleOrDefault(s => s.Uri == requestedVersion.Uri);
            bool isUpdate = null != existingVersion;
            
            // ===> Authorisation
            if (!isUpdate && !agents.CheckSchemaAuthorisation(account))
                throw new UnauthorizedAccessException("User does not have permissions to create a new schema series");
            agents.CheckAuthorisation(requestedVersion, existingVersion, account);

            if (requestedVersion.HasMetadataRole())
            {
                // Old version does not exist? - Add the new schema series
                if (!isUpdate) dbContext.SchemaSeries.Add(requestedVersion);
                else {
                    // Otherwise - Update the old schema series with new data
                    requestedVersion.Id = existingVersion!.Id; // make sure to preserve the unique ID
                    dbContext.Entry(existingVersion).CurrentValues.SetValues(requestedVersion);
                    DataSchema? newCurrentSchema = null;
                    if (!String.IsNullOrEmpty(xSeries.CurrentSchemaUri))
                        newCurrentSchema = dbContext.DataSchemas
                                               .Include(d => d.InSeries)
                                               .Include(d => d.Concepts)
                                               .ThenInclude(c => c.Properties)
                                               .Include(d => d.UsedNamespaces)
                                               .AsSplitQuery()
                                               .SingleOrDefault(d => d.Uri == xSeries.CurrentSchemaUri) ??
                                           throw new ArgumentException(
                                               "The current schema declared for the schema series not found");
                    existingVersion.Current = newCurrentSchema;
                    dbContext.Entry(existingVersion).State = EntityState.Modified;
                }
            }

            if (requestedVersion.HasOwnershipRole())
                cataloguing.HandleResourceEditors(existingVersion ?? requestedVersion, isUpdate, xSeries);
            else if (!requestedVersion.HasMetadataRole())
                throw new ArgumentException("No change to the schema series was requested");
            
            if (!(existingVersion ?? requestedVersion).Validate(account.Roles.Contains(GlobalRole.DataAdmin))) 
                throw new ArgumentException("Schema series did not pass validation");

            dbContext.SaveChanges();
            transaction.Commit();
            SrsSemaphore.Release();

            return existingVersion?.Uri ?? requestedVersion.Uri;
        } catch (Exception) {
            dbContext.ChangeTracker.Clear(); transaction.Rollback(); SrsSemaphore.Release(); throw;
        }
    }

    public void DeleteSchemaSeries(string identifier, string userId)
    {
        using IDbContextTransaction transaction = dbContext.Database.BeginTransaction();
        try {
            // To delete, make sure that all the related objects are loaded (included into the context)
            SchemaSeries? series = dbContext.SchemaSeries
                .Include(s => s.Schemas)
                .Include(s => s.Current)
                .Include(s => s.EditorshipsLink)
                .Include(s => s.EditorshipsLink.Editors)
                .Include(s => s.EditorshipsLink.EditorRoles)
                .AsSplitQuery()
                .SingleOrDefault(s => s.Uri == identifier);
            
            // ===> Authorisation
            if (null == series || !agents.CheckVisibilityAndSetRoles(series, userId))
                throw new ArgumentException($"Schema series [{identifier}] not found or not visible to the user");
            if (!series.HasMetadataRole())
                throw new UnauthorizedAccessException(
                    "User does not have permissions to delete this schema series");
            
            // Cannot delete if it contains some schemas
            if (0 != series.Schemas.Count || null != series.Current)
                throw new InvalidOperationException(
                    "Cannot delete schema series that contains schemas or has a current schema set");
            
            dbContext.Entry(series).State = EntityState.Deleted;
            dbContext.Entry(series.EditorshipsLink).State = EntityState.Deleted;
            dbContext.SaveChanges();
            transaction.Commit();
        } catch (Exception) { dbContext.ChangeTracker.Clear(); transaction.Rollback(); throw; }
    }

    public bool CheckSchemaSeriesId(string identifier) {
        return !dbContext.SchemaSeries.Any(s => s.Uri == identifier);
    }

    public IEnumerable<XDataSchema> GetDataSchemaList(string? query, string? userId)
    {
        query = query?.ToLower();
        
        ICollection<DataSchema> schemas = dbContext.DataSchemas
            .Include(s => s.InSeries)
            .Include(s => s.Datasets)
            .Include(s => s.DataServices)
            .Include(s => s.Concepts)
            .ThenInclude(c => c.Properties)
            .Include(s => s.UsedNamespaces)
            .AsSplitQuery()
            .Where(s => string.IsNullOrEmpty(query) || query.Length < 3 ||
                        s.Title.ToLower().Contains(query)).ToList();
        
        // ===> Authorisation
        schemas = schemas.Where(s => agents.CheckVisibilityAndSetRoles(s.InSeries, userId)).ToList();

        ICollection<XDataSchema> xSchemaList =
            mapper.Map<ICollection<DataSchema>, ICollection<XDataSchema>>(schemas);
        
        // ===> Authorisation
        foreach (XDataSchema ds in xSchemaList) ds.IsUserEditable = 
            schemas.Single(s => s.Uri == ds.Uri).InSeries.HasMetadataRole();

        return xSchemaList;
    }

    public IEnumerable<XDataSchema> GetSeriesDataSchemaList(string seriesIdentifier, string? userId)
    {
        SchemaSeries? series = dbContext.SchemaSeries.SingleOrDefault(s => s.Uri == seriesIdentifier);
        
        // ===> Authorisation
        if (null == series || !agents.CheckVisibilityAndSetRoles(series, userId))
            throw new ArgumentException("Schema series not found");
            
        ICollection<DataSchema> schemas = dbContext.DataSchemas
            .Include(d => d.InSeries)
            .Include(d => d.Concepts)
            .ThenInclude(c => c.Properties)
            .Include(d => d.UsedNamespaces)
            .AsSplitQuery()
            .Where(d => d.InSeries.Uri == seriesIdentifier).ToList();

        ICollection<XDataSchema> xDataSchemas =
            mapper.Map<ICollection<DataSchema>, ICollection<XDataSchema>>(schemas);
        
        // ===> Authorisation
        if (series.HasMetadataRole())
            foreach (XDataSchema xSchema in xDataSchemas) xSchema.IsUserEditable = true;

        return xDataSchemas;
    }

    public XDataSchema GetDataSchema(string identifier, string? userId)
    {
        DataSchema? schema = dbContext.DataSchemas
            .Include(d => d.InSeries)
            .Include(d => d.Concepts)
            .ThenInclude(d => d.Properties)
            // .ThenInclude(d => d.Type) TODO - add support for types (primitive?)
            .Include(d => d.UsedNamespaces)
            .AsSplitQuery()
            .SingleOrDefault(d => d.Uri == identifier);
        
        // ===> Authorisation
        if (null == schema || !agents.CheckVisibilityAndSetRoles(schema.InSeries, userId))
            throw new NotFoundOrVisibleException($"Schema [{identifier}] not found or not visible to the user");
        
        XDataSchema xDataSchema = mapper.Map<XDataSchema>(schema);
        
        // ===> Authorisation
        xDataSchema.IsUserEditable = schema.InSeries.HasMetadataRole();
        
        return xDataSchema;
    }

    public string UpsertDataSchema(XDataSchema xSchema, string userId)
    {
        // Set a semaphore to prevent from multiple threads to generate the same URI
        SchSemaphore.Wait();
            
        // Generate unique identifier if needed
        bool generatedUri = cataloguing.CheckAndGenerateUri(xSchema);
        
        DataSchema requestedVersion = mapper.Map<DataSchema>(xSchema);
        
        using IDbContextTransaction transaction = dbContext.Database.BeginTransaction();
        try {
            UserAccount account = agents.GetUserAccount(userId)!;
            
            // Validate custom namespaces
            foreach (Namespace ns in requestedVersion.UsedNamespaces)
                if (dbContext.Namespaces.Any(n => !(n is CustomNamespace)
                                                  && (n.Prefix == ns.Prefix || n.Iri == ns.Iri)))
                    throw new ArgumentException("Standard namespace cannot be used as a custom namespace");

            // Adds standard namespaces that are omitted by the mapping mechanism
            foreach(XNamespace xNamespace in xSchema.UsedNamespaces)
                if (!xNamespace.IsCustom) {
                    Namespace standardNamespace = dbContext.Namespaces
                        .SingleOrDefault(n => !(n is CustomNamespace) && n.Prefix == xNamespace.Prefix) 
                        ?? throw new ArgumentException("Standard namespace not found!");
                    requestedVersion.UsedNamespaces.Add(standardNamespace);
                }
            
            DataSchema? existingVersion = null;
            // Get the old version if it exists in the database
            if (!generatedUri) existingVersion = dbContext.DataSchemas
                .Include(d => d.InSeries)
                .Include(d => d.InSeries.EditorshipsLink)
                .Include(d => d.InSeries.EditorshipsLink.EditorRoles)
                .Include(d => d.InSeries.EditorshipsLink.Editors)
                .Include(d => d.Concepts)
                .ThenInclude(d => d.Properties)
                .Include(d => d.UsedNamespaces)
                .AsSplitQuery()
                .SingleOrDefault(d => d.Uri == requestedVersion.Uri);
            bool isUpdate = null != existingVersion;

            // ===> Authorisation
            agents.CheckAuthorisation(xSchema, existingVersion, account);

            if (isUpdate)
                requestedVersion.UsedNamespaces =
                    common.PrepareNamespaceUpdate(requestedVersion.UsedNamespaces,
                        existingVersion!.UsedNamespaces);
            
            // Links namespaces for concepts and properties to the namespaces used in the schema 
            foreach (Concept concept in requestedVersion.Concepts){
                XConcept xConcept = xSchema.Concepts.Single(c => c.Name == concept.Name);
                if (!String.IsNullOrEmpty(xConcept.NamespacePrefix))
                    concept.Namespace = requestedVersion.UsedNamespaces
                                            .SingleOrDefault(n => n.Prefix == xConcept.NamespacePrefix)
                                        ?? throw new ArgumentException(
                                            $"Namespace with prefix '{xConcept.NamespacePrefix}' not found in the schema");
                foreach (Property property in concept.Properties){
                    string? propertyNamespacePrefix = xConcept.Properties[property.Name].NamespacePrefix;
                    if (!String.IsNullOrEmpty(propertyNamespacePrefix))
                        property.Namespace = requestedVersion.UsedNamespaces
                                                 .SingleOrDefault(n => n.Prefix == propertyNamespacePrefix)
                                             ?? throw new ArgumentException(
                                                 $"Namespace with prefix '{propertyNamespacePrefix}' not found in the schema");
                }
            }

            if (xSchema.IsUserEditable) {
                // Get the parent schema series
                // + Authorisation based on SchemaSeries privileges
                SchemaSeries parentSeries = cataloguing.HandleParents(xSchema, account, existingVersion);
                
                if (!isUpdate) {
                    // Old version does not exist? - Add the new data schema and insert to the schema series
                    dbContext.DataSchemas.Add(requestedVersion);
                    parentSeries.Schemas.Add(requestedVersion);
                } else {
                    // Otherwise - Update the old data schema with new data (possibly change the schema series)
                    requestedVersion.Id = existingVersion!.Id; // make sure to preserve the unique ID
                    dbContext.Entry(existingVersion).CurrentValues.SetValues(requestedVersion);
                    dbContext.Entry(existingVersion).State = EntityState.Modified;
                    existingVersion.UsedNamespaces = requestedVersion.UsedNamespaces; // replace the namespaces
                    existingVersion.Concepts = common.PrepareConceptUpdate(
                        requestedVersion.Concepts, existingVersion.Concepts); // replace the concepts
                    parentSeries.Schemas.Add(existingVersion);
                }

                ((existingVersion ?? requestedVersion).Concepts.SingleOrDefault(c => c.Name == xSchema.MainConceptName)
                 ?? throw new ArgumentException("Main concept not found in the schema")).IsMain = true;
                if (null != xSchema.DefaultNamespacePrefix) {
                    Namespace defaultNamespace =
                        (existingVersion ?? requestedVersion).UsedNamespaces.SingleOrDefault(c =>
                             c.Prefix == xSchema.DefaultNamespacePrefix)
                         ?? throw new ArgumentException("Default namespace not found in the schema");
                    if (defaultNamespace is CustomNamespace customNamespace) customNamespace.IsDefault = true;
                    else throw new ArgumentException("Default namespace must be a custom namespace");
                }

                // Rollback if the schema and its main concept are not valid
                if (!(existingVersion ?? requestedVersion).Validate())
                    throw new ArgumentException("Data schema did not pass validation");

                dbContext.SaveChanges();
                transaction.Commit();
                SchSemaphore.Release();
            } else
                throw new ArgumentException("No change to the resource was requested");
            
            return existingVersion?.Uri ?? requestedVersion.Uri;
        } catch (Exception) { dbContext.ChangeTracker.Clear(); transaction.Rollback();
            SchSemaphore.Release(); throw; }
    }

    public void DeleteDataSchema(string identifier, string userId)
    {
        using IDbContextTransaction transaction = dbContext.Database.BeginTransaction();
        try {
            DataSchema? schema = dbContext.DataSchemas
                .Include(s => s.InSeries)
                .Include(s => s.Concepts)
                .ThenInclude(c => c.Properties)
                .Include(s => s.UsedNamespaces)
                .AsSplitQuery()
                .SingleOrDefault(d => d.Uri == identifier);
            
            // ===> Authorisation
            if (null == schema || !agents.CheckVisibilityAndSetRoles(schema.InSeries, userId))
                throw new ArgumentException($"Schema [{identifier}] not found or not visible to the user");
            if (!schema.InSeries.HasMetadataRole())
                throw new UnauthorizedAccessException("User does not have permissions to delete this schema");
            
            // Cannot delete if some datasets or distributions are linked to the schema
            if (dbContext.Datasets.Any(d => null != d.Schema && d.Schema.Id == schema.Id) ||
                dbContext.Distributions.Any(d => null != d.Schema && d.Schema.Id == schema.Id))
                throw new InvalidOperationException(
                    "Cannot delete schema that is linked to datasets or distributions");

            foreach (CustomNamespace cn in schema.UsedNamespaces.OfType<CustomNamespace>())
                // If the custom namespace is not used by any other schema, delete it
                // (in the current version, this should always be the case)
                if (!dbContext.DataSchemas.Any(d => d.UsedNamespaces.Contains(cn) && d.Id != schema.Id))
                    dbContext.Entry(cn).State = EntityState.Deleted;
            
            if (dbContext.AppTemplates.Include(at => at.Schema)
                .Any(at => at.Schema.Id == schema.Id))
                throw new InvalidOperationException("Cannot delete schema as it is in use by one or more app templates");
            
            dbContext.Entry(schema).State = EntityState.Deleted;
            dbContext.SaveChanges();
            transaction.Commit();
        } catch (Exception) { dbContext.ChangeTracker.Clear(); transaction.Rollback(); throw; }
    }

    public bool CheckSchemaId(string identifier)
    {
        return !dbContext.DataSchemas.Any(s => s.Uri == identifier);
    }

    public IEnumerable<XNamespace> GetStandardNamespaceList(){
        ICollection<Namespace> namespaces = dbContext.Namespaces
            .Where(n => !(n is CustomNamespace)).ToList();

        ICollection<XNamespace> xSchemaSeriesList =
            mapper.Map<ICollection<Namespace>, ICollection<XNamespace>>(namespaces);

        return xSchemaSeriesList;
    }
}

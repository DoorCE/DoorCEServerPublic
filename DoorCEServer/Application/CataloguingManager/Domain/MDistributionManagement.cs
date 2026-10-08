using AutoMapper;
using DoorCEModel.Infrastructure;
using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEModel.Infrastructure.DataModel.Datasets;
using DoorCEServer.Application.CataloguingManager.Common;
using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Application.CataloguingManager.Interfaces;
using DoorCEServer.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DoorCEServer.Application.CataloguingManager.Domain;

public class MDistributionManagement(IMapper mapper, ApplicationDbContext dbContext,
    MCataloguingManagerCommon common, MAgentsCommon agents) : DistributionManagementAPI
{
    private static readonly SemaphoreSlim DsrSemaphore = new(1, 1);
    private static readonly SemaphoreSlim SrvSemaphore = new(1, 1);
    
    // ==== Distribution management ==========================================================
    
    public IEnumerable<XDistribution> GetDatasetDistributionList(string identifier, string? userId)
    {
        var dataset = dbContext.Datasets
            .Include(d => d.Distributions)
            .ThenInclude(d => d.DataService)
            .AsSplitQuery()
            .FirstOrDefault(d => d.Uri == identifier);
        
        // ===> Authorisation
        if (null == dataset || !agents.CheckVisibilityAndSetRoles(dataset, userId))
            throw new ArgumentException("Dataset not found or not visible");
        
        var xDistributions = mapper.Map<IEnumerable<XDistribution>>(dataset.Distributions);
        
        // ===> Authorisation
        if (dataset.HasDistributionRole())
            foreach (XDistribution distribution in xDistributions) distribution.IsUserEditable = true;
        
        return xDistributions;
    }

    public XDistribution GetDistribution(string identifier, string? userId)
    {
        Distribution? distribution = dbContext.Distributions
            .Include(d => d.Dataset)
            .Include(d => d.Dataset.EditorshipsLink)
            .Include(d => d.Dataset.EditorshipsLink.EditorRoles)
            .Include(d => d.DataService)
            .Include(d => d.DataService!.EditorshipsLink)
            .Include(d => d.DataService!.EditorshipsLink.EditorRoles)
            .Include(d => d.Schema)
            .AsSplitQuery()
            .SingleOrDefault(d => d.Uri == identifier);
        
        // ===> Authorisation
        if (null == distribution || !agents.CheckVisibilityAndSetRoles(distribution,userId))
            throw new NotFoundOrVisibleException($"Distribution [{identifier}] not found or not visible to the user");
         
        if (null != distribution.FileId && !dbContext.DataFiles.Any(df => df.Id == distribution.FileId))
            throw new SystemException("Data file not found");
        String? fileName = dbContext.DataFiles
            .Where(df => df.Id == distribution.FileId).Select(df => df.Name).FirstOrDefault();
        
        XDistribution xDistribution = mapper.Map<XDistribution>(distribution);
        xDistribution.IsUserEditable = distribution.Dataset.HasDistributionRole();
        xDistribution.FileName = fileName;
        return xDistribution;
    }

    public bool CheckDistributionId(string identifier)
    {
        return !dbContext.Distributions.Any(d => d.Uri == identifier);
    }

    public string UpsertDistribution(XDistribution xDistribution, string userId)
    {
        // Set a semaphore to prevent from multiple threads to generate the same URI
        DsrSemaphore.Wait();
            
        // Generate unique identifier if needed
        bool generatedUri = common.CheckAndGenerateUri(xDistribution);
        
        Distribution requestedVersion = mapper.Map<Distribution>(xDistribution);

        using IDbContextTransaction transaction = dbContext.Database.BeginTransaction();
        try {
            UserAccount account = agents.GetUserAccount(userId)!;

            Distribution? existingVersion = null;
            // Get the old version if it exists in the database
            if(!generatedUri) existingVersion = dbContext.Distributions
                .Include(d => d.Dataset)
                .Include(d => d.Dataset.EditorshipsLink)
                .Include(d => d.Dataset.EditorshipsLink.EditorRoles)
                .Include(d => d.DataService)
                .Include(d => d.DataService!.EditorshipsLink)
                .Include(d => d.DataService!.EditorshipsLink.EditorRoles)
                .Include(d => d.Schema)                
                .AsSplitQuery()
                .SingleOrDefault(d => d.Uri == requestedVersion.Uri);
            bool isUpdate = null != existingVersion;

            // ===> Authorisation
            agents.CheckAuthorisation(xDistribution, existingVersion, account);

            if (xDistribution.IsUserEditable) {
                // Get the parent dataset
                Dataset designatedParent = common.HandleParents(xDistribution, account, existingVersion);
                
                // Update the data schema for this dataset
                DataSchema? schema = null;
                if (!string.IsNullOrEmpty(xDistribution.SchemaUri)) {
                    schema = dbContext.DataSchemas
                        .SingleOrDefault(s => s.Uri == xDistribution.SchemaUri);
                    if (null == schema) throw new ArgumentException(
                        "The schema declared for this distribution not found");
                }
                
                // Validate if the data file exists
                if (null != xDistribution.FileId && !dbContext.DataFiles.Any(df => df.Id == xDistribution.FileId))
                    throw new ArgumentException("Data file not found");
                
                if (!isUpdate) {
                    // Old version does not exist? - Add the new distribution and insert to the dataset
                    dbContext.Distributions.Add(requestedVersion);
                    designatedParent.Distributions.Add(requestedVersion);
                    requestedVersion.Schema = schema;
                } else {
                    // Otherwise - Update the old dataset series with new data (possibly change the catalogue)
                    requestedVersion.Id = existingVersion!.Id; // make sure to preserve the unique ID
                    dbContext.Entry(existingVersion).CurrentValues.SetValues(requestedVersion);
                    dbContext.Entry(existingVersion).State = EntityState.Modified;
                    designatedParent.Distributions.Add(existingVersion);
                    existingVersion.Schema = schema;
                }
            }

            if (!isUpdate && null != xDistribution.DataServiceUri
                || isUpdate && xDistribution.DataServiceUri != existingVersion!.DataService?.Uri)
            {
                // Update relation to data service
                DataService? dataService = null;
                if (null != xDistribution.DataServiceUri) {
                    dataService = dbContext.DataServices
                        .SingleOrDefault(d => d.Uri == xDistribution.DataServiceUri);
                    if (null == dataService)
                        throw new ArgumentException("The data service declared for this distribution not found");
                    dataService.UserRoles = agents.GetUserRoles(dataService, account);
                    if (!dataService.HasDistributionRole())
                        throw new UnauthorizedAccessException(
                            "This user has no privilege to add distributions to this data service");
                }

                if (isUpdate && null == dataService) {
                    existingVersion!.DataService!.UserRoles = agents.GetUserRoles(existingVersion.DataService, account);
                    if (!existingVersion.DataService.HasDistributionRole() || !xDistribution.IsUserEditable)
                        throw new UnauthorizedAccessException(
                            "This user has no privilege to remove this distribution from this data service");
                }
                (existingVersion ?? requestedVersion).DataService = dataService;
            } else if (!xDistribution.IsUserEditable)
                throw new ArgumentException("No change to the distribution was requested");
            
            if (!(existingVersion ?? requestedVersion).Validate()) 
                throw new ArgumentException("Distribution did not pass validation");
            
            dbContext.SaveChanges();
            transaction.Commit();
            DsrSemaphore.Release();
            
            return existingVersion?.Uri ?? requestedVersion.Uri; // return the URI of the added or updated resource
        } catch (Exception) { 
            dbContext.ChangeTracker.Clear(); transaction.Rollback(); DsrSemaphore.Release(); throw;
        }
    }

    public void DeleteDistribution(string identifier, string userId)
    {
        using IDbContextTransaction transaction = dbContext.Database.BeginTransaction();
        try {
            // To delete, make sure that all the related objects are loaded (included into the context)
            Distribution? distribution = dbContext.Distributions
                .Include(d => d.Dataset)
                .Include(d => d.DataService)
                .AsSplitQuery()
                .SingleOrDefault(d => d.Uri == identifier);
            
            // ===> Authorisation
            if (null == distribution || !agents.CheckVisibilityAndSetRoles(distribution, userId))
                throw new ArgumentException($"Distribution [{identifier}] not found or not visible to the user");
            if (!distribution.Dataset.HasDistributionRole())
                throw new UnauthorizedAccessException("This user has no privilege to delete this distribution");
            
            dbContext.Entry(distribution).State = EntityState.Deleted;
            dbContext.SaveChanges();
            transaction.Commit();
        } catch (Exception) { dbContext.ChangeTracker.Clear(); transaction.Rollback(); throw; }
    }
    
    // ==== Data service management ====================================================================

    public XDataService GetDataService(string identifier, string? userId)
    {
        DataService? dataService = dbContext.DataServices
            .Include(d => d.ConformsTo)
            .Include(d => d.Catalogue)
            .Include(d => d.ResponsiblePerson)
            .Include(d => d.ResponsibleOrganisation)
            .Include(d => d.Contacts)
            .Include(d => d.EditorshipsLink)
            .Include(d => d.EditorshipsLink.Editors)
            .Include(d => d.EditorshipsLink.EditorRoles)
            .AsSplitQuery()
            .SingleOrDefault(d => d.Uri == identifier);
        
        // ===> Authorisation
        if (null == dataService || !agents.CheckVisibilityAndSetRoles(dataService, userId))
            throw new NotFoundOrVisibleException($"Data service [{identifier}] not found or not visible to the user");
        
        XDataService xDataService = mapper.Map<XDataService>(dataService);
        return xDataService;
    }

    public bool CheckDataServiceId(string identifier)
    {
        return !dbContext.DataServices.Any(ds => ds.Uri == identifier);
    }

    public string UpsertDataService(XDataService xDataService, IEnumerable<XContactData> newContacts,
        string userId)
    {
        // Set a semaphore to prevent from multiple threads to generate the same URI
        SrvSemaphore.Wait();
            
        // Generate unique identifier if needed
        bool generatedUri = common.CheckAndGenerateUri(xDataService);
        
        DataService requestedVersion = mapper.Map<DataService>(xDataService);

        using IDbContextTransaction transaction = dbContext.Database.BeginTransaction();
        try {
            UserAccount account = agents.GetUserAccount(userId)!;

            DataService? existingVersion = null;
            // Get the old version if it exists in the database
            if (!generatedUri) existingVersion = dbContext.DataServices
                .Include(d => d.EditorshipsLink)
                .Include(d => d.EditorshipsLink.Editors)
                .Include(d => d.EditorshipsLink.EditorRoles)
                .Include(d => d.ResponsibleOrganisation)
                .Include(d => d.ResponsiblePerson)
                .Include(d => d.ResponsiblePerson!.Contacts)
                .Include(d => d.Contacts).ThenInclude(c => c.Agent)
                .Include(d => d.ConformsTo)
                .AsSplitQuery()
                .SingleOrDefault(d => d.Uri == requestedVersion.Uri);
            bool isUpdate = null != existingVersion;
            
            // ===> Authorisation
            agents.CheckAuthorisation(requestedVersion, existingVersion, account);

            if (requestedVersion.HasMetadataRole())
            {
                var designatedParent = common.HandleParentAndContacts(xDataService, account,
                    existingVersion ?? requestedVersion, isUpdate, newContacts);
                
                common.CheckIcon(xDataService);

                if (!isUpdate) {
                    // Old version does not exist? - Add the new service and insert to the catalogue
                    dbContext.DataServices.Add(requestedVersion);
                    designatedParent.Resources.Add(requestedVersion);
                } else {
                    // Otherwise - Update the old data service with new data (possibly change the catalogue)
                    requestedVersion.Id = existingVersion!.Id; // make sure to preserve the unique ID
                    dbContext.Entry(existingVersion).CurrentValues.SetValues(requestedVersion);
                    dbContext.Entry(existingVersion).State = EntityState.Modified;
                    designatedParent.Resources.Add(existingVersion);
                }
            }

            if (requestedVersion.HasOwnershipRole())
                common.HandleResourceEditors(existingVersion ?? requestedVersion, isUpdate, xDataService);
            else if (!requestedVersion.HasMetadataRole())
                throw new ArgumentException("No change to the data service was requested");
            
            if (!(existingVersion ?? requestedVersion).Validate(account.Roles.Contains(GlobalRole.DataAdmin))) 
                throw new ArgumentException("Data service did not pass validation");
            
            dbContext.SaveChanges();
            transaction.Commit();
            SrvSemaphore.Release();
            
            return existingVersion?.Uri ?? requestedVersion.Uri; // return the URI of the added or updated resource
        } catch (Exception) { dbContext.ChangeTracker.Clear(); transaction.Rollback();
            SrvSemaphore.Release(); throw; }
    }

    public void DeleteDataService(string identifier, string userId)
    {
        using IDbContextTransaction transaction = dbContext.Database.BeginTransaction();
        try {
            // To delete, make sure that all the related objects are loaded (included into the context)
            DataService? dataService = dbContext.DataServices
                .Include(d => d.Catalogue)
                .Include(d => d.ResponsiblePerson)
                .Include(d => d.ResponsibleOrganisation)
                .Include(d => d.ConformsTo)
                .Include(d => d.EditorshipsLink)
                .Include(d => d.EditorshipsLink.Editors)
                .Include(d => d.Contacts).ThenInclude(c => c.Agent)
                .AsSplitQuery()
                .SingleOrDefault(d => d.Uri == identifier);
            
            // ===> Authorisation
            if (null == dataService || !agents.CheckVisibilityAndSetRoles(dataService, userId))
                throw new ArgumentException($"Data service [{identifier}] not found or not visible to the user");
            agents.SetUserRoles(dataService.Catalogue, userId);
            if (!dataService.Catalogue.HasMetadataRole())
                throw new UnauthorizedAccessException("This user has no privilege to delete this data service");
            
            // When deleting the dataset, also all the related "orphaned" (without an Agent) contacts need to be removed 
            foreach (ContactData contactData in dataService.Contacts)
                if (null == contactData.Agent)
                    dbContext.Entry(contactData).State = EntityState.Deleted;
            
            dbContext.Entry(dataService).State = EntityState.Deleted;
            dbContext.Entry(dataService.EditorshipsLink).State = EntityState.Deleted;
            dbContext.SaveChanges();
            transaction.Commit();
        } catch (Exception) { dbContext.ChangeTracker.Clear(); transaction.Rollback(); throw; }
    }

    public IEnumerable<XDistribution> GetServiceDistributionList(string identifier, string? userId)
    {
        var dataService = dbContext.DataServices
            .Include(d => d.Distributions)
            .ThenInclude(d => d.Dataset)
            .AsSplitQuery()
            .FirstOrDefault(d => d.Uri == identifier);
        
        // ===> Authorisation
        if (null == dataService || !agents.CheckVisibilityAndSetRoles(dataService, userId))
            throw new NotFoundOrVisibleException($"Data service [{identifier}] not found or not visible to the user");

        ICollection<XDistribution> xDistributions = [];
        foreach (Distribution distribution in dataService.Distributions) {
            var xDistribution = mapper.Map<XDistribution>(distribution);
            agents.SetUserRoles(distribution.Dataset, userId);
            xDistribution.IsUserEditable = distribution.Dataset.HasDistributionRole();
            xDistributions.Add(xDistribution);
        }

        return xDistributions;
    }
}
using DoorCEModel.Infrastructure;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEServer.Tests.CataloguingManager.DtoHelpers;
using DoorCEServer.Tests.CataloguingManager.Labels;
using DoorCEServer.Tests.SchemaManager.DtoHelpers;
using DoorCEServer.Tests.SchemaManager.Labels;
using Microsoft.EntityFrameworkCore.Storage;
using Serilog;

namespace DoorCEServer.Tests;

public class BaseTestHelper(ApplicationDbContext context,
    SchemaSeriesHelper sSeriesHelper,
    SchemaHelper schemaHelper,
    OrganisationHelper orgHelper,
    PersonHelper personHelper,
    CatalogueHelper catHelper,
    DatasetSeriesHelper dSeriesHelper,
    DatasetHelper datasetHelper,
    DataServiceHelper dataServiceHelper,
    DistributionHelper distributionHelper)
{
    protected readonly SchemaSeriesHelper SSeriesHelper = sSeriesHelper;
    protected readonly SchemaHelper SchemaHelper = schemaHelper;
    protected readonly OrganisationHelper OrgHelper = orgHelper;
    protected readonly PersonHelper PersonHelper = personHelper;
    protected readonly CatalogueHelper CatHelper = catHelper;
    protected readonly DatasetSeriesHelper DSeriesHelper = dSeriesHelper;
    protected readonly DatasetHelper DatasetHelper = datasetHelper;
    protected readonly DataServiceHelper ServiceHelper = dataServiceHelper;
    protected readonly DistributionHelper DistrHelper = distributionHelper;
    
    public short ClearDatabase()
    {
        using IDbContextTransaction transaction = context.Database.BeginTransaction();
        try {
            context.DataItems.RemoveRange(context.DataItems);
            context.Distributions.RemoveRange(context.Distributions);
            context.CataloguedResources.RemoveRange(context.CataloguedResources);
            context.Catalogues.RemoveRange(context.Catalogues);
            context.AcquisitionApps.RemoveRange(context.AcquisitionApps);
            context.CodeFiles.RemoveRange(context.CodeFiles);
            context.CodePackages.RemoveRange(context.CodePackages);
            context.AppTemplates.RemoveRange(context.AppTemplates);
            context.Standards.RemoveRange(context.Standards);
            context.Namespaces.RemoveRange(context.Namespaces.OfType<CustomNamespace>());
            context.SchemaSeries.RemoveRange(context.SchemaSeries);
            context.ContactDatas.RemoveRange(context.ContactDatas);
            context.Agents.RemoveRange(context.Agents);
            context.Icons.RemoveRange(context.Icons);
            context.DataFiles.RemoveRange(context.DataFiles);
            context.ResourceEditorshipsLinks.RemoveRange(context.ResourceEditorshipsLinks);
            // Additional cleaning
            context.NamespaceElements.RemoveRange(context.NamespaceElements);
            context.SaveChanges();
            transaction.Commit();
            return 0;
        } catch (Exception) { context.ChangeTracker.Clear(); transaction.Rollback();
            return -1;
        }
    }

    public bool UpsertAll(OrgLabel? orgLabel=null, PersonLabel? personLabel=null, CatLabel? catLabel=null,
        DSeriesLabel? dseriesLabel=null, DatasetLabel? datasetLabel=null, ServiceLabel? serviceLabel=null,
        DistrLabel? distributionLabel=null, SSeriesLabel? sSeriesLabel=null, SchemaLabel? schemaLabel=null, 
        bool update = false)
    {
        VariantLabel variantLabel = update ? VariantLabel.ModifiedBeforeUpsert : VariantLabel.BeforeUpsert;
        ContactsLabel contactsLabel = update ? ContactsLabel.Contacts2 : ContactsLabel.Contacts1;
        
        // *create schema series and schema
        if (null != sSeriesLabel)
        {
            SSeriesHelper.Upsert((SSeriesLabel)sSeriesLabel, variantLabel);
        }
        
        if (null != schemaLabel)
        {
            SchemaHelper.Upsert((SchemaLabel)schemaLabel, variantLabel);
        }
        
        // *create organisation
        if (null != orgLabel)
            OrgHelper.Upsert((OrgLabel)orgLabel, variantLabel, contactsLabel);
        
        // *create person
        if (null != personLabel)
            PersonHelper.Upsert((PersonLabel)personLabel, variantLabel, contactsLabel);
        
        // *create catalogue
        if (null != catLabel)
            CatHelper.Upsert((CatLabel)catLabel, variantLabel, contactsLabel);
        
        // *create dataset series
        if (null != dseriesLabel)
            DSeriesHelper.Upsert((DSeriesLabel)dseriesLabel, variantLabel, contactsLabel);
        
        // *create dataset
        if (null != datasetLabel)
            DatasetHelper.Upsert((DatasetLabel)datasetLabel, variantLabel, contactsLabel);
        
        // *create data service
        if (null != serviceLabel)
            ServiceHelper.Upsert((ServiceLabel)serviceLabel, variantLabel, contactsLabel);
        
        // *create distribution
        if (null != distributionLabel)
            DistrHelper.Upsert((DistrLabel)distributionLabel, variantLabel);
        
        return true;
    }
    
    protected void CleanUp(bool cleanupAllowed = true)
    {
        if (cleanupAllowed) ClearDatabase();
        context.ChangeTracker.Clear();
    }

    protected void SetUp(bool cleanupAllowed = true)
    {
        CleanUp(cleanupAllowed);
        context.ChangeTracker.Clear();
    }
    
    public string ErrorCatcher<T2>(Func<T2> method)
    {
        var methodName = method.Method.Name;
        
        Log.Debug("*********************************{MethodName}: starting...",methodName);

        try {
            method();
        }
        catch (Exception e) {
            ClearDatabase();
            Log.Debug("{Message}",e.ToString());
            return $"{methodName}: failed - {e.Message}";
        }

        return $"{methodName}: success";
    }
}
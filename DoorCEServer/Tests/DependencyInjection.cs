using DoorCEServer.Application.CkanProxy.Domain;
using DoorCEServer.Tests.CataloguingManager.DtoHelpers;
using DoorCEServer.Tests.CataloguingManager.TestHelpers;
using DoorCEServer.Tests.CataloguingManager.Tests;
using DoorCEServer.Tests.CkanProxy.Tests;
using DoorCEServer.Tests.SchemaManager.DtoHelpers;
using DoorCEServer.Tests.SchemaManager.TestHelpers;
using DoorCEServer.Tests.SchemaManager.Tests;

namespace DoorCEServer.Tests;

public static class DependencyInjection
{
    public static void AddTestServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<CatalogueHelper>();
        builder.Services.AddScoped<DataServiceHelper>();
        builder.Services.AddScoped<DatasetHelper>();
        builder.Services.AddScoped<DatasetSeriesHelper>();
        builder.Services.AddScoped<DistributionHelper>();
        builder.Services.AddScoped<OrganisationHelper>();
        builder.Services.AddScoped<PersonHelper>();
        
        builder.Services.AddScoped<SchemaHelper>();
        builder.Services.AddScoped<SchemaSeriesHelper>();
        builder.Services.AddScoped<OrganisationHelper>();
        builder.Services.AddScoped<CatalogueHelper>();
        builder.Services.AddScoped<PersonHelper>();
        builder.Services.AddScoped<DatasetHelper>();
        builder.Services.AddScoped<DataServiceHelper>();
        builder.Services.AddScoped<DistributionHelper>();
        
        builder.Services.AddScoped<MDatasetMgmtTestHelper>();
        builder.Services.AddScoped<MAgentsTestHelper>();
        builder.Services.AddScoped<MSchemasTestHelper>();
        builder.Services.AddScoped<MDistrMgmtTestHelper>();

        builder.Services.AddScoped<MDatasetMgmtTest>();
        builder.Services.AddScoped<MDistrMgmtTest>();
        builder.Services.AddScoped<MAgentsTest>();
        builder.Services.AddScoped<MSchemasTest>();
        builder.Services.AddScoped<MCkanActionsTest>();
        builder.Services.AddScoped<MCkanActions>();
    }

    public static void AddPopulateServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<TestCommon>();
        builder.Services.AddScoped<MPopulateDatasetData>();
        builder.Services.AddScoped<MPopulateSchemaData>();
    }
}
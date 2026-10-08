using DoorCEServer.Application.DataTemplateManager.Interfaces;

namespace DoorCEServer.Tests.SchemaManager.Tests;

public class MPopulateSchemaData(SchemaAPI schemaApi, AppTemplateAPI appApi, TestCommon testCommon)
{
    public void PopulateSchemas()
    {
        // ****** Add schema series ******
        schemaApi.UpsertSchemaSeries(
            testCommon.GetMockXSchemaSeries("scs/trees", "Trees"),
            "udas-admin");
        schemaApi.UpsertSchemaSeries(
            testCommon.GetMockXSchemaSeries("scs/tree_species", "Tree Species"),
            "udas-admin");

        // ****** Add data schemas ******
        schemaApi.UpsertDataSchema(
            testCommon.GetMockXDataSchema("sch/tree_data_schema_v1_", "Tree Data Schema (v1)",
                "scs/trees"), "udas-admin");
        schemaApi.UpsertDataSchema(
            testCommon.GetMockXDataSchema("sch/tree_data_schema_v2_", "Tree Data Schema (v2)",
                "scs/trees", true), "udas-admin");
    }

    public void PopulateApps()
    {
    // ****** Add app templates ******
        appApi.UpsertAppTemplate(
            testCommon.GetMockXAppTemplate("apt/tree_entry", "Tree Entry", 
                "sch/tree_data_schema_v2_", "Tree"), "udas-admin");
        
        appApi.UpsertAppTemplate(
            testCommon.GetMockXAppTemplate("apt/species_entry", "Species Entry", 
                "sch/tree_data_schema_v2_", "Tree Species"), "udas-admin");
        
        // ****** Add apps ******
        appApi.UpsertApp(testCommon
            .GetMockXApp("app/tree_entry_app","Tree Entry App","apt/tree_entry",
                "dat/trees_in_city/1.0",
                ["dat/trees_in_parks/1.0", "dat/trees_in_forests/1.0"]),
                "udas-admin");
        
        appApi.UpsertApp(testCommon
            .GetMockXApp("app/species_entry_app","Species Entry App","apt/species_entry",
                "dat/trees_in_city/1.0",
                ["dat/trees_in_parks/1.0", "dat/trees_in_forests/1.0"]),
                "udas-admin");
    }
}
using DoorCEModel.Infrastructure;
using DoorCEServer.Application;
using Microsoft.AspNetCore.Mvc;
using DoorCEServer.Tests;
using DoorCEServer.Tests.CataloguingManager.DtoHelpers;
using DoorCEServer.Tests.CataloguingManager.Tests;
using DoorCEServer.Tests.CkanProxy.Tests;
using DoorCEServer.Tests.SchemaManager.DtoHelpers;
using DoorCEServer.Tests.SchemaManager.Tests;
using Serilog;

namespace DoorCEServer.WebApi.Controllers;

[ApiController]
[Route("maintenance")]
public class MaintenanceController(ApplicationDbContext context, MPopulateDatasetData populateDatasets,
    MPopulateSchemaData populateSchemas, MDistrMgmtTest distributionTest, MAgentsTest agentTest,
    MDatasetMgmtTest datasetTest, MSchemasTest schemaTest,SchemaSeriesHelper schemaSeriesHelper,
    SchemaHelper schemaHelper,
    OrganisationHelper organisationHelper, PersonHelper personHelper,
    CatalogueHelper catalogueHelper, DatasetSeriesHelper datasetSeriesHelper, DatasetHelper datasetHelper,
    DataServiceHelper dataServiceHelper, DistributionHelper distributionHelper,
    DataInitialisation dataInitialiser, MCkanActionsTest ckanTest) : ControllerBase
{
    private readonly BaseTestHelper _baseTestHelper = new(context, schemaSeriesHelper, schemaHelper, organisationHelper,
        personHelper, catalogueHelper, datasetSeriesHelper, datasetHelper, dataServiceHelper, distributionHelper);

    [HttpPost("TextCkanProxy")]
    public string TestCkanProxy() {
        return ckanTest.TestAll();
    }

    [HttpPost ("TestUpsertOrganisation")]
    public string TestUpsertOrganisation()
    {
        var i = ErrorCatcher(agentTest.InsertOrganisationBasicTest);
        var u = ErrorCatcher(agentTest.UpdateOrganisationBasicTest);
        
        return $"{i}\n{u}";
    }
    
    [HttpPost ("TestUpsertPerson")]
    public string TestUpsertPerson()
    {
        var i = ErrorCatcher(agentTest.InsertPersonWithOrgBasicTest);
        var u = ErrorCatcher(agentTest.UpdatePersonWithOrgBasicTest);
        
        return $"{i}\n{u}";
    }
    
    [HttpPost ("TestUpsertCatalogue")]
    public string TestUpsertCatalogue()
    {
        var i = ErrorCatcher(datasetTest.InsertCatalogueBasicTest);
        var u = ErrorCatcher(datasetTest.UpdateCatalogueBasicTest);
        
        return $"{i}\n{u}";
    }
    
    [HttpPost ("TestUpsertDatasetSeries")]
    public string TestUpsertDatasetSeries()
    {
        var i = ErrorCatcher(datasetTest.InsertDatasetSeriesBasicTest);
        var u = ErrorCatcher(datasetTest.UpdateDatasetSeriesWithCatalogueBasicTest);
        
        return $"{i}\n{u}";
    }
    
    [HttpPost ("TestUpsertDataset")]
    public string TestUpsertDataset()
    {
        var i = ErrorCatcher(datasetTest.InsertDatasetWithSeriesBasicTest);
        var u = ErrorCatcher(datasetTest.UpdateDatasetWithSeriesBasicTest);
        
        return $"{i}\n{u}";
    }
    
    [HttpPost ("TestUpsertDataService")]
    public string TestUpsertDataService()
    {
        var i = ErrorCatcher(distributionTest.InsertDataServiceBasicTest);
        var u = ErrorCatcher(distributionTest.UpdateDataServiceBasicTest);
        
        return $"{i}\n{u}";
    }
    
    [HttpPost ("TestUpsertDistribution")]
    public string TestUpsertDistribution()
    {
        var i = ErrorCatcher(distributionTest.InsertDistributionBasicTest);
        var u = ErrorCatcher(distributionTest.UpdateDistributionBasicTest);
        
        return $"{i}\n{u}";
    }
    
    [HttpPost ("TestUpsertSchemaSeries")]
    public string TestUpsertSchemaSeries()
    {
        var i = ErrorCatcher(schemaTest.InsertSchemaSeriesBasicTest);
        var u = ErrorCatcher(schemaTest.UpdateSchemaSeriesBasicTest);
        
        return $"{i}\n{u}";
    }
    
    [HttpPost ("TestUpsertSchema")]
    public string TestUpsertSchema()
    {
        var i = ErrorCatcher(schemaTest.InsertSchemaBasicTest);
        var u = ErrorCatcher(schemaTest.UpdateSchemaBasicTest);
        
        return $"{i}\n{u}";
    }
    
    [HttpPost("TestUpsertAll")]
    public string TestUpsertAll()
    {
        string resultOrg = TestUpsertOrganisation();
        string resultPerson = TestUpsertPerson();
        string resultCat = TestUpsertCatalogue();
        string resultDSeries = TestUpsertDatasetSeries();
        string resultDataset = TestUpsertDataset();
        string resultService = TestUpsertDataService();
        string resultDistr = TestUpsertDistribution();
        string resultSSeries = TestUpsertSchemaSeries();
        string resultSchema = TestUpsertSchema();
        
        dataInitialiser.InitiateMainCatalogue(); // TODO - refactor this
        
        return $"{resultOrg}\n{resultPerson}\n{resultCat}" +
               $"\n{resultDSeries}\n{resultDataset}\n{resultService}\n{resultDistr}\n{resultSSeries}\n{resultSchema}";
    }

    [HttpDelete("TestDeleteSchema")]
    public string TestDeleteSchema()
    {
        return ErrorCatcher(schemaTest.DeleteSchemaBasicTest);
    }
    
    [HttpDelete("TestDeleteSchemaSeries")]
    public string TestDeleteSchemaSeries()
    {
        return ErrorCatcher(schemaTest.DeleteSchemaSeriesBasicTest);
    }
    
    [HttpDelete("TestDeleteDistribution")]
    public string TestDeleteDistribution()
    {
        return ErrorCatcher(distributionTest.DeleteDistributionBasicTest);
    }

    [HttpDelete("TestDeleteDataService")]
    public string TestDeleteDataService()
    {
        return ErrorCatcher(distributionTest.DeleteDataServiceBasicTest);
    }
    
    [HttpDelete("TestDeleteDataset")]
    public string TestDeleteDataset()
    {
        return ErrorCatcher(datasetTest.DeleteDatasetBasicTest);
    }
    
    [HttpDelete("TestDeleteDatasetSeries")]
    public string TestDeleteDatasetSeries()
    {
        return ErrorCatcher(datasetTest.DeleteDatasetSeriesWithCatalogueTest);
    }
    
    [HttpDelete("TestDeleteCatalogue")]
    public string TestDeleteCatalogue()
    {
        string e = ErrorCatcher(datasetTest.DeleteCatalogueTest_Empty);
        string c = ErrorCatcher(datasetTest.DeleteCatalogueTest_WithChild);
        string r = ErrorCatcher(datasetTest.DeleteCatalogueTest_WithResources);

        return $"{e}\n{c}\n{r}";
    }
    
    [HttpDelete("TestDeletePerson")]
    public string TestDeletePerson()
    {
        string b = ErrorCatcher(agentTest.DeletePersonWithOrgBasicTest);
        string m = ErrorCatcher(agentTest.DeletePersonWithOrgTest_ManagesResources);
        string c = ErrorCatcher(agentTest.DeletePersonTest_ContactsUsedByResources);
        
        return $"{b}\n{m}\n{c}";
    }
    
    [HttpDelete("TestDeleteOrganisation")]
    public string TestDeleteOrganisation()
    {
        string b = ErrorCatcher(agentTest.DeleteOrganisationBasicTest);
        string m = ErrorCatcher(agentTest.DeleteOrgTest_ManagesResources);
        string c = ErrorCatcher(agentTest.DeleteOrganisationTest_ContactsUsedByResources);
        
        return $"{b}\n{m}\n{c}";
    }

    [HttpDelete("TestDeleteAll")]
    public string TestDeleteAll()
    {
        string o = TestDeleteOrganisation();
        string p = TestDeletePerson();
        string c = TestDeleteCatalogue();
        string ds = TestDeleteDatasetSeries();
        string d = TestDeleteDataset();
        string dsv = TestDeleteDataService();
        string distr = TestDeleteDistribution();
        string ss = TestDeleteSchemaSeries();
        string s = TestDeleteSchema();
        
        dataInitialiser.InitiateMainCatalogue(); // TODO - refactor this

        return $"{o}\n{p}\n{c}\n{ds}\n{d}\n{dsv}\n{distr}\n{ss}\n{s}";
    }
    
    private string ErrorCatcher(Func<bool,bool> method)
    {
        var methodName = method.Method.Name;

        try {
            method(true);
        }
        catch (Exception e) {
            ClearDatabase();
            Log.Debug("{Message}",e.ToString());
            return $"{methodName}: failed - {e.Message}";
        }

        return $"{methodName}: success";
    }

    /// <summary>
    /// Populates the database with test data
    /// </summary>
    /// <returns>Test</returns>
    [HttpPost("PopulateDatabase")]
    public string PopulateDatabase()
    {
        string message = "";
        try {
            populateSchemas.PopulateSchemas();
        } catch (Exception e) {
            message += $"Error: database not populated (Schemas) - {e.Message}";
        }

        try {
            populateDatasets.Populate();
        } catch (Exception e) {
            message += (0 == message.Length ? "" : " ") + $"Error: database not populated (Datasets) - {e.Message}";
        }
        
        try {
            populateSchemas.PopulateApps();
        } catch (Exception e) {
            message += (0 == message.Length ? "" : " ") + $"Error: database not populated (Apps) - {e.Message}";
        }
        
        return 0 == message.Length ? "Success: database populated" : message;
    }
    
    /// <summary>
    /// Clears (purges) the database - all data is removed
    /// </summary>
    /// <returns>success message</returns>
    [HttpDelete("ClearDatabase")]
    public string ClearDatabase()
    {
        if (0 == _baseTestHelper.ClearDatabase()) {
            dataInitialiser.InitiateMainCatalogue();
            return "Success: database cleared";
        }

        return "Error: database not cleared";
    }
    
    /// <summary>
    /// Creates the database - all tables are created (if not yet done)
    /// </summary>
    /// <returns>success message</returns>
    [HttpPost("CreateDatabase")]
    public void CreateDatabase()
    {
        dataInitialiser.InitiateDatabase();
    }
    
     /*[HttpPost("TestUpsertDataItemsToDatastore")]
     public IActionResult TestUpsertDataItems([FromQuery] string datasetUri, [FromQuery] string conceptUri)
     {
         var result = api.TestUpsert(datasetUri, conceptUri);
         return Ok(result);
     }*/
     
     private string Indent(string text, int level)
     {
         var prefix = new string('\t', level);
         return string.Join("\n", text.Split('\n').Select(line => prefix + line));
     }
    
     private string Column(bool startAlignment = true, int tabs = 0, params string[] children)
         => "Column(\n" 
            + Indent((startAlignment ? "\tcrossAxisAlignment: CrossAxisAlignment.start,\n" : null)
                     + "\tchildren: [\n" +
                     $"{Indent(string.Join(",\n", children), 2)}\n" +
                     "\t],\n)", tabs);
     
     private string Row(int tabs = 0, params string[] children)
         => "Row(\n" 
            + Indent("\tchildren: [\n" +
                     $"{Indent(string.Join(",\n", children), 2)}\n" +
                     "\t],\n)", tabs);
     private string DoorText(string text, string style)
         => "DoorText(\n"
            + $"\t{text},\n"
            + $"\t{style}\n" 
            + ")";

     [HttpGet("TestStringGeneration")]
     public string TestStringGeneration()
     {
         return "\t\treturn " + Column(tabs: 2, children: ["SizedBox()", Row(tabs:0, children:[DoorText("hello", "DoorTextStyle.norm")])]);
     }
}
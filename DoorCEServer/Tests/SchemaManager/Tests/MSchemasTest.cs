using DoorCEServer.Tests.SchemaManager.Labels;
using DoorCEServer.Tests.SchemaManager.TestHelpers;

namespace DoorCEServer.Tests.SchemaManager.Tests;

public class MSchemasTest(MSchemasTestHelper testHelper)
{
    public bool InsertSchemaSeriesBasicTest(bool cleanupAllowed = true)
    {
        var sseries = SSeriesLabel.BasicSchemaSeries;
        
        return testHelper.SSeriesInsertBasicTest(sseries, cleanupAllowed: cleanupAllowed);
    }

    public bool UpdateSchemaSeriesBasicTest(bool cleanupAllowed = true)
    {
        var sseries = SSeriesLabel.BasicSchemaSeries;

        return testHelper.SSeriesUpdateBasicTest(sseries, cleanupAllowed: cleanupAllowed);
    }
    
    public bool InsertSchemaBasicTest(bool cleanupAllowed = true)
    {
        var schema = SchemaLabel.BasicSchema;
        var sseries = SSeriesLabel.BasicSchemaSeries;
        
        return testHelper.SchemaInsertBasicTest(schema, sseries, cleanupAllowed);
    }

    public bool UpdateSchemaBasicTest(bool cleanupAllowed = true)
    {
        var schema = SchemaLabel.BasicSchema;
        var sseries = SSeriesLabel.BasicSchemaSeries;

        return testHelper.SchemaUpdateBasicTest(schema, sseries, cleanupAllowed);
    }
    
    public bool DeleteSchemaSeriesBasicTest(bool cleanupAllowed = true)
    {
        var sseries = SSeriesLabel.BasicSchemaSeries;
        
        return testHelper.DeleteSSeriesBasicTest(sseries, cleanupAllowed);
    }
    
    public bool DeleteSchemaBasicTest(bool cleanupAllowed = true)
    {
        var schema = SchemaLabel.BasicSchema;
        var sseries = SSeriesLabel.BasicSchemaSeries;
        
        testHelper.DeleteSchemaTest(schema, sseries, cleanupAllowed);
        
        return true;
    }
}
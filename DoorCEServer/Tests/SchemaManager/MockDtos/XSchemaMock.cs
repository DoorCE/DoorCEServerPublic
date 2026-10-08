using DoorCEServer.Application.DataTemplateManager.Dtos;
using DoorCEServer.Tests.SchemaManager.Labels;

namespace DoorCEServer.Tests.SchemaManager.MockDtos;

public class XSchemaMock
{
    private readonly Dictionary<SchemaLabel, List<Dictionary<VariantLabel, XDataSchema>>> _xSchemas;
    private readonly TestCommon _testCommon;

    private readonly XSchemaSeriesMock _xSSeriesMock;
    
    public XSchemaMock()
    {
        _xSchemas = new Dictionary<SchemaLabel, List<Dictionary<VariantLabel, XDataSchema>>>();
        _testCommon = new TestCommon();
        
        _xSSeriesMock = new XSchemaSeriesMock();
        
        Setup();
    }

    public XDataSchema Get(SchemaLabel schemaLabel, VariantLabel variantLabel)
    {
        try
        {
            var schemaList = _xSchemas[schemaLabel];
            var dictWithKey = schemaList.First(d => d.ContainsKey(variantLabel));
            return dictWithKey[variantLabel];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new Exception($"Mock schema not found: {schemaLabel}, {variantLabel}");
        }
    }
    
    private void Add(XDataSchema xSchema, SchemaLabel schemaLabel, VariantLabel variantLabel)
    {
        if (_xSchemas.ContainsKey(schemaLabel))
        {
            _xSchemas[schemaLabel].Add(new Dictionary<VariantLabel, XDataSchema>{{variantLabel, xSchema}});
        }
        else
        {
            _xSchemas.Add(schemaLabel, [new Dictionary<VariantLabel, XDataSchema>{{variantLabel, xSchema}}]);
        }
    }

    private void Setup()
    {
        // create schema series
        XSchemaSeries basicSSeries = _xSSeriesMock.Get(SSeriesLabel.BasicSchemaSeries, VariantLabel.AfterUpsert);
        XSchemaSeries basicSSeriesModified = _xSSeriesMock.Get(SSeriesLabel.BasicSchemaSeries, VariantLabel.ModifiedAfterUpsert);
        
        // create schemas and add them to dict
        
        XDataSchema basicSchemaBeforeUpsert = _testCommon.GetMockXDataSchema(
            "doorce/schemas/tree/1", "Tree Data Schema (v1)", basicSSeries.Uri!);
        Add(basicSchemaBeforeUpsert, SchemaLabel.BasicSchema, VariantLabel.BeforeUpsert);
        
        var basicSchemaAfterUpsert = _testCommon.GetMockXDataSchema(
            "doorce/schemas/tree/1", "Tree Data Schema (v1)", basicSSeries.Uri!);
        basicSchemaAfterUpsert.SeriesTitle = basicSSeries.Title;
        basicSchemaAfterUpsert.DefaultNamespaceIri = "https://doorce.com/schemas/tree#";
        basicSchemaAfterUpsert.MainConceptPrefix = "tree";
        basicSchemaAfterUpsert.UsedNamespaces.Where((n) => n.Prefix == "xsd").ToList()[0].Iri = "http://www.w3.org/2001/XMLSchema";
        Add(basicSchemaAfterUpsert, SchemaLabel.BasicSchema, VariantLabel.AfterUpsert);
        
        XDataSchema basicSchemaModifiedBeforeUpsert = _testCommon.GetMockXDataSchema(
            "doorce/schemas/tree/1", 
            "Tree Data Schema (v1) (upd)",
            basicSSeriesModified.Uri!);
        basicSchemaModifiedBeforeUpsert.Description = "This is our new schema. (upd)";
        Add(basicSchemaModifiedBeforeUpsert, SchemaLabel.BasicSchema, VariantLabel.ModifiedBeforeUpsert);

        var basicSchemaModifiedAfterUpsert = _testCommon.GetMockXDataSchema(
            "doorce/schemas/tree/1", 
            "Tree Data Schema (v1) (upd)",
            basicSSeriesModified.Uri!);
        basicSchemaModifiedAfterUpsert.Description = "This is our new schema. (upd)";
        basicSchemaModifiedAfterUpsert.SeriesTitle = basicSSeriesModified.Title;
        basicSchemaModifiedAfterUpsert.DefaultNamespaceIri = "https://doorce.com/schemas/tree#";
        basicSchemaModifiedAfterUpsert.MainConceptPrefix = "tree";
        basicSchemaModifiedAfterUpsert.UsedNamespaces.Where((n) => n.Prefix == "xsd").ToList()[0].Iri = "http://www.w3.org/2001/XMLSchema";
        Add(basicSchemaModifiedAfterUpsert, SchemaLabel.BasicSchema, VariantLabel.ModifiedAfterUpsert);
    }
}
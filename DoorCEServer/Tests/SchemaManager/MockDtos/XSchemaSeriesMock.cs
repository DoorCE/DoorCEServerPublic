using DoorCEServer.Application.DataTemplateManager.Dtos;
using DoorCEServer.Tests.SchemaManager.Labels;

namespace DoorCEServer.Tests.SchemaManager.MockDtos;

public class XSchemaSeriesMock
{
    private readonly Dictionary<SSeriesLabel, List<Dictionary<VariantLabel, XSchemaSeries>>> _xSchemaSeries;
    private readonly TestCommon _testCommon;
    
    public XSchemaSeriesMock()
    {
        _xSchemaSeries = new();
        _testCommon = new();
        
        Setup();
    }

    public XSchemaSeries Get(SSeriesLabel seriesLabel, VariantLabel variantLabel)
    {
        try
        {
            var seriesList = _xSchemaSeries[seriesLabel];
            var dictWithKey = seriesList.First(d => d.ContainsKey(variantLabel));
            return dictWithKey[variantLabel];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new($"Mock schema series not found: {seriesLabel}, {variantLabel}");
        }
    }
    
    private void Add(XSchemaSeries xSeries, SSeriesLabel sseriesLabel, VariantLabel variantLabel)
    {
        if (_xSchemaSeries.ContainsKey(sseriesLabel))
        {
            _xSchemaSeries[sseriesLabel].Add(new Dictionary<VariantLabel, XSchemaSeries>{{variantLabel, xSeries}});
        }
        else
        {
            _xSchemaSeries.Add(sseriesLabel, [new Dictionary<VariantLabel, XSchemaSeries>{{variantLabel, xSeries}}]);
        }
    }

    private void Setup()
    {
        // create schemas
        // TODO
        
        // create schema series and add them to dict
        
        XSchemaSeries basicSSeriesBeforeUpsert = _testCommon.GetMockXSchemaSeries(
            "doorce/schema-series/trees", "Trees");
        Add(basicSSeriesBeforeUpsert, SSeriesLabel.BasicSchemaSeries, VariantLabel.BeforeUpsert);
       
        var basicSSeriesAfterUpsert = _testCommon.GetMockXSchemaSeries(
            "doorce/schema-series/trees", "Trees");
        Add(basicSSeriesAfterUpsert, SSeriesLabel.BasicSchemaSeries, VariantLabel.AfterUpsert);
        
        XSchemaSeries basicSSeriesModifiedBeforeUpsert =
            _testCommon.GetMockXSchemaSeries("doorce/schema-series/trees", "Trees (upd)");
        basicSSeriesModifiedBeforeUpsert.Description = "This is our new schema series. (upd)";
        basicSSeriesModifiedBeforeUpsert.Type = new List<short>() {1, 2};
        Add(basicSSeriesModifiedBeforeUpsert, SSeriesLabel.BasicSchemaSeries, VariantLabel.ModifiedBeforeUpsert);

        var basicSSeriesModifiedAfterUpsert =
            _testCommon.GetMockXSchemaSeries("doorce/schema-series/trees", "Trees (upd)");
        basicSSeriesModifiedAfterUpsert.Description = "This is our new schema series. (upd)";
        basicSSeriesModifiedAfterUpsert.Type = new List<short>() {1, 2};
        Add(basicSSeriesModifiedAfterUpsert, SSeriesLabel.BasicSchemaSeries, VariantLabel.ModifiedAfterUpsert);
    }
}
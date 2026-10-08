using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Application.DataTemplateManager.Dtos;
using DoorCEServer.Tests.CataloguingManager.Labels;

namespace DoorCEServer.Tests.CataloguingManager.MockDtos;

public class XDistributionMock
{
    private readonly Dictionary<DistrLabel, List<Dictionary<VariantLabel, XDistribution>>> _xDistributions;
    private readonly TestCommon _testCommon;
    
    private readonly XDatasetMock _datasetMock;
    
    public XDistributionMock()
    { 
        _xDistributions = new();
        _testCommon = new TestCommon();
        
        _datasetMock = new XDatasetMock();
        
        Setup();
    }
    
    public XDistribution Get(DistrLabel distributionLabel, VariantLabel variantLabel)
    {
        try
        {
            var distrList = _xDistributions[distributionLabel];
            var dictWithKey = distrList.First(d => d.ContainsKey(variantLabel));
            return dictWithKey[variantLabel];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new Exception($"Mock distribution not found: {distributionLabel}, {variantLabel}");
        }
    }
    
    private void Add(XDistribution xDistribution, DistrLabel distrLabel, VariantLabel variantLabel)
    {
        if (_xDistributions.ContainsKey(distrLabel))
        {
            _xDistributions[distrLabel].Add(new Dictionary<VariantLabel, XDistribution>{{variantLabel, xDistribution}});
        }
        else
        {
            _xDistributions.Add(distrLabel, [new Dictionary<VariantLabel, XDistribution>{{variantLabel, xDistribution}}]);
        }
    }

    private void Setup()
    {
        // create datasets, *data services, *schemas
        XDataset basicDataset = _datasetMock.Get(DatasetLabel.BasicDataset, VariantLabel.AfterUpsert);
        XDataset basicDatasetModified = _datasetMock.Get(DatasetLabel.BasicDataset, VariantLabel.ModifiedAfterUpsert);

        // create distributions and add them to dict
        XDistribution basicDistrBeforeUpsert = _testCommon.GetMockXDistribution(
            "dis/full_tree_data",
            new Dictionary<string, string> { { "en", "New distribution" }, { "pl", "Nowa dystrybucja" }, { "it", "Nuova distribuzione" } },
            basicDataset.Uri!
        );
        Add(basicDistrBeforeUpsert, DistrLabel.BasicDistr, VariantLabel.BeforeUpsert);

        var basicDistrAfterUpsert = _testCommon.GetMockXDistribution(
            "dis/full_tree_data",
            new Dictionary<string, string> { { "en", "New distribution" }, { "pl", "Nowa dystrybucja" }, { "it", "Nuova distribuzione" } },
            basicDataset.Uri!
        );
        basicDistrAfterUpsert.DatasetTitle = basicDataset.Title;
        basicDistrAfterUpsert.DatasetIconUri = basicDataset.IconUri;
        Add(basicDistrAfterUpsert, DistrLabel.BasicDistr, VariantLabel.AfterUpsert);

        var basicDistrModifiedBeforeUpsert = BasicDistrModifiedBeforeUpsert(basicDatasetModified);
        Add(basicDistrModifiedBeforeUpsert, DistrLabel.BasicDistr, VariantLabel.ModifiedBeforeUpsert);

        var basicDistrModifiedAfterUpsert = BasicDistrModifiedBeforeUpsert(basicDatasetModified);
        basicDistrModifiedAfterUpsert.DatasetTitle = basicDatasetModified.Title;
        basicDistrModifiedAfterUpsert.DatasetIconUri = basicDatasetModified.IconUri;
        Add(basicDistrModifiedAfterUpsert, DistrLabel.BasicDistr, VariantLabel.ModifiedAfterUpsert);
    }

    private XDistribution BasicDistrModifiedBeforeUpsert(XDataset dataset, XDataService? service=null, XDataSchema? schema=null)
    {
        XDistribution xDistribution = _testCommon.GetMockXDistribution(
            "dis/full_tree_data",
            new Dictionary<string, string> { { "en", "New distribution (upd)" }, { "pl", "Nowa dystrybucja (upd)" }, { "it", "Nuova distribuzione (upd)" } },
            dataset.Uri!,
            service?.Uri
        );

        xDistribution.AccessUrl = new List<string>() { "https://doorce.com/distributions/new-distrib1" };
        xDistribution.Format = "app/json";
        xDistribution.AccessStatus = 1;
        xDistribution.ByteSize = 12030;
        xDistribution.ReleaseDate = DateTime.Today.AddDays(9).ToUniversalTime();
        xDistribution.SchemaUri = schema?.Uri;
        xDistribution.DatasetIconUri = dataset.IconUri;

        return xDistribution;
    }
}
using DoorCEServer.Tests.CataloguingManager.Labels;
using DoorCEServer.Tests.CataloguingManager.TestHelpers;

namespace DoorCEServer.Tests.CataloguingManager.Tests;

public class MDatasetMgmtTest(MDatasetMgmtTestHelper testHelper)
{
    public bool InsertCatalogueBasicTest(bool cleanupAllowed = true)
    {
        var cat = CatLabel.BasicCat;
        var org = OrgLabel.BasicOrg;
        var person = PersonLabel.PersonWithOrg;
        
        return testHelper.CatInsertBasicTest(cat, org, person, cleanupAllowed);
    }

    public bool UpdateCatalogueBasicTest(bool cleanupAllowed = true)
    {
        var cat = CatLabel.BasicCat;
        var org = OrgLabel.BasicOrg;
        var person = PersonLabel.PersonWithOrg;

        return testHelper.CatUpdateBasicTest(cat, org, person, cleanupAllowed);
    }
    
    public bool InsertDatasetSeriesBasicTest(bool cleanupAllowed = true)
    {
        var dseries = DSeriesLabel.DSeriesWithCat;
        var org = OrgLabel.BasicOrg;
        var person = PersonLabel.PersonWithOrg;
        var cat = CatLabel.BasicCat;
        
        return testHelper.DSeriesInsertBasicTest(dseries, cat, org, person, cleanupAllowed);
    }

    public bool UpdateDatasetSeriesWithCatalogueBasicTest(bool cleanupAllowed = true)
    {
        var dseries = DSeriesLabel.DSeriesWithCat;
        var org = OrgLabel.BasicOrg;
        var person = PersonLabel.PersonWithOrg;
        var cat = CatLabel.BasicCat;

        return testHelper.DSeriesUpdateBasicTest(dseries, cat, org, person, cleanupAllowed);
    }
    
    public bool InsertDatasetWithSeriesBasicTest(bool cleanupAllowed = true)
    {
        var dataset = DatasetLabel.DatasetWithSeries;
        var org = OrgLabel.BasicOrg;
        var person = PersonLabel.PersonWithOrg;
        var cat = CatLabel.BasicCat;
        var dseries = DSeriesLabel.DSeriesWithCat;
        
        return testHelper.DatasetInsertBasicTest(dataset, cat, org, person, dseries, cleanupAllowed: cleanupAllowed);
    }

    public bool UpdateDatasetWithSeriesBasicTest(bool cleanupAllowed = true)
    {
        var dataset = DatasetLabel.DatasetWithSeries;
        var org = OrgLabel.BasicOrg;
        var person = PersonLabel.PersonWithOrg;
        var cat = CatLabel.BasicCat;
        var dseries = DSeriesLabel.DSeriesWithCat;
        

        return testHelper.DatasetUpdateBasicTest(dataset, cat, org, person, dseries, cleanupAllowed: cleanupAllowed);
    }

    public bool DeleteCatalogueTest_Empty(bool cleanupAllowed = true)
    {
        var cat = CatLabel.BasicCat;
        var org = OrgLabel.BasicOrg;
        var person = PersonLabel.PersonWithOrg;
        
        return testHelper.DeleteCatalogueTest_Empty(cat, org, person, cleanupAllowed);
    }
    
    public bool DeleteCatalogueTest_WithChild(bool cleanupAllowed = true)
    {
        var cat = CatLabel.BasicCat;
        var org = OrgLabel.BasicOrg;
        var person = PersonLabel.PersonWithOrg;
        
        return testHelper.DeleteCatalogueTest_WithChild(cat, org, person, cleanupAllowed);
    }
    
    public bool DeleteDatasetSeriesWithCatalogueTest(bool cleanupAllowed = true)
    {
        var dseries = DSeriesLabel.DSeriesWithCat;
        var org = OrgLabel.BasicOrg;
        var person = PersonLabel.PersonWithOrg;
        var cat = CatLabel.BasicCat;
        
        return testHelper.DeleteDSeriesBasicTest(dseries, cat, org, person, cleanupAllowed: cleanupAllowed);
    }

    public bool DeleteDatasetBasicTest(bool cleanupAllowed = true)
    {
        var dataset = DatasetLabel.BasicDataset;
        var org = OrgLabel.BasicOrg;
        var person = PersonLabel.PersonWithOrg;
        var cat = CatLabel.BasicCat;
        
        return testHelper.DeleteDatasetTest(dataset, cat, org, person, cleanupAllowed: cleanupAllowed);
    }

    public bool DeleteCatalogueTest_WithResources(bool cleanupAllowed = true)
    {
        var dataset = DatasetLabel.BasicDataset;
        var org = OrgLabel.BasicOrg;
        var person = PersonLabel.PersonWithOrg;
        var cat = CatLabel.BasicCat;

        return testHelper.DeleteCatTest_WithResources(dataset, cat, org, person, cleanupAllowed);
    }
}
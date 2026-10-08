using DoorCEServer.Tests.CataloguingManager.Labels;
using DoorCEServer.Tests.CataloguingManager.TestHelpers;

namespace DoorCEServer.Tests.CataloguingManager.Tests;

public class MDistrMgmtTest(MDistrMgmtTestHelper testHelper)
{
    public bool InsertDataServiceBasicTest(bool cleanupAllowed = true)
    {
        var service = ServiceLabel.BasicService;
        var org = OrgLabel.BasicOrg;
        var person = PersonLabel.PersonWithOrg;
        var cat = CatLabel.BasicCat;
        
        return testHelper.ServiceInsertBasicTest(service, cat, org, person, cleanupAllowed);
    }

    public bool UpdateDataServiceBasicTest(bool cleanupAllowed = true)
    {
        var service = ServiceLabel.BasicService;
        var org = OrgLabel.BasicOrg;
        var person = PersonLabel.PersonWithOrg;
        var cat = CatLabel.BasicCat;

        return testHelper.ServiceUpdateBasicTest(service, cat, org, person, cleanupAllowed);
    }
    
    public bool InsertDistributionBasicTest(bool cleanupAllowed = true)
    {
        var distr = DistrLabel.BasicDistr;
        
        var org = OrgLabel.BasicOrg;
        var person = PersonLabel.PersonWithOrg;
        var dataset = DatasetLabel.BasicDataset;
        var cat = CatLabel.BasicCat;
        
        return testHelper.DistrInsertBasicTest(distr, cat, dataset, org, person, cleanupAllowed: cleanupAllowed);
    }

    public bool UpdateDistributionBasicTest(bool cleanupAllowed = true)
    {
        var distr = DistrLabel.BasicDistr;

        var org = OrgLabel.BasicOrg;
        var person = PersonLabel.PersonWithOrg;
        var dataset = DatasetLabel.BasicDataset;
        var cat = CatLabel.BasicCat;

        return testHelper.DistrUpdateBasicTest(distr, dataset, cat, org, person, cleanupAllowed: cleanupAllowed);
    }

    public bool DeleteDataServiceBasicTest(bool cleanupAllowed = true)
    {
        var service = ServiceLabel.BasicService;
        var org = OrgLabel.BasicOrg;
        var person = PersonLabel.PersonWithOrg;
        var cat = CatLabel.BasicCat;

        return testHelper.DeleteServiceTest(service, cat, org, person, cleanupAllowed: cleanupAllowed);
    }
    
    public bool DeleteDistributionBasicTest(bool cleanupAllowed = true)
    {
        var distr = DistrLabel.BasicDistr;
        var org = OrgLabel.BasicOrg;
        var person = PersonLabel.PersonWithOrg;
        var dataset = DatasetLabel.BasicDataset;
        var cat = CatLabel.BasicCat;
        
        return testHelper.DeleteDistrTest(distr, dataset, cat, org, person, cleanupAllowed: cleanupAllowed);
    }
}
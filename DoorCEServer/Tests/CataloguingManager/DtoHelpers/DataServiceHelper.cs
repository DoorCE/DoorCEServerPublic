using DoorCEModel.Infrastructure;
using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Application.CataloguingManager.Interfaces;
using DoorCEServer.Common.Exceptions;
using DoorCEServer.Tests.CataloguingManager.Labels;
using DoorCEServer.Tests.CataloguingManager.MockDtos;

namespace DoorCEServer.Tests.CataloguingManager.DtoHelpers;

public class DataServiceHelper(ApplicationDbContext context, DistributionManagementAPI distributionApi,
    TestCommon testCommon)
{
    private readonly XDataServiceMock _xServiceMock = new();
    
    public XDataService? GetFromDb(ServiceLabel serviceLabel)
    {
        XDataService xService = _xServiceMock.Get(serviceLabel, VariantLabel.AfterUpsert);
        try {
            return distributionApi.GetDataService(xService.Uri!, "udas-admin");
        } catch (NotFoundOrVisibleException) { return null; }
    }

    public void Upsert(ServiceLabel serviceLabel, VariantLabel variantLabel, ContactsLabel contactsLabel)
    {
        XDataService xService = _xServiceMock.Get(serviceLabel, variantLabel);
        List<XContactData> xContacts = _xServiceMock.GetContacts(serviceLabel, contactsLabel);
        
        distributionApi.UpsertDataService(xService, xContacts,"udas-admin");
    }

    public void Delete(ServiceLabel serviceLabel)
    {
        XDataService xService = _xServiceMock.Get(serviceLabel, VariantLabel.AfterUpsert);
        distributionApi.DeleteDataService(xService.Uri!, "udas-admin");
    }
    
    public bool ValidateDelete(ServiceLabel serviceLabel, VariantLabel variantBeforeDelete)
    {
        XDataService serviceBeforeDelete = _xServiceMock.Get(serviceLabel, variantBeforeDelete);
        // check if service was deleted
        if (!context.DataServices.Any(ds => ds.Uri == serviceBeforeDelete.Uri)) {
            // check if contacts were deleted
            return serviceBeforeDelete.ContactsUris
                .All(contactUri => !context.ContactDatas.Any(c => c.Uri == contactUri));
        }
        return false;
    }

    public bool Validate(XDataService xService, ServiceLabel serviceLabel, VariantLabel variantToCompare)
    {
        return testCommon.CheckDataServiceEquality(xService, _xServiceMock.Get(serviceLabel, variantToCompare));
    }
}
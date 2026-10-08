using DoorCEModel.Infrastructure;
using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Application.CataloguingManager.Interfaces;
using DoorCEServer.Common.Exceptions;
using DoorCEServer.Tests.CataloguingManager.Labels;
using DoorCEServer.Tests.CataloguingManager.MockDtos;

namespace DoorCEServer.Tests.CataloguingManager.DtoHelpers;

public class CatalogueHelper(ApplicationDbContext context, DatasetManagementAPI datasetApi, TestCommon testCommon)
{
    private readonly XCatalogueMock _xCatMock = new();
    
    public XCatalogue? GetFromDb(CatLabel catalogueLabel)
    {
        XCatalogue xCatalogue = _xCatMock.Get(catalogueLabel, VariantLabel.AfterUpsert);
        try
        {
            return datasetApi.GetCatalogue(xCatalogue.Uri!, "udas-admin");
        } catch (NotFoundOrVisibleException) { return null; }
    }

    public void Upsert(CatLabel catalogueLabel, VariantLabel variantLabel, ContactsLabel contactsLabel)
    {
        XCatalogue xCatalogue = _xCatMock.Get(catalogueLabel, variantLabel);
        List<XContactData> xContacts = _xCatMock.GetContacts(catalogueLabel, contactsLabel);
        
        datasetApi.UpsertCatalogue(xCatalogue, xContacts, "udas-admin");
    }

    public void Delete(CatLabel catalogueLabel)
    {
        XCatalogue xCatalogue = _xCatMock.Get(catalogueLabel, VariantLabel.AfterUpsert);
        datasetApi.DeleteCatalogue(xCatalogue.Uri!, "udas-admin");
    }
    
    public bool ValidateDelete(CatLabel catLabel, VariantLabel variantLabel)
    {
        XCatalogue catBeforeDelete = _xCatMock.Get(catLabel, variantLabel);
        
        // check if catalogue was deleted
        if (!context.Catalogues.Any(c => c.Uri == catBeforeDelete.Uri)) {
            // check if contacts were deleted
            return catBeforeDelete.ContactsUris.All(contactUri 
                => !context.ContactDatas.Any(c => c.Uri == contactUri));
        }

        return false;
    }

    public bool Validate(XCatalogue xCatalogue, CatLabel catalogueLabel, VariantLabel variantLabel)
    {
        return testCommon.CheckCatalogueEquality(xCatalogue, _xCatMock.Get(catalogueLabel, variantLabel));
    }
}
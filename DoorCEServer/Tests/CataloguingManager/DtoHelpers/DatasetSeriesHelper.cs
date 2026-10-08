using DoorCEModel.Infrastructure;
using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Application.CataloguingManager.Interfaces;
using DoorCEServer.Common.Exceptions;
using DoorCEServer.Tests.CataloguingManager.Labels;
using DoorCEServer.Tests.CataloguingManager.MockDtos;

namespace DoorCEServer.Tests.CataloguingManager.DtoHelpers;

public class DatasetSeriesHelper(ApplicationDbContext context, DatasetManagementAPI datasetApi, TestCommon testCommon)
{
    private readonly XDatasetSeriesMock _xDSeriesMock = new();
    
    public XDatasetSeries? GetFromDb(DSeriesLabel seriesLabel)
    {
        XDatasetSeries xSeries = _xDSeriesMock.Get(seriesLabel, VariantLabel.AfterUpsert);
        try {
            return datasetApi.GetSeries(xSeries.Uri!, "udas-admin");
        } catch (NotFoundOrVisibleException) { return null; }
    }

    public void Upsert(DSeriesLabel seriesLabel, VariantLabel variantLabel, ContactsLabel contactsLabel)
    {
        XDatasetSeries xSeries = _xDSeriesMock.Get(seriesLabel, variantLabel);
        List<XContactData> xContacts = _xDSeriesMock.GetContacts(seriesLabel, contactsLabel);
        
        datasetApi.UpsertSeries(xSeries, xContacts,"udas-admin");
    }

    public void Delete(DSeriesLabel seriesLabel)
    {
        XDatasetSeries xSeries = _xDSeriesMock.Get(seriesLabel, VariantLabel.AfterUpsert);
        datasetApi.DeleteSeries(xSeries.Uri!, "udas-admin");
    }

    public bool ValidateDelete(DSeriesLabel seriesLabel, VariantLabel variantBeforeDelete)
    {
        XDatasetSeries dseriesBeforeDelete = _xDSeriesMock.Get(seriesLabel, variantBeforeDelete);
        
        // check if agent was deleted
        if (!context.DatasetSeries.Any(ds => ds.Uri == dseriesBeforeDelete.Uri)) {
            // check if contacts were deleted
            return dseriesBeforeDelete.ContactsUris.All(contactUri
                => !context.ContactDatas.Any(c => c.Uri == contactUri));
        }

        return false;
    }

    public bool Validate(XDatasetSeries xSeries, DSeriesLabel seriesLabel, VariantLabel variantToCompare)
    {
        return testCommon.CheckDatasetSeriesEquality(xSeries, _xDSeriesMock.Get(seriesLabel, variantToCompare));
    }
}
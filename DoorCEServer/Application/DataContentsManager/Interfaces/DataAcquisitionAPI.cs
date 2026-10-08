using DoorCEServer.Application.DataContentsManager.Dtos;

namespace DoorCEServer.Application.DataContentsManager.Interfaces;

/// <summary>
/// The interface dedicated to be used by the Acquisition Apps for the management
/// of structured data within Datasets. It offers CRUD operations on source
/// (working copies of ) Dataset Items and publishing these working copies into
/// active ones.
/// </summary>
public interface DataAcquisitionAPI
{
    IEnumerable<XDataItem> GetDatasetItemList(string datasetUri, string? conceptUri, XDataFilterParams? criteria,
        string userId);
    XDataItem GetDataItem(string identifier, string datasetUri, string? conceptUri, string userId);
    void UpsertDataItem(XDataItem xDataItem, string userId);
    void DeleteDataItem(string identifier, string datasetUri, string? conceptUri, string userId);
    int CheckDataItem(XDataItem xDataItem, string userId);
    int CheckExistingDataItem(string identifier, string datasetUri, string? conceptUri, string userId);
    void SubmitSourceDatasetContents(string datasetUri, string userId);
}
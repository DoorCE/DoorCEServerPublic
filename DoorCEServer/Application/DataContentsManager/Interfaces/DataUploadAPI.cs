using DoorCEServer.Application.DataContentsManager.Dtos;

namespace DoorCEServer.Application.DataContentsManager.Interfaces;

public interface DataUploadAPI
{
    XFileValidationResult UploadData(XDataUploadRequest request, string? userId, IFormFile? file=null);
    void ClearDataItemsByDatasetUri(string datasetUri, string? userId);
    Dictionary<string, int> GetDataItemCountsByConcept(string datasetUri);
}
using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEModel.Infrastructure.DataModel.Datasets;
using DoorCEServer.Application.CkanProxy.Dtos;

namespace DoorCEServer.Application.CkanProxy.Interfaces;

public interface ICkanActions
{
    public Task<XCkanResponse> PublishMetadata(Dataset dataset, Organisation organisation,
        bool ensureDatastoreExists);
    public Task<XCkanResponse> PublishMetadataWithData(Dataset dataset, Organisation organisation);
    public XCkanResponse UnpublishDataset(Dataset dataset);
}
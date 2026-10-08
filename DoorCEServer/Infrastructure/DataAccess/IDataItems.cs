using DoorCEModel.Infrastructure.DataAccess;
using DoorCEModel.Infrastructure.DataModel.DatasetContents;

namespace DoorCEServer.Infrastructure.DataAccess;

public interface IDataItems : ITransactionalAccess
{
    List<DataItem> GetDataItems(string datasetUri, string conceptUri);
}
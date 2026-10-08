using DoorCEModel.Infrastructure;
using DoorCEModel.Infrastructure.DataAccess;
using DoorCEModel.Infrastructure.DataModel.DatasetContents;
using Microsoft.EntityFrameworkCore;

namespace DoorCEServer.Infrastructure.DataAccess;

public class MDataItems(ApplicationDbContext context) : MDbTransactionalAccess(context), IDataItems
{
    public List<DataItem> GetDataItems(string datasetUri, string conceptUri)
    {
        return context.DataItems
            .Include(di => di.Dataset)
            .Include(di => di.Concept)
            .Include(di => di.Concept.Schema)
            .Include(di => di.Concept.Schema!.DefaultNamespace)
            .Include(di => di.Concept.Namespace)
            .Include(di => di.Source)
            .AsSplitQuery()
            .Where(di => di.Dataset.Uri == datasetUri && di.ConceptUri == conceptUri)
            .ToList();
    }
}
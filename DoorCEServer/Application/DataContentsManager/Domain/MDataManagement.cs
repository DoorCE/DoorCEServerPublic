using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEModel.Infrastructure.DataModel.Datasets;
using DoorCEServer.Application.CataloguingManager.Domain;

namespace DoorCEServer.Application.DataContentsManager.Domain;

public abstract class MDataManagement(MAgentsCommon agents)
{
    protected void CheckDataModificationAuthorisation(Dataset dataset, string? userId)
    {
        // ===> Authorisation
        agents.CheckVisibilityAndSetRoles(dataset, userId);
        if (!dataset.HasDistributionRole())
            throw new UnauthorizedAccessException("This user has no privilege to modify data in this dataset");
    }
    
    protected void CheckDataRetrievalAuthorisation(Dataset dataset, string? userId)
    {
        // ===> Authorisation
        if (!agents.CheckVisibilityAndSetRoles(dataset, userId))
            throw new UnauthorizedAccessException("This user has no privilege to read data from this dataset");
    }
}
using DoorCEModel.Infrastructure;
using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Application.CataloguingManager.Interfaces;
using DoorCEServer.Common.Exceptions;
using DoorCEServer.Tests.CataloguingManager.Labels;
using DoorCEServer.Tests.CataloguingManager.MockDtos;

namespace DoorCEServer.Tests.CataloguingManager.DtoHelpers;

public class DistributionHelper(ApplicationDbContext context, DistributionManagementAPI distributionApi, TestCommon testCommon)
{
    private readonly XDistributionMock _xDistrMock = new();
    
    public XDistribution? GetFromDb(DistrLabel distributionLabel)
    {
        XDistribution xDistribution = _xDistrMock.Get(distributionLabel, VariantLabel.AfterUpsert);
        try {
            return distributionApi.GetDistribution(xDistribution.Uri!, "udas-admin");
        } catch (NotFoundOrVisibleException) { return null; }
    }

    public void Upsert(DistrLabel distributionLabel, VariantLabel variantLabel)
    {
        XDistribution xDistribution = _xDistrMock.Get(distributionLabel, variantLabel);
        
        distributionApi.UpsertDistribution(xDistribution, "udas-admin");
    }

    public void Delete(DistrLabel distributionLabel)
    {
        XDistribution xDistribution = _xDistrMock.Get(distributionLabel, VariantLabel.AfterUpsert);
        distributionApi.DeleteDistribution(xDistribution.Uri!, "udas-admin");
    }

    public bool ValidateDelete(DistrLabel distrLabel)
    {
        XDistribution distribution = _xDistrMock.Get(distrLabel, VariantLabel.AfterUpsert);

        // check if distribution was deleted
        return !context.Distributions.Any(d => d.Uri == distribution.Uri);
    }

    public bool Validate(XDistribution xDistribution, DistrLabel distributionLabel, VariantLabel variantToCompare)
    {
        return testCommon.CheckDistributionEquality(xDistribution, _xDistrMock.Get(distributionLabel, variantToCompare));
    }
}
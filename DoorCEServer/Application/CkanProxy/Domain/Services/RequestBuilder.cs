using AutoMapper;
using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEModel.Infrastructure.DataModel.DatasetContents;
using DoorCEModel.Infrastructure.DataModel.Datasets;
using DoorCEServer.Application.CkanProxy.Common;
using DoorCEServer.Application.CkanProxy.Dtos;

namespace DoorCEServer.Application.CkanProxy.Domain.Services;

public class RequestBuilder(IMapper mapper) {
    private static XCkanObject BuildGetCkanObject(string ckanObjectId) { // or name (package/org)
        return new XCkanObject {
            CkanId = ckanObjectId
        };
    }

    public static XCkanObject BuildGetPackage(Dataset dataset)
        => BuildGetCkanObject(MCkanProxyCommon.CreateCkanName(dataset));

    public static XCkanObject BuildGetOrganisation(Organisation org)
        => BuildGetCkanObject(MCkanProxyCommon.CreateCkanName(org));

    public static XCkanObject BuildDeleteCkanObject(string ckanObjectId) {
        return new XCkanObject {
            CkanId = ckanObjectId
        };
    }
    
    public XCkanOrganisation BuildUpsertOrganisation(Organisation org, bool update) {
        XCkanOrganisation xCkanOrganisation = mapper.Map<XCkanOrganisation>(org);
        string orgName = MCkanProxyCommon.CreateCkanName(org);
        if (update) {
            xCkanOrganisation.CkanId = orgName;
        } else {
            xCkanOrganisation.CkanName = orgName;
        }
        return xCkanOrganisation;
    }

    public XCkanPackage BuildUpsertPackage(Dataset dataset, string organisationId, bool update, 
        IConfiguration configuration, ICollection<XCkanResource>? dataResources = null) {
        
        XCkanPackage xCkanPackage = mapper.Map<XCkanPackage>(dataset);
        xCkanPackage.OrganisationId = organisationId;
        
        string packageName = MCkanProxyCommon.CreateCkanName(dataset);
        if (update) {
            xCkanPackage.CkanId = packageName;
        } else {
            xCkanPackage.CkanName = packageName;
        }
        
        xCkanPackage.Resources = dataset.Distributions
            .Select(d => BuildUpsertDistribution(d, packageName, configuration)).ToList();
        
        // add not mapped structured data resources
        if (dataResources != null) {
            xCkanPackage.Resources.AddRange(dataResources);
        }
        
        return xCkanPackage;
    }
    
    public static XCkanResource BuildUpsertCkanStructDataResource(Dataset dataset, Concept  concept) {
        return new XCkanResource {
            CkanId = null,
            PackageId = MCkanProxyCommon.CreateCkanName(dataset),
            CkanName = MCkanProxyCommon.CreateCkanName(concept)
        };
    }

    public XCkanResource BuildUpsertDistribution(Distribution distribution, string packageId,
        IConfiguration configuration, string? ckanId = null) {
        XCkanResource xDistribution = mapper.Map<XCkanResource>(distribution);
        
        xDistribution.PackageId = packageId;
        xDistribution.CkanId = ckanId ?? null;
        xDistribution.DownloadUrl = null != distribution.FileId 
                ? MCkanProxyCommon.GetUdasFileDistributionUrl(configuration, distribution.FileId)
                : distribution.DownloadUrl;
        xDistribution.Url = null != distribution.FileId
            ? MCkanProxyCommon.GetUdasFileDistributionUrl(configuration, distribution.FileId)
            : !string.IsNullOrEmpty(distribution.DownloadUrl)
                ? distribution.DownloadUrl
                : distribution.AccessUrl.FirstOrDefault();
        
        return xDistribution;
    }

    public static XDatastoreRequest BuildGetTable(string tableId) {
        return new XDatastoreRequest {
            ResourceId = tableId,
        };
    }

    public XDatastoreResource BuildUpsertEmptyTable(string resourceId, Dataset dataset, Concept concept) {
        XDatastoreResource resource = mapper.Map<XDatastoreResource>(concept);
        // add primary key field if it does not exist
        if (resource.Fields.All(f => f.Name != resource.PrimaryKey.First()) )
        {
            resource.Fields.Add(new XField
            {
                Name = resource.PrimaryKey.First(),
                Type = "text"
            });
        }
        resource.Aliases = new List<string>() { MCkanProxyCommon.CreateDatastoreAlias(dataset.Uri, concept.Name) };
        resource.ResourceId = resourceId;
        resource.Force = true;
        return resource;
    }

    public static XDatastoreRequest BuildDeleteTable(string resourceId) {
        return new XDatastoreRequest {
            ResourceId = resourceId,
            Force = true
        };
    }

    public XDatastoreRequest BuildUpsertRecords(string resourceId, List<DataItem> dataItems) {
        return new XDatastoreRequest {
            ResourceId = resourceId,
            Force = true,
            Records = mapper.Map<List<Dictionary<string, object?>>>(dataItems)
        };
    }
    
}
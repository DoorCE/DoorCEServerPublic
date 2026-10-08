using System.Collections.ObjectModel;
using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEModel.Infrastructure.DataModel.Datasets;
using DoorCEServer.Application.CkanProxy.Common;
using DoorCEServer.Application.CkanProxy.Dtos;

namespace DoorCEServer.Application.CkanProxy.Domain.Services;

public class CkanMetadataService(CkanClient client, RequestBuilder reqBuilder, IConfiguration configuration) {

    // returns null if package not found
    public async Task<string?> GetPackageId(Dataset dataset) {
        XCkanResponse response = await GetPackage(dataset);
        if (!response.IsSuccess) return null;
        return response.ExtractObjectId();
    }
    
    // returns null if org not found
    private async Task<string?> GetOrganisationId(Organisation org) {
        XCkanResponse response = await GetOrganisation(org);
        if (!response.IsSuccess) return null;
        return response.ExtractObjectId();
    }

    public async Task<ICollection<XCkanResource>?> GetPackageResources(Dataset dataset) {
        XCkanResponse response = await GetPackage(dataset);
        if (!response.IsSuccess) return null;
        return response.ExtractResources();
    }
    
    public async Task<XCkanResponse> GetPackage(Dataset dataset) {
        XCkanObject request = RequestBuilder.BuildGetPackage(dataset);
        return await client.SendAsync(CkanActionNames.PackageShow, request);
    }

    private async Task<XCkanResponse> GetOrganisation(Organisation org) {
        XCkanObject request = RequestBuilder.BuildGetOrganisation(org);
        return await client.SendAsync(CkanActionNames.OrganizationShow, request);
    }

    public async Task<XCkanResponse> CreateStructuredDataResource(Dataset dataset, Concept concept) {
        XCkanResource request = RequestBuilder.BuildUpsertCkanStructDataResource(dataset, concept);
        return await client.SendAsync(CkanActionNames.ResourceCreate, request);
    }
    
    // Create or update distribution
    public async Task<XCkanResponse> UpsertDistribution(Distribution distribution, string? packageId)
    {
        // Make sure the dataset exists and get its id (as current dataset id)
        if (null == packageId) {
            packageId = await GetPackageId(distribution.Dataset);
            if (null == packageId) 
                throw new ArgumentException($"Package {MCkanProxyCommon.CreateCkanName(distribution.Dataset)} not found");
        }
        
        // TODO - implement update
        
        // Create distribution
        XCkanResource requestBody = reqBuilder.BuildUpsertDistribution(distribution, packageId, configuration);
        XCkanResponse ckanDistributionResult = await client.SendAsync(CkanActionNames.ResourceCreate, requestBody);
        
        return ckanDistributionResult;
    }
    
    // Upsert organisation and return its id (null if failed)
    private async Task<XCkanResponse> UpsertOrganisation(Organisation organisation)
    {
        // Check if organisation exists
        XCkanResponse result = await GetOrganisation(organisation);
        
        // Create organisation if it does not exist; else update it
        bool update;
        string action;
        if (result.IsSuccess) { update = true; action = CkanActionNames.OrganizationUpdate; }
        else { update = false; action = CkanActionNames.OrganizationCreate; }
        
        // Create or update organisation
        XCkanOrganisation requestBody = reqBuilder.BuildUpsertOrganisation(organisation, update);
        result = await client.SendAsync(action, requestBody);
        return result;
    }
    
    // Create or update dataset and return status code (0 - created, 1 - updated, -1 - failed)
    public async Task<XCkanResponse> UpsertDataset(Dataset dataset, Organisation organisation)
    {
        // Check if organisation exists; create if not
        string? orgId = await GetOrganisationId(organisation);
        if (null == orgId) {
            var res = await UpsertOrganisation(organisation); // TODO - add error logging!
            if (!res.IsSuccess) {
                return res;
            }
            orgId = res.ExtractObjectId();
        }
        
        // Check if dataset exists
        XCkanResponse ckanPackageResult = await GetPackage(dataset);
        
        // Create dataset if it does not exist; else update it
        if (!ckanPackageResult.IsSuccess) {
            XCkanPackage requestBody = reqBuilder.BuildUpsertPackage(dataset, orgId!, false, configuration);
            return await client.SendAsync(CkanActionNames.PackageCreate, requestBody);
        } else {
            // preserve ckan data resources (to be linked with datastore) and other non-udas resources (not modified here)
            
            // filter out Udas distributions
            ICollection<XCkanResource> dataResources = ckanPackageResult.ExtractResources() ?? new Collection<XCkanResource>();
            
            // add rest to request
            XCkanPackage requestBody = reqBuilder.BuildUpsertPackage(dataset, orgId!, true, configuration, dataResources);
            return await client.SendAsync(CkanActionNames.PackageUpdate, requestBody);
        }
    }
    
    public async Task<XCkanResponse?> PurgeCkanPackage(Dataset dataset) {
        // get Ckan package id
        string? ckanPackageId = await GetPackageId(dataset);

        // return null if it does not exist
        if (null == ckanPackageId) {
            return null;
        }
        
        // delete Ckan package
        XCkanObject packageBody = RequestBuilder.BuildDeleteCkanObject(ckanPackageId);
        XCkanResponse packageResult = await client.SendAsync(CkanActionNames.DatasetPurge, packageBody);
        
        return !packageResult.IsSuccess 
            ? throw new ArgumentException($"Failed to delete CKAN package with id {ckanPackageId}") : packageResult;
    }
    
    public async Task<XCkanResponse> DeleteCkanResource(string resourceId)
    {
        XCkanObject request = RequestBuilder.BuildDeleteCkanObject(resourceId);
        return await client.SendAsync(CkanActionNames.ResourceDelete, request);
    }

    // public async Task<XCkanResponse> DeleteCkanOrganization(string organizationId) {
    //     XCkanObject request = RequestBuilder.BuildDeleteCkanObject(organizationId);
    //     return await client.SendAsync(CkanActionNames.OrganizationDelete, request);
    // }

}
using DoorCEServer.Application.DataTemplateManager.Dtos;

namespace DoorCEServer.Application.DataTemplateManager.Interfaces;

public interface AppTemplateAPI
{
	// Application Template operations
	IEnumerable<XAppTemplate> GetAppTemplateList(string userId);
	XAppTemplate GetAppTemplate(string templateUri, string userId);
	string UpsertAppTemplate(XAppTemplate xTemplate, string userId);
	void DeleteAppTemplate(string templateUri, string userId);
	public bool CheckAppTemplateId(string identifier);
	public string GenerateAppTemplate(string templateUri, string userId);


	// Acquisition App operations
	IEnumerable<XAcquisitionApp> GetAppList(string userId, bool forUsage);
	XAcquisitionApp GetApp(string appUri, string userId, bool forUsage);
	string UpsertApp(XAppCreationRequest xApp, string userId);
	void DeleteApp(string appUri, string userId);
	public bool CheckAppId(string identifier);
	public void DeployApp(string appUri, string userId);

	// Auxiliary operations
	XDefaultAppTemplate GetDefaultAppTemplate(string schemaId, string? conceptName, string? language);
	IEnumerable<XAcquisitionApp> GetDatasetApps(string datasetUri, string userId);
	XAcquisitionApp? GetDefaultDatasetApp(string datasetUri, string userId);
}
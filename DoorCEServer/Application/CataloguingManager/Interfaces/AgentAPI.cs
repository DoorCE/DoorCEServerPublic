using DoorCEServer.Application.CataloguingManager.Dtos;

namespace DoorCEServer.Application.CataloguingManager.Interfaces;

/// <summary>
/// Descriptions can be found in the WebApi/Controllers/AgentController.cs
/// </summary>
public interface AgentAPI
{
	IEnumerable<XOrganisation> GetAllowedOrganisations(string? personId, string userId);
	IEnumerable<XPerson> GetAllowedPersonsForAccount(string? accountId, string userId);
	XAgent GetAgent(string? identifier, string? userId);
	bool CheckAgentId(string identifier);
	string UpsertOrganisation(XOrganisation xOrganisation, IEnumerable<XContactData> newContacts, string userId);
	string UpsertPerson(XPerson xPerson, IEnumerable<XContactData> newContacts, string userId);
	void DeleteAgent(string identifier, string userId);
	IEnumerable<XPerson> GetPersonList(string? query, string? userId);
	IEnumerable<XOrganisation> GetOrganisationList(string? query, string? userId);
	IEnumerable<XPerson> GetPersonsInOrganisation(string identifier, string? userId);
	XAgents GetResourceEditors(string identifier, string? userId);
	IEnumerable<XContactData> GetResourceContacts(string identifier, string? userId);
	IEnumerable<XContactData> GetAgentContacts(string identifier, string? userId);
	IEnumerable<XContactData> GetMultipleAgentContacts(IEnumerable<string> agentUris, string? userId);
	IEnumerable<XUserAccount> GetUserAccountList(string? query, string userId);
	void UpsertUserAccount(XUserAccount xUserAccount, string userId);
	void DeleteUserAccount(string accountId, string userId);
	XUserAccount GetUserAccount(string? accountId, string userId);
	IEnumerable<string> GetAvailableAccountIds(string query, string userId);
}
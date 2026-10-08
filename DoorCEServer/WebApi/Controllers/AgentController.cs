using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Application.CataloguingManager.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace DoorCEServer.WebApi.Controllers;

/// <summary>
/// The interface for the management of Agents and their ContactData. Offers CRUD
/// operations on Persons and Organisations.
/// </summary>
[ApiController]
[Route("agents")]
public class AgentController(AgentAPI api) : AuthControllerBase
{
    /// <summary>
    /// Returns a list of organisations available for the specified person and the current user.
    /// </summary>
    /// <param name="personUid">URI of the person for whom organisations are fetched.</param>
    /// <returns>Collection of XOrganisation (200) or invalid argument (400)</returns>
    [Authorize]
    [HttpGet("GetAllowedOrganisations")]
    public IEnumerable<XOrganisation> GetAllowedOrganisations([FromQuery] string? personUid)
    {
        Log.Debug("GetAllowedOrganisations called for {Person}", personUid);
        return api.GetAllowedOrganisations(personUid, GetCurrentUserId()!);
    }
    
    /// <summary>
    /// Returns a list of persons that can be associated with a new/given account.
    /// </summary>
    /// <param name="accountId">Optional account identifier - if provided then the person
    /// associated with the account is added to the list</param>
    /// <returns>Collection of XPerson (200) or unauthorized/not found (40X)</returns>
    [Authorize]
    [HttpGet("GetAllowedPersonsForAccount")]
    public IEnumerable<XPerson> GetAllowedPersonsForAccount([FromQuery] string? accountId)
    {
        Log.Debug("GetAllowedPersonsForAccount called for {AccountId}", accountId);
        return api.GetAllowedPersonsForAccount(accountId, GetCurrentUserId()!);
    }
    
    /// <summary>
    /// Get a specific Agent (identified by URI)
    /// </summary>
    /// <param name="identifier">URI identifier of the agent.</param>
    /// <returns>XAgent (200), or not found (404)</returns>
    [Authorize]
    [HttpGet("GetAgent")]
    public XAgent GetAgent([FromQuery] string? identifier)
    {
        Log.Debug("GetAgent called for {Identifier}", identifier);
        return api.GetAgent(identifier, GetCurrentUserId(false));
    }
    
    /// <summary>
    /// Check if a specific Agent id (Uri) is unique
    /// </summary>
    /// <param name="identifier"></param>
    /// <returns>"true" if unique, "false" if not unique</returns>
    [HttpGet("CheckAgentId")]
    public bool CheckAgentId([FromQuery] string identifier)
    {
        Log.Debug("CheckAgentId called for {Identifier}", identifier);
        return api.CheckAgentId(identifier);
    }

    /// <summary>
    /// Insert an Organisation. If the Organisation already exists, it will be updated (all fields will be overwritten). 
    /// </summary>
    /// <param name="xOrganisation">Organisation to be added or updated with optional new contacts to be added.</param>
    /// <returns>URI of the added or updated resource (200) or bad request or unauthorised (40X)</returns>
    [Authorize]
    [HttpPost("UpsertOrganisation")]
    public string UpsertOrganisation([FromBody] XOrganisationWithContacts xOrganisation)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("UpsertOrganisation called for {Organisation} by {Id}",
            xOrganisation.Organisation.Uri, userId);
        return api.UpsertOrganisation(xOrganisation.Organisation, xOrganisation.NewContacts, userId);
    }

    /// <summary>
    /// Insert a Person. If the Person already exists, it will be updated (all fields will be overwritten). 
    /// </summary>
    /// <param name="xPerson">Person to be added or updated with optional new contacts to be added.</param>
    /// <returns>URI of the added or updated resource (200) or bad request or unauthorised (40X)</returns>
    [Authorize]
    [HttpPost("UpsertPerson")]
    public string UpsertPerson([FromBody] XPersonWithContacts xPerson)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("UpsertPerson called for {Person} by {Id}", xPerson.Person.Uri, userId);
        return api.UpsertPerson(xPerson.Person, xPerson.NewContacts, userId);
    }

    /// <summary>
    /// Delete a specific Agent (identified by URI)
    /// </summary>
    /// <param name="identifier">URI identifier of the agent.</param>
    /// <returns>deleted (200), bad request or unauthorised (40X)</returns>
    [Authorize]
    [HttpDelete("DeleteAgent")]
    public void DeleteAgent([FromQuery] string identifier)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("DeleteAgent called for {Identifier} by {Id}", identifier, userId);
        api.DeleteAgent(identifier, userId);
    }

    /// <summary>
    /// Get a list of Agents that have a name similar to the query.
    /// </summary>
    /// <param name="query">Fragment of the agent name (at least 3 characters) or empty</param>
    /// <returns>XAgents (200)</returns>
    [Authorize]
    [HttpGet("GetAgentList")]
    public XAgents GetAgentList([FromQuery] string? query)
    {
        Log.Debug("GetAgentList called with query: {Query}", query);
        return new XAgents() {
            Persons = api.GetPersonList(query, GetCurrentUserId(false)),
            Organisations = api.GetOrganisationList(query, GetCurrentUserId(false))
        };
    }

    /// <summary>
    /// Get a list of Persons that have a name similar to the query.
    /// </summary>
    /// <param name="query">Fragment of the person name (at least 3 characters) or empty</param>
    /// <returns>Collection of XPersons (200)</returns>
    [Authorize]
    [HttpGet("GetPersonList")]
    public IEnumerable<XPerson> GetPersonList([FromQuery] string? query)
    {
        Log.Debug("GetPersonList called with query: {Query}", query);
        return api.GetPersonList(query, GetCurrentUserId(false));
    }

    /// <summary>
    /// Get a list of Organisations that have a name similar to the query.
    /// </summary>
    /// <param name="query">Fragment of the organisation name (at least 3 characters) or empty</param>
    /// <returns>Collection of XOrganisations (200)</returns>
    [Authorize]
    [HttpGet("GetOrganisationList")]
    public IEnumerable<XOrganisation> GetOrganisationList([FromQuery] string? query)
    {
        Log.Debug("GetOrganisationList called with query: {Query}", query);
        return api.GetOrganisationList(query, GetCurrentUserId(false));
    }

    /// <summary>
    /// Get persons that are members of the given organisation.
    /// </summary>
    /// <param name="identifier">URI identifier of the organisation.</param>
    /// <returns>Collection of XPersons (200) or bad request if organisation does not exist (400)</returns>
    [Authorize]
    [HttpGet("GetPersonsInOrganisation")]
    public IEnumerable<XPerson> GetPersonsInOrganisation([FromQuery] string identifier)
    {
        Log.Debug("GetPersonsInOrganisation called for {Identifier}", identifier);
        return api.GetPersonsInOrganisation(identifier, GetCurrentUserId(false));
    }

    /// <summary>
    /// Get persons that are editors of the given resource.
    /// </summary>
    /// <param name="identifier">URI identifier of the resource.</param>
    /// <returns>Collection of XAgents (200) or bad request (400)</returns>
    [Authorize]
    [HttpGet("GetResourceEditors")]
    public XAgents GetResourceEditors([FromQuery] string identifier)
    {
        Log.Debug("GetResourceEditors called for {Identifier}", identifier);
        return api.GetResourceEditors(identifier, GetCurrentUserId(false));
    }

    /// <summary>
    /// Get contacts that are associated with the given resource.
    /// </summary>
    /// <param name="identifier">URI identifier of the resource.</param>
    /// <returns>Collection of XContactData (200) or bad request (400)</returns>
    [Authorize]
    [HttpGet("GetResourceContacts")]
    public IEnumerable<XContactData> GetResourceContacts([FromQuery] string identifier)
    {
        Log.Debug("GetResourceContacts called for {Identifier}", identifier);
        return api.GetResourceContacts(identifier, GetCurrentUserId(false));
    }

    /// <summary>
    /// Get contacts that are associated with the given agent.
    /// </summary>
    /// <param name="identifier">URI identifier of the agent.</param>
    /// <returns>Collection of XContactData (200) or bad request (400)</returns>
    [Authorize]
    [HttpGet("GetAgentContacts")]
    public IEnumerable<XContactData> GetAgentContacts([FromQuery] string identifier)
    {
        Log.Debug("GetAgentContacts called for {Identifier}",  identifier);
        return api.GetAgentContacts(identifier, GetCurrentUserId(false));
    }

    /// <summary>
    /// Get contacts that are associated with the given list of agents.
    /// </summary>
    /// <param name="agentUris">URI identifiers of the agents.</param>
    /// <returns>Collection of XContactData (200) or bad request (400)</returns>
    [Authorize]
    [HttpPost("GetMultipleAgentContacts")]
    public IEnumerable<XContactData> GetMultipleAgentContacts([FromBody] IEnumerable<string> agentUris)
    {
        var uriList = agentUris.ToList();
        Log.Debug("GetMultipleAgentContacts called for {Uris}",  string.Join(", ", uriList));
        return api.GetMultipleAgentContacts(uriList, GetCurrentUserId(false));
    }
    
    /// <summary>
    /// Returns a list of user accounts available to the current user.
    /// </summary>
    /// <returns>Collection of XUserAccount (200)</returns>
    [Authorize]
    [HttpGet("GetUserAccountList")]
    public IEnumerable<XUserAccount> GetUserAccountList(string? query)
    {
        Log.Debug("GetUserAccountList called with query: {Query}", query);
        return api.GetUserAccountList(query, GetCurrentUserId()!);
    }
    
    /// <summary>
    /// Inserts or updates a user account. If the account already exists, all fields will be overwritten.
    /// </summary>
    /// <param name="xUserAccount">User account to be added or updated.</param>
    [Authorize]
    [HttpPost("UpsertUserAccount")]
    public void UpsertUserAccount([FromBody] XUserAccount xUserAccount)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("UpsertUserAccount called for {Account} by {Id}", xUserAccount.UserId, userId);
        api.UpsertUserAccount(xUserAccount, userId);
    }
    
    /// <summary>
    /// Deletes a specific user account identified by the account ID.
    /// </summary>
    /// <param name="accountId">Id of the user account.</param>
    [Authorize]
    [HttpDelete("DeleteUserAccount")]
    public void DeleteUserAccount([FromQuery] string accountId)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("DeleteUserAccount called for {Account} by {id}", accountId, userId);
        api.DeleteUserAccount(accountId, userId);
    }
    
    /// <summary>
    /// Returns details of a specific user account identified by URI.
    /// </summary>
    /// <param name="identifier">URI identifier of the user account.</param>
    /// <returns>User account details (200) or not found/unauthorized (40X)</returns>
    [Authorize]
    [HttpGet("GetUserAccount")]
    public XUserAccount GetUserAccount([FromQuery] string? identifier)
    {
        Log.Debug("GetUserAccount called for {Identifier}", identifier);
        return api.GetUserAccount(identifier, GetCurrentUserId()!);
    }
    
    /// <summary>
    /// Returns a list of available user account identifiers matching the query for the current user.
    /// </summary>
    /// <param name="query">Optional search query to filter account identifiers.</param>
    /// <returns>Collection of account identifiers (200)</returns>
    [Authorize]
    [HttpGet("GetAvailableAccountIds")]
    public IEnumerable<string> GetAvailableAccountIds([FromQuery] string query)
    {
        Log.Debug("GetAvailableAccountIds called with query: {Query}", query);
        return api.GetAvailableAccountIds(query, GetCurrentUserId()!);
    }
}
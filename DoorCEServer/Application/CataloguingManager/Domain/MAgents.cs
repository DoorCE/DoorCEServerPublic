using AutoMapper;
using DoorCEModel.Infrastructure;
using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEServer.Application.CataloguingManager.Common;
using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Application.CataloguingManager.Interfaces;
using DoorCEServer.Application.KeycloakAdminProxy.Interfaces;
using DoorCEServer.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DoorCEServer.Application.CataloguingManager.Domain;

public class MAgents(IMapper mapper, ApplicationDbContext dbContext, MCataloguingManagerCommon common,
    MAgentsCommon agents, IKeycloakAdmin keycloak) : AgentAPI
{
    private static readonly SemaphoreSlim OrgSemaphore = new(1, 1);
    private static readonly SemaphoreSlim PrsSemaphore = new (1, 1);
    private static readonly SemaphoreSlim AccSemaphore = new(1, 1);

    public IEnumerable<XOrganisation> GetAllowedOrganisations(string? personId, string userId)
    {
        UserAccount account = agents.GetUserAccount(userId)!;
        
        // Get all the organisations from the database
        List<Organisation> unfilteredOrganisations = dbContext.Organisations
            .Include(r => r.Members)
            .Include(r => r.MemberRoles)
            .Include(r => r.Contacts)
            .AsSplitQuery().ToList();
        
        List<Organisation> filteredOrganisations = [];
        // Iterate over all organisations and filter those that are allowed for the user
        foreach (var organisation in unfilteredOrganisations) {
            organisation.UserRoles = agents.GetUserRoles(organisation, account)!;
            if (organisation.UserRoles.Contains(ManagerRole.AgentManager))
                filteredOrganisations.Add(organisation);
        }
        
        // If a person ID is provided, get the organisations that this person is a member of
        if (!string.IsNullOrEmpty(personId)) {
            Person? person = dbContext.Persons
                .Include(p => p.Contacts)
                .Include(p => p.MemberRoles)
                .Include(p => p.Organisations)
                .Include(p => p.Account)
                .AsSplitQuery()
                .SingleOrDefault(p => p.Uri == personId);
            if (null == person || !agents.CheckVisibilityAndSetRoles(person, userId))
                throw new ArgumentException($"Person [{personId}] not found or not visible to the user");

            // Add organisations that the person is a member of, but not already in the list
            foreach (Organisation org in person.Organisations) 
                if (filteredOrganisations.All(o => o.Uri != org.Uri)) {
                    org.UserRoles = agents.GetUserRoles(org, account);
                    filteredOrganisations.Add(org);
                }
        }

        return mapper.Map<ICollection<Organisation>, ICollection<XOrganisation>>(filteredOrganisations);
    }

    public IEnumerable<XPerson> GetAllowedPersonsForAccount(string? accountId, string userId)
    {
        UserAccount account = agents.GetUserAccount(userId)!;
        if (!account.Roles.Contains(GlobalRole.AgentAdmin))
            throw new UnauthorizedAccessException("Not allowed to manage accounts");
        
        // Get all the persons from the database
        List<XPerson> filteredPersons = GetPersonList(null, userId)
            .Where(xp => string.IsNullOrEmpty(xp.UserId)).ToList();
        
        // If an account ID is provided, get the person that this account is assigned to
        if (!string.IsNullOrEmpty(accountId)) {
            UserAccount? requestedAccount = dbContext.UserAccounts
                .Include(a => a.Person)
                .AsSplitQuery()
                .SingleOrDefault(a => a.UserId == accountId);
            
            if (null == requestedAccount)
                throw new ArgumentException($"User account [{accountId}] not found");

            if (null != requestedAccount.Person) {
                dbContext.Entry(requestedAccount.Person).Collection(p => p.Contacts).Load();
                dbContext.Entry(requestedAccount.Person).Collection(p => p.MemberRoles).Load();
                dbContext.Entry(requestedAccount.Person).Collection(p => p.Organisations).Load();
                requestedAccount.Person.UserRoles = agents.GetUserRoles(requestedAccount.Person, account);
                if (filteredPersons.All(p => p.Uri != requestedAccount.Person.Uri))
                    // If the person is not already in the list, add it
                    filteredPersons.Add(mapper.Map<XPerson>(requestedAccount.Person));
            }
        }

        return filteredPersons;
    }

    public XAgent GetAgent(string? identifier, string? userId)
    {
        if (null == identifier) {
            // If the identifier is not provided, return the current user's person
            var account = agents.GetUserAccount(userId);
            if (null == account?.Person)
                throw new NotFoundOrVisibleException(
                    $"Current user does not have a person associated with it");
            dbContext.Entry(account.Person).Collection(p => p.Contacts).Load();
            dbContext.Entry(account.Person).Collection(p => p.MemberRoles).Load();
            dbContext.Entry(account.Person).Collection(p => p.Organisations).Load();
            account.Person.UserRoles = agents.GetUserRoles(account.Person, account);
            
            return mapper.Map<XPerson>(account.Person);
        }

        Agent? agent = dbContext.Agents
            .Include(a => a.Contacts)
            .Include(a => ((Person)a).MemberRoles)
            .Include(a => ((Person)a).Organisations)
            .Include(a => ((Person)a).Account)
            .Include(a => ((Organisation)a).MemberRoles)
            .Include(a => ((Organisation)a).Members)
            .AsSplitQuery()
            .SingleOrDefault(a => a.Uri == identifier);
        
        // ===> Authorisation
        if (null == agent || !agents.CheckVisibilityAndSetRoles(agent, userId))
            throw new NotFoundOrVisibleException($"Agent [{identifier}] not found or not visible to the user");
        
        XAgent xAgent = agent is Person ? mapper.Map<XPerson>(agent) : mapper.Map<XOrganisation>(agent);
        return xAgent;
    }

    public bool CheckAgentId(string identifier)
    {
        return !dbContext.Agents.Any(a => a.Uri == identifier);
    }

    public string UpsertOrganisation(XOrganisation xOrganisation, IEnumerable<XContactData> newContacts, string userId)
    {
        // Set a semaphore to prevent from multiple threads to generate the same URI
        OrgSemaphore.Wait();
            
        // Generate unique identifier if needed
        bool generatedUri = common.CheckAndGenerateUri(xOrganisation);

        // Map the DTO to the domain model
        Organisation requestedVersion = mapper.Map<Organisation>(xOrganisation);

        using var transaction = dbContext.Database.BeginTransaction();
        try {
            UserAccount account = agents.GetUserAccount(userId)!;

            Organisation? existingVersion = null;
            // Get the old version if it exists in the database
            if (!generatedUri) existingVersion = dbContext.Organisations
                .Include(o => o.Members)
                .Include(o => o.MemberRoles)
                .AsSplitQuery()
                .SingleOrDefault(o => o.Uri == requestedVersion.Uri);
            bool isUpdate = null != existingVersion;
            
            // ===> Authorisation
            agents.CheckAuthorisation(requestedVersion, existingVersion, account);

            // ===> Setup organisation metadata
            if (requestedVersion.HasMetadataRole()) {
                // Update contact data for this organisation
                HandleAgentContacts(existingVersion ?? requestedVersion, isUpdate,
                    xOrganisation.ContactsUris, newContacts);

                if (!isUpdate)
                    // Old version does not exist? - Add the new organisation
                    dbContext.Organisations.Add(requestedVersion);
                else {
                    // Otherwise - Update the old organisation with new data
                    requestedVersion.Id = existingVersion!.Id; // make sure to keep the same ID
                    dbContext.Entry(existingVersion).CurrentValues.SetValues(requestedVersion);
                    dbContext.Entry(existingVersion).State = EntityState.Modified;
                }
            }

            // ===> Setup organisation ownership roles
            if (requestedVersion.HasAgentRole()) {
                // Handle member persons and their roles
                List<Membership> memberRoles = [];
                if (0 != xOrganisation.MembersUris.Count) {
                    ICollection<Person> members = dbContext.Persons
                        .Where(o => xOrganisation.MembersUris.Contains(o.Uri)).ToList();
                    // TODO - optimise with "agents.SetUserRoles(members, account);"
                    foreach (Person member in members) {
                        member.UserRoles = agents.GetUserRoles(member, account);
                        if (!member.HasAgentRole()) throw new UnauthorizedAccessException(
                            $"User does not have the right roles to manage this person [{member.Uri}]");
                        memberRoles.Add(new Membership() {
                            Organisation = existingVersion ?? requestedVersion,
                            Person = member,
                            Roles = xOrganisation.MembersRoles.ContainsKey(member.Uri)
                                ? xOrganisation.MembersRoles[member.Uri] : new List<ManagerRole>()
                        });
                    }
                    
                    if (memberRoles.Count != xOrganisation.MembersUris.Count)
                        throw new ArgumentException("Some member of the organisation not found");
                }

                // Set them depending on whether it is an insert or an update
                (existingVersion ?? requestedVersion).MemberRoles = memberRoles;
                dbContext.Entry(existingVersion ?? requestedVersion).Collection(o => o.Members).Load();
            } else if (!requestedVersion.HasMetadataRole())
                throw new ArgumentException("No change to the organisation was requested");
            
            // Validation
            if (!(existingVersion ?? requestedVersion).Validate())
                throw new ArgumentException("Organisation data did not pass validation");
            
            dbContext.SaveChanges();
            transaction.Commit();
            OrgSemaphore.Release();
            
            return existingVersion?.Uri ?? requestedVersion.Uri;
        } catch (Exception) { 
            dbContext.ChangeTracker.Clear(); transaction.Rollback(); OrgSemaphore.Release(); throw;
        }
    }

    public string UpsertPerson(XPerson xPerson, IEnumerable<XContactData> newContacts, string userId)
    {
        // Set a semaphore to prevent from multiple threads to generate the same URI
        PrsSemaphore.Wait();
            
        // Generate unique identifier if needed
        bool generatedUri = common.CheckAndGenerateUri(xPerson);
        
        // Map the DTO to the domain model
        Person requestedVersion = mapper.Map<Person>(xPerson);
        
        using var transaction = dbContext.Database.BeginTransaction();
        try
        {
            UserAccount account = agents.GetUserAccount(userId)!;

            Person? existingVersion = null;
            // Get the old version if it exists in the database
            if (!generatedUri) existingVersion = dbContext.Persons
                .Include(p => p.Organisations)
                .Include(p => p.MemberRoles)
                .Include(p => p.Account)
                .AsSplitQuery()
                .SingleOrDefault(p => p.Uri == requestedVersion.Uri);
            bool isUpdate = null != existingVersion;
            
            // ===> Authorisation
            agents.CheckAuthorisation(requestedVersion, existingVersion, account);

            // ===> Setup agent metadata and ownership roles
            if (requestedVersion.HasAgentRole()) {
                // Handle user accounts - create new account and set userId
                if (!string.IsNullOrEmpty(xPerson.UserId) && (!isUpdate || null == existingVersion!.Account || 
                        existingVersion.Account.UserId != xPerson.UserId)) {
                    bool createKeycloakUser = !string.IsNullOrEmpty(xPerson.UserPassword) ||
                                              !string.IsNullOrEmpty(xPerson.UserEmail);
                    // Tried to change user account without admin roles?
                    if (isUpdate && null != existingVersion!.Account && !account.Roles.Contains(GlobalRole.AgentAdmin))
                        throw new UnauthorizedAccessException("Cannot change user ID of the person without admin roles");
                    // Make sure that the user ID is unique and exists in Keycloak
                    if (dbContext.UserAccounts.Any(a => a.UserId == xPerson.UserId))
                        throw new ArgumentException($"For this user ID [{xPerson.UserId}] an account already exists");
                    bool userExistsInKeycloak = keycloak.CheckUserExists(xPerson.UserId);
                    if (!createKeycloakUser && !userExistsInKeycloak)
                        throw new ArgumentException($"User ID [{xPerson.UserId}] does not exist in Keycloak");
                    if (createKeycloakUser) {
                        if (userExistsInKeycloak)
                            throw new ArgumentException($"User ID [{xPerson.UserId}] already exists in Keycloak");
                        keycloak.CreateUser(xPerson);
                    }
                    // Create a new account and link (new or replace) it to the person
                    (existingVersion ?? requestedVersion).Account = new UserAccount(){ UserId = xPerson.UserId };
                } else if (isUpdate && string.IsNullOrEmpty(xPerson.UserId) && null != existingVersion!.Account) {
                    // Remove the user ID from the person
                    if (!account.Roles.Contains(GlobalRole.AgentAdmin))
                        throw new UnauthorizedAccessException("Cannot remove user ID from the person without admin roles");
                    existingVersion.Account = null;
                }
                
                // Get the roles for parent organisations
                ICollection<Membership> memberRoles = HandleParentAndContacts(xPerson, account,
                    existingVersion ?? requestedVersion, isUpdate, newContacts);

                if (!isUpdate)
                    // Old version does not exist? - Add the new person
                    dbContext.Persons.Add(requestedVersion);
                else {
                    // Otherwise - Update the old person with new data
                    requestedVersion.Id = existingVersion!.Id; // make sure to keep the same ID
                    dbContext.Entry(existingVersion).CurrentValues.SetValues(requestedVersion);
                    dbContext.Entry(existingVersion).State = EntityState.Modified;
                }
                    
                (existingVersion ?? requestedVersion).MemberRoles = memberRoles;
                dbContext.Entry(existingVersion ?? requestedVersion).Collection(o => o.Organisations).Load();
                
                // Validation
                if (!(existingVersion ?? requestedVersion).Validate())
                    throw new ArgumentException("Person data did not pass validation");
                
                dbContext.SaveChanges();
                transaction.Commit();
                PrsSemaphore.Release();
            } else
                throw new ArgumentException("No change to the person was requested");
            
            return existingVersion?.Uri ?? requestedVersion.Uri;
        } catch (Exception) {
            dbContext.ChangeTracker.Clear(); transaction.Rollback(); PrsSemaphore.Release(); throw;
        }
    }

    public void DeleteAgent(string identifier, string userId)
    {
        using IDbContextTransaction transaction = dbContext.Database.BeginTransaction();
        try {
            // To delete, make sure that all the related objects are loaded (included into the context)
            Agent? agent = dbContext.Agents
                .Include(a => a.Contacts).ThenInclude(c => c.Resources)
                .ThenInclude(r => r.Contacts)
                .Include(a => a.ResourceLink)
                .ThenInclude(r => r.Editors)
                .Include(a => a.EditorRoles)
                .Include(a => ((Person)a).MemberRoles)
                .Include(a => ((Person)a).Organisations)
                .Include(a => ((Person)a).Account)
                .Include(a => ((Person)a).ManagedResources)
                .Include(a => ((Organisation)a).MemberRoles)
                .Include(a => ((Organisation)a).Members)
                .Include(a => ((Organisation)a).ManagedResources)
                .AsSplitQuery()
                .SingleOrDefault(c => c.Uri == identifier);
            
            // ===> Authorisation
            if (null == agent || !agents.CheckVisibilityAndSetRoles(agent, userId))
                throw new ArgumentException($"Agent [{identifier}] not found or not visible to the user");
            if (!agent.HasAgentRole())
                throw new UnauthorizedAccessException($"User does not have the right to delete this agent");
            
            // Cannot delete if its contact data is used by some resources
            foreach (ContactData contactData in agent.Contacts)
                if (0 != contactData.Resources.Count)
                    throw new InvalidOperationException(
                        "Cannot delete agent because it has contact data used by some resources");

            // Cannot delete if it manages some resources or its contacts are used (solely) by some resources
            if (0 != agent.ManagedResources.Count)
                throw new InvalidOperationException("Cannot delete agent because it manages some resources");
            if (agent.ResourceLink.Any(r => 1 == r.Editors.Count))
                throw new InvalidOperationException(
                    "Cannot delete agent because it is the only editor of some resources");
            if (agent.Contacts.Any(c => c.Resources.Any(r => r.Contacts.Count == 1)))
                throw new InvalidOperationException(
                    "Cannot delete agent because it has contact data that is the only contact for some resources");
            
            dbContext.Entry(agent).State = EntityState.Deleted;
            foreach (ContactData contactData in agent.Contacts)
                dbContext.Entry(contactData).State = EntityState.Deleted;
            dbContext.SaveChanges();
            transaction.Commit();
        } catch (Exception) { dbContext.ChangeTracker.Clear(); transaction.Rollback(); throw; }
    }
    
    public IEnumerable<XPerson> GetPersonList(string? query, string? userId)
    {
        query = query?.ToLower();
        
        ICollection<Person> persons = dbContext.Persons
            .Include(p => p.Organisations)
            .Include(p => p.MemberRoles)
            .Include(p => p.Account)
            .Include(p => p.Contacts)
            .AsSplitQuery()
            .Where(p => string.IsNullOrEmpty(query) || query.Length < 3 
                                                    || p.GivenNames.Any(n => n.ToLower().Contains(query)) ||
                                                    p.FamilyName.ToLower().Contains(query)).ToList();

        // Authorisation
        persons = persons.Where(p => agents.CheckVisibilityAndSetRoles(p, userId)).ToList();

        return mapper.Map<ICollection<Person>, ICollection<XPerson>>(persons);
    }
    
    public IEnumerable<XOrganisation> GetOrganisationList(string? query, string? userId)
    {
        query = query?.ToLower();
        
        ICollection<Organisation> organisations = dbContext.Organisations
            .Include(a => a.Members)
            .Include(a => a.MemberRoles)
            .Include(a => a.Contacts)
            .AsSplitQuery()
            .Where(p=> string.IsNullOrEmpty(query) || query.Length < 3 
                                                   || p.Name.ToLower().Contains(query)).ToList();
        
        // Authorisation
        organisations = organisations.Where(o => agents.CheckVisibilityAndSetRoles(o, userId)).ToList();

        return mapper.Map<ICollection<Organisation>, ICollection<XOrganisation>>(organisations);
    }

    public IEnumerable<XPerson> GetPersonsInOrganisation(string identifier, string? userId)
    {
        Organisation? org = dbContext.Organisations.SingleOrDefault(o => o.Uri == identifier);
        
        // ===> Authorisation
        if (null == org || !agents.CheckVisibilityAndSetRoles(org, userId))
            throw new NotFoundOrVisibleException($"Organisation [{identifier}] not found or not visible to the user");
        
        ICollection<Person> persons = dbContext.Persons
            .Include(p => p.Organisations)
            .Include(p => p.MemberRoles)
            .Include(p => p.Account)
            .Include(p => p.Contacts)
            .AsSplitQuery()
            .Where(p => p.Organisations.Any(o => o.Uri == identifier)).ToList();
        
        // ==> Authorisation
        agents.SetUserRoles(persons, userId);

        ICollection<XPerson> xPersons = mapper.Map<ICollection<Person>, ICollection<XPerson>>(persons);

        return xPersons;
    }

    public XAgents GetResourceEditors(string identifier, string? userId)
    {
        OwnableResource? resource = dbContext.OwnableResources
            .Include(o => o.EditorshipsLink)
            .SingleOrDefault(o => o.Uri == identifier);
        
        // ===> Authorisation
        if (null == resource || !agents.CheckVisibilityAndSetRoles(resource, userId)) // Authorisation
            throw new NotFoundOrVisibleException($"Resource [{identifier}] not found or not visible to the user");
        
        ICollection<Person> persons = dbContext.Persons
            .Include(p => p.Organisations)
            .Include(p => p.MemberRoles)
            .Include(p => p.Account)
            .Include(p => p.Contacts)
            .Include(p => p.ResourceLink)
            .Include(p => p.EditorRoles)
            .AsSplitQuery()
            .Where(p => p.ResourceLink.Any(r => r.Id == resource.EditorshipsLink.Id)).ToList();
        
        // ===> Authorisation
        agents.SetUserRoles(persons, userId);
        
        ICollection<Organisation> organisations = dbContext.Organisations
            .Include(p => p.Contacts)
            .Include(p => p.ResourceLink)
            .Include(p => p.EditorRoles)
            .Include(p => p.Members)
            .Include(p => p.MemberRoles)
            .AsSplitQuery()
            .Where(p => p.ResourceLink.Any(r => r.Id == resource.EditorshipsLink.Id)).ToList();
        
        // ===> Authorisation
        agents.SetUserRoles(organisations, userId);

        XAgents xAgents = new XAgents {
            Persons = mapper.Map<ICollection<Person>, ICollection<XPerson>>(persons),
            Organisations = mapper.Map<ICollection<Organisation>, ICollection<XOrganisation>>(organisations)
        };

        return xAgents;
    }

    public IEnumerable<XContactData> GetResourceContacts(string identifier, string? userId)
    {
        OwnableResource? resource = dbContext.OwnableResources.SingleOrDefault(o => o.Uri == identifier);
        
        // ===> Authorisation
        if (null == resource || !agents.CheckVisibilityAndSetRoles(resource, userId)) // Authorisation
            throw new NotFoundOrVisibleException($"Resource [{identifier}] not found or not visible to the user");
        
        ICollection<ContactData> contacts = dbContext.ContactDatas
            .Include(c => c.Resources)
            .Include(c => c.Agent)
            .AsSplitQuery()
            .Where(c => c.Resources.Any(o => o.Uri == identifier)).ToList();

        ICollection<XContactData> xContacts =
            mapper.Map<ICollection<ContactData>, ICollection<XContactData>>(contacts);

        return xContacts;
    }

    public IEnumerable<XContactData> GetAgentContacts(string identifier, string? userId)
    {
        Agent? agent = dbContext.Agents.SingleOrDefault(o => o.Uri == identifier);
        
        // ===> Authorisation
        if (null == agent || !agents.CheckVisibilityAndSetRoles(agent, userId)) // Authorisation
            throw new NotFoundOrVisibleException($"Agent [{identifier}] not found or not visible to the user");
        
        ICollection<ContactData> contacts = dbContext.ContactDatas
            .Include(c => c.Resources)
            .Include(c => c.Agent)
            .AsSplitQuery()
            .Where(c => c.Agent != null && c.Agent.Uri == identifier).ToList();

        ICollection<XContactData> xContacts =
            mapper.Map<ICollection<ContactData>, ICollection<XContactData>>(contacts);

        return xContacts;
    }

    public IEnumerable<XContactData> GetMultipleAgentContacts(IEnumerable<string> agentUris, string? userId)
    {
        if (null == agentUris || 0 == agentUris.Count())
            throw new ArgumentException("No agents provided");
        
        List<Agent> agentList = dbContext.Agents
            .Include(a => a.Contacts)
            .AsSplitQuery()
            .Where(c => agentUris.Contains(c.Uri)).ToList();
        
        // ===> Authorisation
        if (agentUris.ToList().Count != agentList.Count 
            || !agentList.All(a => agents.CheckVisibilityAndSetRoles(a, userId)))
            throw new NotFoundOrVisibleException("Some agents not found or not visible to the user");
        
        return mapper.Map<ICollection<ContactData>, ICollection<XContactData>>(
                agentList.SelectMany(a => a.Contacts).ToList());
    }
    
    // ===== User accounts management ===================================================================

    public IEnumerable<XUserAccount> GetUserAccountList(string? query, string userId)
    {
        // ===> Authorisation
        UserAccount userAccount = agents.GetUserAccount(userId)!;
        if (!userAccount.Roles.Contains(GlobalRole.AgentAdmin))
            throw new UnauthorizedAccessException("Not allowed to manage user accounts");
        
        var accounts = dbContext.UserAccounts
            .Include(a => a.Person)
            .Where(a => string.IsNullOrEmpty(query) || a.UserId.Contains(query))
            .ToList();
        
        return mapper.Map<ICollection<UserAccount>, ICollection<XUserAccount>>(accounts);
    }

    public void UpsertUserAccount(XUserAccount xUserAccount, string userId)
    {
        // ===> Authorisation
        UserAccount userAccount = agents.GetUserAccount(userId)!;
        if (!userAccount.Roles.Contains(GlobalRole.AgentAdmin))
            throw new UnauthorizedAccessException("Not allowed to manage user accounts");
        
        // Set a semaphore to prevent from multiple threads to create accounts with the same ID
        AccSemaphore.Wait();
        
        // Map the DTO to the domain model
        if (xUserAccount.Roles.Contains(GlobalRole.SchemaCreator))
            xUserAccount.Roles.Remove(GlobalRole.SchemaCreator); // Do not store it in the database
        UserAccount requestedVersion = mapper.Map<UserAccount>(xUserAccount);
        
        using var transaction = dbContext.Database.BeginTransaction();
        try
        {
            // Get the existing account (if any)
            UserAccount? existingVersion = dbContext.UserAccounts
                .Include(a => a.Person)
                .SingleOrDefault(a => a.UserId == xUserAccount.UserId);
            
            bool isUpdate = null != existingVersion; // Is this an update or a new account?

            // Make sure that the user ID exists in Keycloak (for new accounts)
            if (!isUpdate && !keycloak.CheckUserExists(requestedVersion.UserId))
                    throw new ArgumentException($"User ID [{requestedVersion.UserId}] does not exist in Keycloak");

            // If the person URI is provided, retrieve it from the database
            Person? designatedPerson = dbContext.Persons
                .Include(p => p.Account)
                .SingleOrDefault(p => p.Uri == xUserAccount.PersonUri);
            
            if (designatedPerson is { Account: not null } // the designated person already has an account
                // and this is not the same person as the one requested
                && (!isUpdate || designatedPerson.Uri != existingVersion!.Person?.Uri))
                throw new ArgumentException(
                    $"Person [{designatedPerson.Uri}] already has an account associated with it");
                
            if (!isUpdate) { // If this is a new account, add the requested version to the database
                requestedVersion.Person = designatedPerson;
                dbContext.UserAccounts.Add(requestedVersion);
            } else { // Otherwise, update the existing account
                requestedVersion.Id = existingVersion!.Id; // make sure to preserve the unique ID
                dbContext.Entry(existingVersion).CurrentValues.SetValues(requestedVersion);
                // Is the designated person different from the existing one?
                if (null != designatedPerson && designatedPerson.Uri != existingVersion.Person?.Uri)
                    // Then update the person reference
                    existingVersion.Person = designatedPerson;
                else if (null == xUserAccount.PersonUri) // If the person is not set, remove the reference
                    existingVersion.Person = null;
                // Set the state to modified so that it is saved
                dbContext.Entry(existingVersion).State = EntityState.Modified;
            }

            dbContext.SaveChanges();
            transaction.Commit();
            AccSemaphore.Release();
        } catch (Exception) {
            dbContext.ChangeTracker.Clear(); transaction.Rollback(); AccSemaphore.Release(); throw;
        }
    }

    public void DeleteUserAccount(string accountId, string userId)
    {
        // ===> Authorisation
        UserAccount userAccount = agents.GetUserAccount(userId)!;
        if (!userAccount.Roles.Contains(GlobalRole.AgentAdmin))
            throw new UnauthorizedAccessException("Not allowed to manage user accounts");
        using IDbContextTransaction transaction = dbContext.Database.BeginTransaction();
        try {
            var account = dbContext.UserAccounts
                .Include(a => a.Person).SingleOrDefault(a => a.UserId == accountId);
            if (account == null)
                throw new NotFoundOrVisibleException($"User account [{accountId}] does not exist");

            dbContext.Entry(account).State = EntityState.Deleted;
            dbContext.SaveChanges();
            transaction.Commit();
        } catch (Exception) {
            dbContext.ChangeTracker.Clear(); transaction.Rollback(); throw;
        }
    }

    public XUserAccount GetUserAccount(string? accountId, string userId)
    {
        // ===> Authorisation
        UserAccount userAccount = agents.GetUserAccount(userId)!;
        if (null != accountId && !userAccount.Roles.Contains(GlobalRole.AgentAdmin))
            throw new UnauthorizedAccessException("Not allowed to manage user accounts");
        
        // If the account ID is not provided, return the current user's account
        var account = null != accountId ?  
            dbContext.UserAccounts.Include(a => a.Person)
                .SingleOrDefault(a => a.UserId == accountId) 
            : userAccount;
        if (account == null)
            throw new NotFoundOrVisibleException($"User account [{accountId}] does not exist");

        XUserAccount xAccount = mapper.Map<XUserAccount>(account);
        if (agents.CheckSchemaAuthorisation(account))
            xAccount.Roles.Add(GlobalRole.SchemaCreator);
        return xAccount;
    }

    public IEnumerable<string> GetAvailableAccountIds(string query, string userId)
    {
        if (query.Length < 3)
            throw new ArgumentException("Query has to be at least 3 characters long");

        // ===> Authorisation
        UserAccount userAccount = agents.GetUserAccount(userId)!;
        agents.CheckAccountAvailabilityAuthorisation(userAccount);
        
        IEnumerable<string> userIds = keycloak.GetUserIds(query);
        
        return userIds.Where(id => dbContext.UserAccounts.All(a => a.UserId != id));
    }

    // ****** Private methods ******************************************
    
    private ICollection<Membership> HandleParentAndContacts(XPerson xPerson, UserAccount account,
        Person person, bool isUpdate, IEnumerable<XContactData> newContacts)
    {
        List<Membership> rolesInOrganisations = [];
        if (0 == xPerson.OrganisationsUris.Count) {
            if (!account.Roles.Contains(GlobalRole.AgentAdmin))
                throw new UnauthorizedAccessException("Persons without organisations can be created by admin roles only");
        } else {
            ICollection<Organisation> parents = dbContext.Organisations
                .Where(o => xPerson.OrganisationsUris.Contains(o.Uri)).ToList();
            // TODO - optimise with "agents.SetUserRoles(members, account);"
            foreach (Organisation parent in parents) {
                dbContext.Entry(parent).Collection(o => o.Members).Load();
                dbContext.Entry(parent).Collection(o => o.MemberRoles).Load();
                parent.UserRoles = agents.GetUserRoles(parent, account);
                var roles = xPerson.RolesInOrganisations.ContainsKey(parent.Uri)
                    ? xPerson.RolesInOrganisations[parent.Uri]
                    : new List<ManagerRole>();
                if (!parent.HasAgentRole()) {
                    Membership? existingMembership = person.MemberRoles
                        .SingleOrDefault(m => m.Organisation.Uri == parent.Uri);
                    
                    if (null == existingMembership || existingMembership.Roles.Count != roles.Count
                        || !existingMembership.Roles.All(r => roles.Contains(r)))
                        throw new UnauthorizedAccessException(
                            $"User does not have the right to add persons to this organisation [{parent.Uri}]");
                }

                rolesInOrganisations.Add(new Membership() {
                    Organisation = parent,
                    Person = person,
                    Roles = roles
                });
            }
                    
            if (rolesInOrganisations.Count != xPerson.OrganisationsUris.Count)
                throw new ArgumentException("Some parent organisation not found");
            if (isUpdate)
                foreach (Membership removedMembership in person.MemberRoles
                             .Where(m => !xPerson.OrganisationsUris.Contains(m.Organisation.Uri)).ToList()) {
                    dbContext.Entry(removedMembership).Reference(m => m.Organisation).Load();
                    removedMembership.Organisation.UserRoles =
                        agents.GetUserRoles(removedMembership.Organisation, account);
                    if (!removedMembership.Organisation.HasAgentRole())
                        throw new UnauthorizedAccessException(
                            $"User does not have the right to remove persons from this organisation [{removedMembership.Organisation.Uri}]");
                }
        }
                
        // Update contact data for this person
        HandleAgentContacts(person, isUpdate, xPerson.ContactsUris, newContacts);

        return rolesInOrganisations;
    }

    private void HandleAgentContacts(Agent agent, bool isOld,
        ICollection<string> contactIds, IEnumerable<XContactData> newContacts, bool movingAllowed = false)
    {
        // Remove from the database contact data to be removed from the dataset that have no agents attached 
        if (isOld) {
            dbContext.Entry(agent).Collection(a => a.Contacts).Load();
            foreach (ContactData contactData in agent.Contacts)
                if (!contactIds.Contains(contactData.Uri)) {
                    dbContext.Entry(contactData).Collection(c => c.Resources).Load();
                    if (0 == contactData.Resources.Count)
                        dbContext.Entry(contactData).State = EntityState.Deleted;
                    else throw new InvalidOperationException(
                        $"Cannot remove contact data [{contactData.Uri}] because it is used by some resources");
                }
        }
        
        // Create a list that contains all new contacts that are prepared to be added to the database
        List<ContactData> updatedContacts = newContacts.Select(
            c => common.UpsertContactData(c, true, agent)).ToList();

        if (movingAllowed) {
            // Create a list that contains also contacts to be moved from other contacts
            updatedContacts.AddRange(dbContext.ContactDatas.Where(c => contactIds.Contains(c.Uri)).ToList());
            // Replace the previous list (one without the contacts to be moved)
            agent.Contacts = updatedContacts;
        }
    }
}
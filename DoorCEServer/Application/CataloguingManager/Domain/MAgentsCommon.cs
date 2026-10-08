using System.Collections.ObjectModel;
using DoorCEModel.Infrastructure;
using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEModel.Infrastructure.DataModel.Applications;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEModel.Infrastructure.DataModel.Datasets;
using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Application.DataTemplateManager.Dtos;
using Microsoft.EntityFrameworkCore;

namespace DoorCEServer.Application.CataloguingManager.Domain;

public class MAgentsCommon(ApplicationDbContext context)
{
    public Agent GetAgent(string agentId)
    {
        var agent = context.Agents.FirstOrDefault(c => c.Uri == agentId);
        if (null == agent) throw new ArgumentException($"Agent {agentId} not found");
        return agent;
    }

    // ==== "CheckAuthorisation" methods =======================================================================
    // These methods assure that the user has sufficient roles to perform the requested operation.
    // If the roles are not OK, the methods throw an exception, otherwise - they just allow to pass through.
    // The requested roles are not changed even if the user has more roles than requested.
    // =========================================================================================================
    
    public void CheckAuthorisation(ManageableResource requestedVersion, ManageableResource? existingVersion,
        UserAccount account)
    {
        if (null == existingVersion) {
            // To insert just check that the requested roles are sufficient; parent roles are checked later
            ICollection<EditorRole> requiredRoles = [EditorRole.MetadataEditor, EditorRole.OwnershipEditor];
            if (null == requestedVersion.UserRoles || 
                !requiredRoles.All(r => requestedVersion.UserRoles.Contains(r)))
                throw new UnauthorizedAccessException("Not enough roles to create a new resource");
        } else {
            ICollection<EditorRole> currentEditorRoles = GetUserRoles(existingVersion, account);
            if (null == requestedVersion.UserRoles)
                throw new ArgumentException("No roles are specified to update the resource");
            if (!requestedVersion.UserRoles.All(r => currentEditorRoles.Contains(r)))
                throw new UnauthorizedAccessException("Not enough roles to update a new resource");
        }
    }
    
    public void CheckAuthorisation(XDistribution xDistribution, Distribution? existingVersion,
        UserAccount account)
    {
        if (null == existingVersion) {
            // To insert just check that the requested roles are sufficient; parent roles are checked later
            if (!xDistribution.IsUserEditable)
                throw new ArgumentException("Cannot create a new distribution - editability flag not set");
        } else {
            if (xDistribution.IsUserEditable &&
                !GetUserRoles(existingVersion.Dataset, account).Contains(EditorRole.DistributionEditor))
                throw new UnauthorizedAccessException("User does not have permission to edit this distribution");
        }
    }
    
    public void CheckAuthorisation(XDataSchema xDataSchema, DataSchema? existingVersion,
        UserAccount account)
    {
        if (null == existingVersion) {
            // To insert just check that the requested roles are sufficient; parent roles are checked later
            if (!xDataSchema.IsUserEditable)
                throw new ArgumentException("Cannot create a new data schema - editability flag not set");
        } else {
            if (xDataSchema.IsUserEditable &&
                !GetUserRoles(existingVersion.InSeries, account).Contains(EditorRole.MetadataEditor))
                throw new UnauthorizedAccessException("User does not have permission to edit this data schema");
        }
    }
    
    public void CheckAuthorisation(Agent requestedVersion, Agent? existingVersion, UserAccount account)
    {
        if (null == existingVersion) {
            // To insert check that the requested roles are sufficient; parent roles are checked later
            if (null == requestedVersion.UserRoles ||
                !requestedVersion.UserRoles.Contains(ManagerRole.AgentManager))
                throw new ArgumentException("Not enough roles to create a new agent");
            if (requestedVersion is Organisation && !account.Roles.Contains(GlobalRole.AgentAdmin))
                throw new UnauthorizedAccessException("Cannot create a new organisation without the admin role");
        } else {
            ICollection<ManagerRole>? currentEditorRoles = GetUserRoles(existingVersion, account)!;
            if (null == requestedVersion.UserRoles)
                throw new ArgumentException("No roles are specified to update the organisation");
            if (!requestedVersion.UserRoles.All(r => currentEditorRoles.Contains(r)))
                throw new UnauthorizedAccessException("Not enough roles to update the organisation");
        }
    }

    public void CheckStorageAuthorisation(UserAccount account, bool isIcon = false)
    {
        if (account.Roles.Contains(GlobalRole.DataAdmin)) return;
        context.Entry(account).Reference(a => a.Person).Load();
        if (null != account.Person) {
            context.Entry(account.Person).Collection(p => p.EditorRoles).Load();
            if (account.Person.EditorRoles.Any(mr => mr.Roles.Contains(
                    isIcon ? EditorRole.MetadataEditor : EditorRole.DistributionEditor)))
                return; // the person has a role that allows to manage distributions
            context.Entry(account.Person).Collection(p => p.MemberRoles).Load();
            foreach (Membership m in account.Person.MemberRoles
                         .Where(mr => mr.Roles.Contains(
                             isIcon ? ManagerRole.MetadataManager : ManagerRole.DistributionManager))) {
                // the person has a role in an organisation that allows to manage distributions
                context.Entry(m).Reference(mr => mr.Organisation).Load();
                context.Entry(m.Organisation).Collection(o => o.EditorRoles).Load();
                if (m.Organisation.EditorRoles.Any(er => er.Roles.Contains(
                        isIcon ? EditorRole.MetadataEditor : EditorRole.DistributionEditor)))
                    return; // the organisation has a role that allows to manage at least one distribution
            }
        }
        throw new UnauthorizedAccessException
            ($"User does not have permission to manage {(isIcon ? "icons" : "files")}.");
    }
    
    public void CheckAccountAvailabilityAuthorisation(UserAccount account)
    {
        if (account.Roles.Contains(GlobalRole.AgentAdmin)) return;
        if (null != account.Person) {
            context.Entry(account.Person).Collection(p => p.MemberRoles).Load();
            if (account.Person.MemberRoles.Any(m => m.Roles.Contains(ManagerRole.AgentManager))) return;
        }
        throw new UnauthorizedAccessException("Not allowed to check account availability");
    }
    
    // ==== End of "CheckAuthorisation" methods ================================================================

    public UserAccount? GetUserAccount(string? userId)
    {
        if (string.IsNullOrEmpty(userId)) return null;
        var account = context.UserAccounts
            .Include(userAccount => userAccount.Person)
            .AsSplitQuery()
            .FirstOrDefault(ua => ua.UserId == userId);
        if (null == account) throw new UnauthorizedAccessException($"User account with userId '{userId}' not found.");
        return account;
    }
    
    public bool IsGloballyVisible(ManageableResource resource)
    {
        // Catalogues are always visible
        if (resource is Catalogue or SchemaSeries) return true;
        // Application templates and applications are always NOT globally visible
        if (resource is AppTemplate or AcquisitionApp) return false;
        List<AccessRightsType> accesses = [AccessRightsType.Public, AccessRightsType.Provisional];
        List<DatasetStatus> statuses = [DatasetStatus.Active, DatasetStatus.Draft];
        // Chek is the resource is visible for everyone
        // (it is public or provisional and additionally has to be active or draft for datasets)
        if (accesses.Contains(((CataloguedResource)resource).AccessRights) 
            && (resource is not Dataset dataset || statuses.Contains(dataset.Status))) return true;
        return false;
    }

    public ICollection<EditorRole> GetAgentRolesForResource(ManageableResource resource, Agent agent)
    {
        ICollection<EditorRole> roles = new Collection<EditorRole>();
        // Check if the agent has editor roles directly
        context.Entry(resource).Reference(r => r.EditorshipsLink).Load();
        context.Entry(resource.EditorshipsLink).Collection(r => r.EditorRoles).Load();
        foreach (var editor in resource.EditorRoles) {
            context.Entry(editor).Reference(e => e.Agent).Load();
            if (editor.Agent!.Uri == agent.Uri) {
                roles = editor.Roles;
                break;
            }
        }
        
        // Check if the agent (being a person) has manager roles in some organisations
        // and set appropriate editor roles for the resource
        if (agent is Person per) {
            context.Entry(per).Collection(p => p.MemberRoles).Load();
            // Iterate through memberships in organisations
            foreach (var mr in per.MemberRoles) {
                if (0 == mr.Roles.Count) continue; // no manager role in this organisation
                context.Entry(mr).Reference(m => m.Organisation).Load();
                context.Entry(mr.Organisation).Collection(o => o.EditorRoles).Load();
                // Determine the equivalent editor roles for the manager roles (skip the AgentManager role!)
                var equivalentEditorRoles = mr.Roles.Where(r => r != ManagerRole.AgentManager)
                    .Select(GetEquivalentEditorRole).ToList();
                // For a given organisation, iterate through editor roles for resources
                foreach (var er in mr.Organisation.EditorRoles) {
                    context.Entry(er).Reference(e => e.ResourceLink).Load();
                    if (GetManageableResource(er.ResourceLink).Uri == resource.Uri) {
                        // Add those editor roles for the organisation where the person has the manager role
                        roles = roles.Union(er.Roles
                            .Where(r => equivalentEditorRoles.Contains(r))).ToList();
                        break;
                    }
                }
            }
        }
        
        // Check if the agent has editor roles for the parent catalogues (recursively)
        if (resource is Catalogue cat)
            context.Entry(cat).Reference(c => c.PartOf).Load();
        else  if (resource is CataloguedResource res)
            context.Entry(res).Reference(cr => cr.Catalogue).Load();
        Catalogue? parentCatalogue = resource is OwnableResource or ? or.GetParent() : null;
        if (null == parentCatalogue) return roles;
        roles = roles.Union(GetAgentRolesForResource(parentCatalogue, agent)).ToList();
        return roles;
    }
    
    private ManageableResource GetManageableResource(ResourceEditorshipsLink resourceEditorshipsLink)
    {
        ManageableResource? result = context.OwnableResources
            .Include(r => r.EditorshipsLink)
            .SingleOrDefault(r => r.EditorshipsLink.Id == resourceEditorshipsLink.Id);
        if (null != result) return result;
        result = context.SchemaSeries
            .Include(r => r.EditorshipsLink)
            .SingleOrDefault(r => r.EditorshipsLink.Id == resourceEditorshipsLink.Id);
        if (null != result) return result;
        result = context.AcquisitionApps
            .Include(r => r.EditorshipsLink)
            .SingleOrDefault(r => r.EditorshipsLink.Id == resourceEditorshipsLink.Id);
        if (null != result) return result;
        result = context.AppTemplates
            .Include(r => r.EditorshipsLink)
            .SingleOrDefault(r => r.EditorshipsLink.Id == resourceEditorshipsLink.Id);
        if (null != result) return result;
        throw new ArgumentException("ResourceEditors does not belong to any manageable resource");
    }

    private ICollection<ManagerRole>? GetPersonRolesForAgent(Agent agent, Person person)
    {
        if (agent is Organisation org) {
            context.Entry(org).Collection(o => o.MemberRoles).Load();
            foreach (var member in org.MemberRoles) {
                context.Entry(member).Reference(m => m.Person).Load();
                if (member.Person.Uri == person.Uri) return member.Roles;
            }
            return null;
        }
        ICollection<ManagerRole>? roles = null;
        Person checkedPerson = (Person) agent;
        context.Entry(checkedPerson).Collection(p => p.MemberRoles).Load();
        foreach (Membership member in checkedPerson.MemberRoles) {
            context.Entry(member).Reference(m => m.Organisation).Load();
            var rolesInOrg = GetPersonRolesForAgent(member.Organisation, person);
            if (null == roles) roles = rolesInOrg;
            else if (null != rolesInOrg) roles = roles.Union(rolesInOrg).ToList();
        }
        return roles;
    }

    public ICollection<EditorRole> GetUserRoles(ManageableResource resource, UserAccount? account)
    {
        ICollection<EditorRole> roles = new Collection<EditorRole>();
        if (null == account) return roles;
        // Check if the account has the global role that allows editing
        if (account.Roles.Contains(GlobalRole.AgentAdmin))
            roles = GetEquivalentEditorRoles(GlobalRole.AgentAdmin);
        if (account.Roles.Contains(GlobalRole.DataAdmin))
            roles = roles.Union(GetEquivalentEditorRoles(GlobalRole.DataAdmin)).ToList();
        context.Entry(account).Reference(a => a.Person).Load();
        if (null == account.Person) return roles;
        roles = roles.Union(GetAgentRolesForResource(resource,account.Person)).ToList();
        return roles;
    }
    
    public ICollection<ManagerRole>? GetUserRoles(Agent agent, UserAccount? account, bool visibilityCheck = false)
    {
        ICollection<ManagerRole> roles = new Collection<ManagerRole>();
        if (null == account) return roles;
        // Check if the account has the global role that allows editing
        if (account.Roles.Contains(GlobalRole.AgentAdmin))
            roles = GetEquivalentManagerRoles(GlobalRole.AgentAdmin);
        if (account.Roles.Contains(GlobalRole.DataAdmin))
            roles = roles.Union(GetEquivalentManagerRoles(GlobalRole.DataAdmin)).ToList();
        context.Entry(account).Reference(a => a.Person).Load();
        if (null == account.Person) return roles;
        
        if (account.Person.Uri == agent.Uri)
            // If the agent is the same as the person, return the roles directly
            roles = roles.Union([ManagerRole.AgentManager]).ToList();
        
        // Check if the 'account' agent has any roles for the agent 
        var agentRoles = GetPersonRolesForAgent(agent, account.Person);
        if (null == agentRoles)
            return 0 == roles.Count && visibilityCheck ? null : roles;
        return roles.Union(agentRoles).ToList();
    }
    
    private EditorRole GetEquivalentEditorRole(ManagerRole managerRole)
    {
        return managerRole switch {
            ManagerRole.DistributionManager => EditorRole.DistributionEditor,
            ManagerRole.MetadataManager => EditorRole.MetadataEditor,
            ManagerRole.OwnershipManager => EditorRole.OwnershipEditor,
            _ => throw new ArgumentOutOfRangeException(nameof(managerRole), managerRole, null)
        };
    }

    private ICollection<EditorRole> GetEquivalentEditorRoles(GlobalRole role)
    {
        return role switch {
            GlobalRole.DataAdmin => [EditorRole.DistributionEditor, EditorRole.MetadataEditor,
                EditorRole.OwnershipEditor],
            GlobalRole.AgentAdmin => [EditorRole.OwnershipEditor],
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown global role")
        };
    }
    
    private ICollection<ManagerRole> GetEquivalentManagerRoles(GlobalRole role)
    {
        return role switch {
            GlobalRole.DataAdmin => [ManagerRole.DistributionManager, ManagerRole.MetadataManager,
                ManagerRole.OwnershipManager],
            GlobalRole.AgentAdmin => [ManagerRole.AgentManager, ManagerRole.OwnershipManager],
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown manager role")
        };
    }
    
    public void SetUserRoles(ManageableResource resource, string? userId)
    {
        resource.UserRoles = GetUserRoles(resource, GetUserAccount(userId));
    }
    
    public void SetUserRoles(IEnumerable<ManageableResource> resources, string? userId)
    {
        foreach (ManageableResource resource in resources)
            resource.UserRoles = GetUserRoles(resource, GetUserAccount(userId));
    }
    
    public bool CheckVisibilityAndSetRoles(ManageableResource resource, string? userId)
    {
        SetUserRoles(resource, userId);
        if (0 == resource.UserRoles!.Count && !IsGloballyVisible(resource)) return false;
        return true;
    }
    
    public bool CheckVisibilityAndSetRoles(Distribution distribution, string? userId)
    {
        return CheckVisibilityAndSetRoles(distribution.Dataset, userId) || 
            null != distribution.DataService && DatasetStatus.Deleted != distribution.Dataset.Status 
            && CheckVisibilityAndSetRoles(distribution.DataService, userId);
    }
    
    public void SetUserRoles(IEnumerable<Agent> agents, string? userId)
    {
        foreach (Agent agent in agents)
            agent.UserRoles = GetUserRoles(agent, GetUserAccount(userId));
    }
    
    public bool CheckVisibilityAndSetRoles(Agent agent, string? userId)
    {
        var roles = GetUserRoles(agent, GetUserAccount(userId), true);
        agent.UserRoles = roles ?? [];
        return null != roles;
    }

    public bool CheckSchemaAuthorisation(UserAccount account)
    {
        if (account.Roles.Contains(GlobalRole.DataAdmin)) return true;
        context.Entry(account).Reference(a => a.Person).Load();
        if (null != account.Person) {
            context.Entry(account.Person).Collection(p => p.EditorRoles).Load();
            // Check if the person has MetadataEditor privilege to any Catalogue
            foreach (Editorship e in account.Person.EditorRoles
                         .Where(er => er.Roles.Contains(EditorRole.MetadataEditor))) {
                context.Entry(e).Reference(er => er.ResourceLink).Load();
                if (context.Catalogues.Any(c => c.EditorshipsLink.Id == e.ResourceLink.Id))
                    return true;
            }
            
            context.Entry(account.Person).Collection(p => p.MemberRoles).Load();
            foreach (Membership m in account.Person.MemberRoles
                         .Where(mr => mr.Roles.Contains(ManagerRole.MetadataManager))) {
                // the person has a role in an organisation that allows to manage schemas
                context.Entry(m).Reference(mr => mr.Organisation).Load();
                context.Entry(m.Organisation).Collection(o => o.EditorRoles).Load();
                
                // Check if the organisation has MetadataEditor privilege to any Catalogue
                foreach (Editorship e in m.Organisation.EditorRoles
                             .Where(er => er.Roles.Contains(EditorRole.MetadataEditor))) {
                    context.Entry(e).Reference(er => er.ResourceLink).Load();
                    if (context.Catalogues.Any(c => c.EditorshipsLink.Id == e.ResourceLink.Id))
                        return true;
                }
            }
        }
        return false;
    }
}
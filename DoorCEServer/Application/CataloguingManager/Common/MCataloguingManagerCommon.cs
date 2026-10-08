using System.Text.RegularExpressions;
using AutoMapper;
using DoorCEModel.Infrastructure;
using DoorCEModel.Infrastructure.DataModel;
using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEModel.Infrastructure.DataModel.Datasets;
using DoorCEServer.Application.CataloguingManager.Domain;
using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Application.DataTemplateManager.Dtos;
using Microsoft.EntityFrameworkCore;

namespace DoorCEServer.Application.CataloguingManager.Common;

public class MCataloguingManagerCommon(IMapper mapper, ApplicationDbContext context, MAgentsCommon agents)
{
    public void HandleResourceContacts(OwnableResource resource, bool isOld,
        XOwnableResource xResource, IEnumerable<XContactData> newContacts)
    {
        // Update the responsible person and organisation
        Person? responsiblePerson = null;
        if (!string.IsNullOrEmpty(xResource.ResponsiblePersonUri)) {
            responsiblePerson = context.Persons
                .Include(p => p.Contacts)
                .AsSplitQuery()
                .SingleOrDefault(p => p.Uri == xResource.ResponsiblePersonUri);
            if (null == responsiblePerson)
                throw new ArgumentException("Responsible person not found");
        }

        if (isOld) context.Entry(resource).Reference(r => r.ResponsiblePerson).Load();
        resource.ResponsiblePerson = responsiblePerson;

        Organisation? responsibleOrganisation = null;
        if (!string.IsNullOrEmpty(xResource.ResponsibleOrganisationUri)) {
            responsibleOrganisation = context.Organisations
                .Include(o => o.Contacts)
                .AsSplitQuery()
                .SingleOrDefault(o => o.Uri == xResource.ResponsibleOrganisationUri);
            if (null == responsibleOrganisation) 
                throw new ArgumentException("Responsible organisation not found");
        }

        if (isOld) context.Entry(resource).Reference(r => r.ResponsibleOrganisation).Load();
        resource.ResponsibleOrganisation = responsibleOrganisation;

        // Create a list that contains all new contacts that are prepared to be added to the database
        List<XContactData> xContactDataList = newContacts.ToList();
        List<ContactData> updatedContacts = xContactDataList.Select(c => UpsertContactData(c, false)).ToList();

        // Remove from the database contact data to be removed from the dataset that have no agents attached 
        if (isOld) {
            context.Entry(resource).Collection(r => r.Contacts).Load();
            foreach (ContactData contactData in resource.Contacts)
                if (!xResource.ContactsUris.Contains(contactData.Uri)
                    && xContactDataList.All(c => c.Uri != contactData.Uri)) {
                    context.Entry(contactData).Reference(c => c.Agent).Load();
                    if (null == contactData.Agent)
                        context.Entry(contactData).State = EntityState.Deleted;
                }
        }

        // Create a list that contains existing contacts already attached to the dataset
        List<ContactData> existingContacts = context.ContactDatas
            .Where(c => xResource.ContactsUris.Contains(c.Uri)).ToList();
        if (existingContacts.Count != xResource.ContactsUris.Count)
            throw new ArgumentException("Some contacts not found");
        updatedContacts.AddRange(existingContacts);

        resource.Contacts = updatedContacts;
    }

    public void HandleResourceEditors(ManageableResource resource, bool isOld,
        XManageableResource xResource)
    {
        // Update the editors
        ICollection<Editorship> editorRoles = new List<Editorship>();
        if (0 != xResource.EditorsUris.Count) {
            IQueryable<Agent> agentList = context.Agents
                .Where(a => xResource.EditorsUris.Contains(a.Uri));
            if (agentList.Count() != xResource.EditorsUris.Count)
                throw new ArgumentException("Some editors not found");
            // TODO - uncomment this when "database populate" is updated to have accounts for all persons
            // if (agentList.Any(a => a is Person && null == ((Person)a).Account))
            //    throw new Exception("Some editors are persons without accounts");
            if (xResource.EditorsUris.Any(e => !xResource.EditorsRoles.ContainsKey(e)))
                throw new ArgumentException("Some editors roles not found");
            editorRoles = agentList.Select(a => new Editorship() {
                Agent = a,
                ResourceLink = resource.EditorshipsLink,
                Roles = xResource.EditorsRoles[a.Uri]
            }).ToList();
        }

        if (isOld) {
            context.Entry(resource).Reference(r => r.EditorshipsLink).Load();
            context.Entry(resource.EditorshipsLink).Collection(r => r.EditorRoles).Load();
        } else resource.EditorshipsLink = new ResourceEditorshipsLink();

        resource.EditorshipsLink.EditorRoles = editorRoles;
        context.Entry(resource).Reference(r => r.EditorshipsLink).Load();
        context.Entry(resource.EditorshipsLink).Collection(r => r.Editors).Load();
    }

    public Catalogue HandleParentAndContacts(XCataloguedResource xResource, UserAccount account,
        CataloguedResource resource, bool isUpdate, IEnumerable<XContactData> newContacts)
    {
        if (string.IsNullOrEmpty(xResource.CatalogueUri))
            throw new ArgumentException("Resource must have a catalogue");
                
        // Get the parent catalogue
        Catalogue? designatedParent = context.Catalogues
            .SingleOrDefault(d => d.Uri == xResource.CatalogueUri);
        // Check if the parent catalogue exists
        if (null == designatedParent)
            throw new ArgumentException("Parent catalogue not found");
        designatedParent.UserRoles = agents.GetUserRoles(designatedParent, account);

        // Prevent from inserting a resource (new or being moved) into the parent catalogue
        // if the user does not have the right roles
        if (!isUpdate || resource.Catalogue.Uri != designatedParent.Uri) // parent is to be changed?
            if (!designatedParent.HasMetadataRole())
                throw new UnauthorizedAccessException(
                    $"User not permitted to add a new resource in this catalogue [{xResource.Uri}]");
                
        // Update contact data for this dataset series
        HandleResourceContacts(resource, isUpdate, xResource, newContacts);

        return designatedParent;
    }

    public Dataset HandleParents(XDistribution xDistribution, UserAccount account, Distribution? oldVersion)
    {
        if (string.IsNullOrEmpty(xDistribution.DatasetUri))
            throw new ArgumentException("Distribution must have a dataset");
        
        // Get the parent dataset
        Dataset? designatedParent = context.Datasets
            .SingleOrDefault(d => d.Uri == xDistribution.DatasetUri);
        // Check if the parent dataset exists
        if (null == designatedParent) throw new ArgumentException("Parent dataset not found");
        designatedParent.UserRoles = agents.GetUserRoles(designatedParent, account);
                
        // Prevent from inserting a new distribution into the parent dataset
        // if the user does not have the right roles
        if (null == oldVersion || oldVersion.Dataset.Uri != designatedParent.Uri) // parent is to be changed?
            if (!designatedParent.HasDistributionRole())
                throw new UnauthorizedAccessException(
                    $"User not permitted to add a new distribution in this catalogue [{xDistribution.Uri}]");

        return designatedParent;
    }
    
    public SchemaSeries HandleParents(XDataSchema xDataSchema, UserAccount account, DataSchema? oldVersion)
    {
        if (string.IsNullOrEmpty(xDataSchema.SeriesUri))
            throw new ArgumentException("Data schema must be in a series");
        
        // Get the parent series
        SchemaSeries? designatedParent = context.SchemaSeries
            .SingleOrDefault(d => d.Uri == xDataSchema.SeriesUri);
        // Check if the parent series exists
        if (null == designatedParent) throw new ArgumentException("Parent series not found");
        designatedParent.UserRoles = agents.GetUserRoles(designatedParent, account);
                
        // Prevent from inserting a new data schema into the parent series
        // if the user does not have the right roles
        if (null == oldVersion || oldVersion.InSeries.Uri != designatedParent.Uri) // parent is to be changed?
            if (!designatedParent.HasMetadataRole())
                throw new UnauthorizedAccessException(
                    $"User not permitted to add a new data schema in this series [{xDataSchema.Uri}]");

        return designatedParent;
    }

    public ContactData UpsertContactData(XContactData xContact, bool updateAllowed,
        Agent? agent = null)
    {
        ContactData requestedVersion = mapper.Map<ContactData>(xContact);

        // Trying to update contact data for an existing agent?
        if (!updateAllowed && (null != xContact.AgentUri || null != agent))
            throw new InvalidOperationException("Agent exists - unauthorised update");

        // Check if contact data already exists and if so - get it
        ContactData? existingVersion = context.ContactDatas
            .Include(c => c.Agent)
            .AsSplitQuery()
            .SingleOrDefault(c => c.Uri == requestedVersion.Uri);

        // Check if this contact is part of agent data and if so - get the agent
        if (null == agent && null != xContact.AgentUri)
            agent = context.Agents
                .SingleOrDefault(c => c.Uri == xContact.AgentUri);

        // Old version does not exist? - Add the new contact
        if (null == existingVersion) {
            context.ContactDatas.Add(requestedVersion);
            if (null != agent) agent.Contacts.Add(requestedVersion);
            return requestedVersion;
        }

        // Trying to update contact data for an existing agent (sneaky)?
        if (!updateAllowed && null != existingVersion.Agent)
            throw new InvalidOperationException("Agent exists - sneaky unauthorised update");

        // Otherwise - Update the old contact with new data
        requestedVersion.Id = existingVersion.Id; // make sure to keep the same ID
        context.Entry(existingVersion).CurrentValues.SetValues(requestedVersion);
        context.Entry(existingVersion).State = EntityState.Modified;
        if (null != agent) agent.Contacts.Add(existingVersion);
        else if (null == xContact.AgentUri)
            existingVersion.Agent = null;
        return existingVersion;
    }
    
    public bool IsSameOrChildCatalogue(Catalogue child, Catalogue parent)
    {
        if (child.Uri == parent.Uri) return true; // same catalogue
        context.Entry(child).Reference(c => c.PartOf).Load(); // load parent catalogue
        if (null == child.PartOf) return false; // no parent catalogue
        if (child.PartOf.Uri == parent.Uri) return true; // direct parent
        return IsSameOrChildCatalogue(child.PartOf, parent); // recursive check
    }

    public bool CheckAndGenerateUri(XIdentifiableElement xElement)
    {
        string prefix;
        switch (xElement) {
            case XCatalogue: prefix = "cat"; break;
            case XDataset: prefix = "dat"; break;
            case XDatasetSeries: prefix = "ser"; break;
            case XDataService: prefix = "srv"; break;
            case XDistribution: prefix = "dis"; break;
            case XSchemaSeries: prefix = "scs"; break;
            case XDataSchema: prefix = "sch"; break;
            case XPerson: prefix = "prs"; break;
            case XOrganisation: prefix = "org"; break;
            case XAcquisitionApp: prefix = "app"; break;
            case XAppTemplate: prefix = "apt"; break;
            default:
                throw new ArgumentOutOfRangeException(nameof(xElement), xElement, "Unknown resource type");
        }
        
        string? title = null;
        // Generate unique identifier if needed
        if (String.IsNullOrEmpty(xElement.Uri)) {
            Dictionary<string, string>? titles = null;
            switch (xElement) {
                case XOwnableResource xor: titles = xor.Title; break;
                case XDistribution xd: titles = xd.Title; break;
                case XSchemaSeries xss: title = xss.Title; break;
                case XDataSchema xds: title = xds.Title; break;
                case XPerson xp: title = xp.FamilyName; break;
                case XOrganisation xo: title = xo.Name; break;
                case XAcquisitionApp xaa: title = xaa.Title; break;
                case XAppTemplate xat: title = xat.Title; break;
                default: throw new ArgumentException("Critical error - should not happen");
            }

            title ??= (titles!.Keys.Contains("en") ? titles["en"] : titles.First().Value);
            switch (xElement) {
                case XOwnableResource: xElement.Uri = GenerateUri(prefix, title, context.OwnableResources); break;
                case XDistribution: xElement.Uri = GenerateUri(prefix, title, context.Distributions); break;
                case XSchemaSeries: xElement.Uri = GenerateUri(prefix, title, context.SchemaSeries); break;
                case XDataSchema: xElement.Uri = GenerateUri(prefix, title, context.DataSchemas); break;
                case XPerson: xElement.Uri = GenerateUri(prefix, title, context.Persons); break;
                case XOrganisation: xElement.Uri = GenerateUri(prefix, title, context.Organisations); break;
                case XAcquisitionApp: xElement.Uri = GenerateUri(prefix, title, context.AcquisitionApps); break;
                case XAppTemplate: xElement.Uri = GenerateUri(prefix, title, context.AppTemplates); break;
                default: throw new ArgumentException("Critical error - should not happen");
            }
            return true;
        }
        return false;
    }
    
    public string GenerateUri<T>(string prefix, string baseName, DbSet<T> dbSet) where T : class, IdentifiableElement
    {
        baseName = Regex.Replace(baseName.ToLower(), @"[^a-zA-Z0-9]+", "_");
        string uri = prefix + "/" + baseName;
        int i = 1;
        while (dbSet.Any(e => e.Uri == uri)) {
            uri = prefix + "/" + baseName + i;
            i++;
        }
        return uri;
    }
    
    public void CheckIcon(XOwnableResource xResource)
    {
        // Validate if the data file exists
        if (null != xResource.IconUri && !context.Icons.Any(i => i.Uri == xResource.IconUri))
            throw new ArgumentException("Icon not found");
    }
}
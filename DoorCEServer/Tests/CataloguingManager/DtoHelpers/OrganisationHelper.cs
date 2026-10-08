using DoorCEModel.Infrastructure;
using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Application.CataloguingManager.Interfaces;
using DoorCEServer.Common.Exceptions;
using DoorCEServer.Tests.CataloguingManager.Labels;
using DoorCEServer.Tests.CataloguingManager.MockDtos;
using Microsoft.EntityFrameworkCore;

namespace DoorCEServer.Tests.CataloguingManager.DtoHelpers;

public class OrganisationHelper(ApplicationDbContext context, AgentAPI agentApi, TestCommon testCommon)
{
    private readonly XOrganisationMock _xOrgMock = new();
    
    public XAgent? GetFromDb(OrgLabel organisationLabel)
    {
        XOrganisation xOrg = _xOrgMock.Get(organisationLabel, VariantLabel.AfterUpsert);
        try {
            return agentApi.GetAgent(xOrg.Uri, "udas-admin");
        } catch (NotFoundOrVisibleException) { return null; }
    }

    public Organisation GetModel(OrgLabel orgLabel) {
        XOrganisation xOrg = _xOrgMock.Get(orgLabel, VariantLabel.AfterUpsert);

        Agent? agent = context.Agents
            .Include(a => a.Contacts)
            .Include(a => ((Person)a).MemberRoles)
            .Include(a => ((Person)a).Organisations)
            .Include(a => ((Person)a).Account)
            .Include(a => ((Organisation)a).MemberRoles)
            .Include(a => ((Organisation)a).Members)
            .AsSplitQuery()
            .SingleOrDefault(a => a.Uri == xOrg.Uri);
        
        if (null == agent)
            throw new NotFoundOrVisibleException($"Organisation {orgLabel} not found");

        return agent as Organisation ?? throw new InvalidOperationException();
    }

    public void Upsert(OrgLabel organisationLabel, VariantLabel orgVariantLabel, ContactsLabel contactsLabel)
    {
        XOrganisation xOrg = _xOrgMock.Get(organisationLabel, orgVariantLabel);
        List<XContactData> xContacts = _xOrgMock.GetContacts(organisationLabel, contactsLabel);
        
        agentApi.UpsertOrganisation(xOrg, xContacts, "udas-admin");
    }

    public void Delete(OrgLabel organisationLabel)
    {
        XOrganisation xOrg = _xOrgMock.Get(organisationLabel, VariantLabel.AfterUpsert);
        agentApi.DeleteAgent(xOrg.Uri!, "udas-admin");
    }
    
    public bool ValidateDelete(OrgLabel orgLabel, VariantLabel variantLabel)
    {
        XOrganisation orgBeforeDelete = _xOrgMock.Get(orgLabel, variantLabel);
        XAgent? storedOrg = GetFromDb(orgLabel);
        
        // check if agent was deleted
        if (null != storedOrg) return false;

        // check if contacts were deleted
        return orgBeforeDelete.ContactsUris.All(contactUri => !context.ContactDatas.Any(c => c.Uri == contactUri));
    }

    public bool Validate(XOrganisation xOrganisation, OrgLabel organisationLabel, VariantLabel variantLabel)
    {
        return testCommon.CheckOrganisationEquality(xOrganisation, _xOrgMock.Get(organisationLabel, variantLabel));
    }
}
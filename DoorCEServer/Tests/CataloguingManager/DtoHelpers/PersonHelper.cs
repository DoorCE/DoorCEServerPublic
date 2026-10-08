using DoorCEModel.Infrastructure;
using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Application.CataloguingManager.Interfaces;
using DoorCEServer.Common.Exceptions;
using DoorCEServer.Tests.CataloguingManager.Labels;
using DoorCEServer.Tests.CataloguingManager.MockDtos;

namespace DoorCEServer.Tests.CataloguingManager.DtoHelpers;

public class PersonHelper(ApplicationDbContext context, AgentAPI agentApi, TestCommon testCommon)
{
    private readonly XPersonMock _xPersonMock = new();
    
    public XAgent? GetFromDb(PersonLabel personLabel)
    {
        XPerson xPerson = _xPersonMock.Get(personLabel, VariantLabel.AfterUpsert);
        try {
            return agentApi.GetAgent(xPerson.Uri, "udas-admin");
        } catch (NotFoundOrVisibleException) { return null; }
    }

    public void Upsert(PersonLabel personLabel, VariantLabel variantLabel, ContactsLabel contactsLabel)
    {
        XPerson xPerson = _xPersonMock.Get(personLabel, variantLabel);
        List<XContactData> xContacts = _xPersonMock.GetContacts(personLabel, contactsLabel);
        
        agentApi.UpsertPerson(xPerson, xContacts, "udas-admin");
    }

    public void Delete(PersonLabel personLabel)
    {
        XPerson xPerson = _xPersonMock.Get(personLabel, VariantLabel.AfterUpsert);
        agentApi.DeleteAgent(xPerson.Uri!, "udas-admin");
    }

    public bool ValidateDelete(PersonLabel personLabel, VariantLabel variantLabel)
    {
        XPerson personBeforeDelete = _xPersonMock.Get(personLabel,variantLabel);
        XAgent? storedPerson = GetFromDb(personLabel);
        
        // check if agent was deleted
        if (null != storedPerson) return false;

        // check if contacts were deleted
        return personBeforeDelete.ContactsUris.All(contactUri => !context.ContactDatas.Any(c => c.Uri == contactUri));
    }

    public bool Validate(XPerson xPerson, PersonLabel personLabel, VariantLabel variantLabel)
    {
        return testCommon.CheckPersonEquality(xPerson, _xPersonMock.Get(personLabel, variantLabel));
    }
}
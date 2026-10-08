namespace DoorCEServer.Tests;

public enum VariantLabel : short
{
    BeforeUpsert,
    AfterUpsert,
    ModifiedBeforeUpsert,
    ModifiedAfterUpsert,
    
    WithExtraPersonContacts,
    WithExtraOrgContacts,
    
    ChildCatBeforeUpsert,
    ChildCatAfterUpsert,
}
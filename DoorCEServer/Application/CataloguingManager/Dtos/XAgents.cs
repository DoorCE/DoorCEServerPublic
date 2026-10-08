namespace DoorCEServer.Application.CataloguingManager.Dtos
{
    public class XAgents
    {
        public IEnumerable<XOrganisation> Organisations { get; set; } = new List<XOrganisation>();
        public IEnumerable<XPerson> Persons { get; set; } = new List<XPerson>();
    }
}
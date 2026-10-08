namespace DoorCEServer.Application.CataloguingManager.Dtos
{
	public class XOrganisationWithContacts
	{
		public required XOrganisation Organisation { get; set; }
        public required IEnumerable<XContactData> NewContacts { get; set; }
	}
}
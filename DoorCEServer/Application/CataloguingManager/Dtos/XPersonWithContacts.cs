namespace DoorCEServer.Application.CataloguingManager.Dtos
{
	public class XPersonWithContacts
	{
		public required XPerson Person { get; set; }
        public required IEnumerable<XContactData> NewContacts { get; set; }
	}
}
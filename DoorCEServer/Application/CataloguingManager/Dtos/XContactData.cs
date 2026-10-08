namespace DoorCEServer.Application.CataloguingManager.Dtos
{
	public class XContactData : XIdentifiableElement {
		public required string Name { get; set; }
		public required string Contents { get; set; }
		public string? AgentUri { get; set; }
		public string? AgentName { get; set; }
	}
}
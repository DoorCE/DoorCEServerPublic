namespace DoorCEServer.Application.DataTemplateManager.Dtos;

public class XAppCreationRequest : XAcquisitionApp {
	public IEnumerable<XSourceCreationRequest> NewResources { get; set; } = new List<XSourceCreationRequest>();
	public string? SourceCatalogueUri { get; set; }
}
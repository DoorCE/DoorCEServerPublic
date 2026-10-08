namespace DoorCEServer.Application.DataTemplateManager.Dtos;

public class XSourceCreationRequest {
	public required string SourceDatasetName { get; set; }
	public required IEnumerable<string> AgentUris{ get; set; }
}
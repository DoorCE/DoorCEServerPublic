namespace DoorCEServer.Application.AppGenProxy.Dtos {
	public class AppDeploymentRequestDto {
		public required string AppUri{ get; set; }
		public required string TemplateUri{ get; set; }
		public required string ActiveResourceUri{ get; set; }
		public required IEnumerable<string> SourceResourceUris{ get; set; }
	}
}
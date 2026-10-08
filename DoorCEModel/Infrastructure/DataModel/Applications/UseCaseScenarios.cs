namespace DoorCEModel.Infrastructure.DataModel.Applications
{
	public class UseCaseScenarios
	{
		public int Id { get; set; }
		
		public required string UseCaseName { get; set; }
		public required string ScenariosContents { get; set; }
	}
}
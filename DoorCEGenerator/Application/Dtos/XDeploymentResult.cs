namespace DoorCEGenerator.Application.Dtos;

public class XDeploymentResult
{
    public required string AppUri { get; set; }
    public required short Status { get; set; }
    public string Log { get; set; } = "";
}
using DoorCEModel.Infrastructure.DataModel.Agents;

namespace DoorCEServer.Application.CataloguingManager.Dtos;

public class XUserAccount
{
    public required string UserId { get; set; }
    public ICollection<GlobalRole> Roles { get; set; } = new List<GlobalRole>();
    public string? PersonUri { get; set; }
    public string? PersonFullName { get; set; }
}
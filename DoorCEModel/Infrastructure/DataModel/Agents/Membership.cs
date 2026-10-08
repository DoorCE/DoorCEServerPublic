namespace DoorCEModel.Infrastructure.DataModel.Agents;

public class Membership
{
    public ICollection<ManagerRole> Roles { get; set; } = new List<ManagerRole>();
    public required Person Person { get; set; }
    public required Organisation Organisation { get; set; }
}
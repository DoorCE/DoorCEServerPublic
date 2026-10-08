namespace DoorCEModel.Infrastructure.DataModel.Agents;

public class UserAccount
{
	public int Id { get; set; }
	public required string UserId { get; set; }
	public ICollection<GlobalRole> Roles { get; set; } = new List<GlobalRole>();
	public Person? Person { get; set; }
}
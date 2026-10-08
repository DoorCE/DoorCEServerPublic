namespace DoorCEModel.Infrastructure.DataModel.BinaryContents;

public class Icon
{
    public int Id { get; set; }
    public required string Uri { get; set; }
    public required string Name { get; set; }
    public required string ContentType { get; set; }
    public required byte[] Content { get; set; }
    public DateTime ModificationDate { get; set; }
}
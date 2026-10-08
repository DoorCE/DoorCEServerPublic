namespace DoorCEModel.Infrastructure.DataModel.BinaryContents;

public class DataFile
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string ContentType { get; set; }
    public required byte[] Content { get; set; }
    public DateTime ModificationDate { get; set; }
}
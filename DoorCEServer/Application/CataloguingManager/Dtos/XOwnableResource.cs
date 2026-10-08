namespace DoorCEServer.Application.CataloguingManager.Dtos
{
    public class XOwnableResource : XManageableResource
    {
        // ***** From XMultiDescriptionElement ************
        public required Dictionary<string, string> Title { get; set; }
        public required Dictionary<string, string> Description { get; set; }
        // ***** End from XMultiDescriptionElement ************
        
        // ==== Metadata properties ====
        public Dictionary<string, string> Path { get; set; } = new();
        public string? IconUri { get; set; }
        public string? ResponsiblePersonUri { get; set; }
        public string? ResponsiblePersonName { get; set; }
        public string? ResponsibleOrganisationUri { get; set; }
        public string? ResponsibleOrganisationName { get; set; }
        public ICollection<string> ContactsUris { get; set; } = new List<string>();
    }
}
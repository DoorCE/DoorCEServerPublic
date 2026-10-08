namespace DoorCEServer.Application.KeycloakAdminProxy.Dtos;

public class AccountDto
{
    public string? Id { get; set; } = "";
    public string Username { get; set; } = "";
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool Enabled { get; set; }
    public List<CredentialDto>? Credentials { get; set; }
}
namespace DoorCEServer.Application.KeycloakAdminProxy.Dtos;

public class CredentialDto
{
    public string Type { get; set; } = "password";
    public string Value { get; set; } = "";
    public bool Temporary { get; set; } = false;
}
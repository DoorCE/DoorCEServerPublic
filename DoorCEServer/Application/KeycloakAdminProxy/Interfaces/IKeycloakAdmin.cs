using DoorCEServer.Application.CataloguingManager.Dtos;

namespace DoorCEServer.Application.KeycloakAdminProxy.Interfaces;

public interface IKeycloakAdmin
{
    bool CheckUserExists(string userId);
    IEnumerable<string> GetUserIds(string? query = null);
    void CreateUser(XPerson xPerson);
}
using DoorCEServer.Application.StorageManager.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace DoorCEServer.Application.StorageManager.Interfaces;

public interface StorageAPI
{
    int UpsertFile(XFileData metadata, IFormFile? file, string userId);
    
    FileContentResult GetFile(int identifier, string? userId);
    
    void DeleteFile(int identifier, string userId);
    
    string UpsertIcon(XIconData metadata, IFormFile? file, string userId);
    
    IEnumerable<XIconData> GetIconList(string? query);
    
    FileContentResult GetIcon(string identifier);
    
    void DeleteIcon(string identifier, string userId);
}
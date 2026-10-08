namespace DoorCEServer.Application.DataContentsManager.Dtos;

public record XFileValidationResult(
    bool IsValid,
    
    // eg. "Tree" : {"Row 1" : ["required: Error text", ....]}
    Dictionary<string, Dictionary<string,List<string>>> Errors 
);
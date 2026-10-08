namespace DoorCEGenerator.Application.Interfaces;

public interface AppGenerationAPI 
{
	string GenerateCodeFromTemplate(string templateId, string codeFramework);
}
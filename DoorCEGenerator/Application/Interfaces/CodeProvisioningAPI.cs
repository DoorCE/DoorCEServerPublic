using DoorCEGenerator.Application.Dtos;

namespace DoorCEGenerator.Application.Interfaces;

public interface CodeProvisioningAPI  {
	Dictionary<string,string> GetTemplateCodePackages(string templateId);
	Dictionary<string,string> GetTemplateCode(string codePackageId);
	Dictionary<string, string> GetTemplateCodeByTemplateUri(string templateUri, string codeFramework);
	void AppDeploymentFinished(XDeploymentResult result);
}
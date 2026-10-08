using DoorCEModel.Infrastructure.DataModel.Applications;

namespace DoorCEServer.Application.AppGenProxy.Interfaces;

public interface IAppGen  {
	void DeployApp(AcquisitionApp app);
	string GenerateCodeFromTemplate(string templateId, string codeFramework);
	string? GetVersion();
}
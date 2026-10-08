using DoorCEModel.Infrastructure.DataModel.Applications;
using DoorCEModel.Infrastructure.DataModel.Datasets;

namespace DoorCEServer.Application.DataTemplateManager.Interfaces;

public interface IAppTemplates
{
	AppTemplate CreateDefaultAppTemplate(Dataset dataset, string language = "en");
	string DeployDefaultApp(Dataset dataset, AppTemplate template);
}
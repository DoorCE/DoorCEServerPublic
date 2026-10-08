using DoorCEModel.Infrastructure.DataAccess;
using DoorCEModel.Infrastructure.DataModel.Applications;

namespace DoorCEGenerator.Infrastructure.DataAccess;

public interface IDataTemplates : ITransactionalAccess
{
    AppTemplate? GetAppTemplateWithSchema(string templateId);
}
using DoorCEModel.Infrastructure;
using DoorCEModel.Infrastructure.DataAccess;
using DoorCEModel.Infrastructure.DataModel.Applications;
using Microsoft.EntityFrameworkCore;

namespace DoorCEGenerator.Infrastructure.DataAccess;

public class MDataTemplates(ApplicationDbContext context) : MDbTransactionalAccess(context), IDataTemplates
{
    public AppTemplate? GetAppTemplateWithSchema(string templateId)
    {
        return context.AppTemplates
            .Include(t => t.Schema)
            .Include(t => t.Schema.UsedNamespaces)
            .Include(t => t.Schema.Concepts)
                .ThenInclude(c => c.Properties)
            .Include(t => t.AuxiliaryConcepts)
                .ThenInclude(c => c.Properties)
            .Include(t => t.DataSpecification)
            .Include(t => t.UseCases)
            .Include(t => t.Versions)
            .Include(c => c.EditorshipsLink)
            .Include(c => c.EditorshipsLink.Editors)
            .Include(c => c.EditorshipsLink.EditorRoles)
            .AsSplitQuery()
            .SingleOrDefault(t => t.Uri == templateId);
    }
}
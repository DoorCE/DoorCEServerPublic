using DoorCEModel.Infrastructure;
using DoorCEModel.Infrastructure.DataAccess;
using DoorCEModel.Infrastructure.DataModel.Applications;
using DoorCEModel.Infrastructure.DataModel.CodeContents;
using Microsoft.EntityFrameworkCore;

namespace DoorCEGenerator.Infrastructure.DataAccess;

public class MCodeRepository(ApplicationDbContext context) : MDbTransactionalAccess(context), ICodeRepository
{
    public CodePackage? GetCodePackageWithFiles(string packageId)
    {
        return context.CodePackages
            .Include(p => p.Files)
            .Include(p => p.Template)
            .AsSplitQuery()
            .SingleOrDefault(p => p.Uri == packageId);
    }
    
    public CodePackage? GetTemplateCodePackageWithFiles(string templateUri, string codeFramework)
    {
        return context.CodePackages
            .Include(p => p.Files)
            .Include(p => p.Template)
            .AsSplitQuery()
            .SingleOrDefault(p => p.Template.Uri == templateUri && p.CodeFramework == codeFramework);
    }
    
    public ICollection<CodePackage> GetTemplateCodePackages(string templateUri)
    {
        return context.CodePackages
            .Include(p => p.Template)
            .Where(p => p.Template.Uri == templateUri)
            .ToList();
    }
    
    public AcquisitionApp? GetAcquisitionAppByUri(string appUri)
    {
        return context.AcquisitionApps
            .SingleOrDefault(a => a.Uri == appUri);
    }
    
    public void UpsertAcquisitionApp(AcquisitionApp requestedVersion)
    {
        var existingVersion = GetAcquisitionAppByUri(requestedVersion.Uri);
        bool isUpdate = null != existingVersion;
        
        if (!isUpdate)
            context.AcquisitionApps.Add(requestedVersion);
        else {
            // Otherwise - Update the old code package with new data (including files)
            requestedVersion.Id = existingVersion!.Id; // make sure to preserve the unique ID
            context.Entry(existingVersion).CurrentValues.SetValues(requestedVersion);
            context.Entry(existingVersion).State = EntityState.Modified;
        }
        context.SaveChanges();
    }

    public string UpsertCodePackage(CodePackage requestedVersion)
    {
        var existingVersion = GetCodePackageWithFiles(requestedVersion.Uri);
        bool isUpdate = null != existingVersion;
        
        if (!isUpdate)
            context.CodePackages.Add(requestedVersion);
        else {
            // Otherwise - Update the old code package with new data (including files)
            requestedVersion.Id = existingVersion!.Id; // make sure to preserve the unique ID
            context.Entry(existingVersion).CurrentValues.SetValues(requestedVersion);
            existingVersion.Files = requestedVersion.Files;
            context.Entry(existingVersion).State = EntityState.Modified;
        }
        requestedVersion.Template.IsReady = true; // Change the status of the template when its code is ready
        context.SaveChanges();
        return requestedVersion.Uri;
    }

    public void DeleteCodePackage(string packageId)
    {
        throw new NotImplementedException();
    }
}
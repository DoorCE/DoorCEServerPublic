using DoorCEGenerator.Application.Dtos;
using DoorCEGenerator.Application.Interfaces;
using DoorCEGenerator.Common.Exceptions;
using DoorCEGenerator.Infrastructure.DataAccess;
using DoorCEModel.Infrastructure.DataModel.Applications;
using DoorCEModel.Infrastructure.DataModel.CodeContents;

namespace DoorCEGenerator.Application.Domain;

public class MCodeProvisioning(ICodeRepository codeService) : CodeProvisioningAPI
{
    public Dictionary<string, string> GetTemplateCodePackages(string templateId)
    {
        ICollection<CodePackage> codePackages = codeService.GetTemplateCodePackages(templateId);
        if (codePackages == null || codePackages.Count == 0)
            throw new NotFoundOrVisibleException($"No code packages found for template with id {templateId}");
        return codePackages.ToDictionary(p => p.CodeFramework, p => p.Uri);
    }

    public Dictionary<string, string> GetTemplateCode(string codePackageId)
    {
        CodePackage codePackage = codeService.GetCodePackageWithFiles(codePackageId)
            ?? throw new NotFoundOrVisibleException($"Code package with id {codePackageId} not found");
        return codePackage.Files.ToDictionary(f => f.Path, f => f.CodeContents);
    }
    
    public Dictionary<string, string> GetTemplateCodeByTemplateUri(string templateUri, string codeFramework= "FLT")
    {
        CodePackage codePackage = codeService.GetTemplateCodePackageWithFiles(templateUri, codeFramework) 
           ?? throw new NotFoundOrVisibleException($"{codeFramework} code package for template {templateUri} not found");
        return codePackage.Files.ToDictionary(f => f.Path, f => f.CodeContents);
    }
    
    public void AppDeploymentFinished(XDeploymentResult result)
    {
        AcquisitionApp app = codeService.GetAcquisitionAppByUri(result.AppUri)
            ?? throw new NotFoundOrVisibleException($"Acquisition app with uri {result.AppUri} not found");
        app.Status = Enum.IsDefined(typeof(AppStatus), result.Status) 
            ? (AppStatus)result.Status 
            : throw new ArgumentException($"Status {result.Status} is invalid");
        app.Log = result.Log;
        codeService.UpsertAcquisitionApp(app);
    }
}
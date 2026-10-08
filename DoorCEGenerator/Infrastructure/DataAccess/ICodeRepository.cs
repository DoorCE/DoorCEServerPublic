using DoorCEModel.Infrastructure.DataAccess;
using DoorCEModel.Infrastructure.DataModel.Applications;
using DoorCEModel.Infrastructure.DataModel.CodeContents;

namespace DoorCEGenerator.Infrastructure.DataAccess;

public interface ICodeRepository : ITransactionalAccess
{
    public CodePackage? GetCodePackageWithFiles(string packageId);
    public CodePackage? GetTemplateCodePackageWithFiles(string templateUri, string codeFramework);
    public ICollection<CodePackage> GetTemplateCodePackages(string templateUri);
    public string UpsertCodePackage(CodePackage codePackage);
    public void DeleteCodePackage(string packageId);
    public AcquisitionApp? GetAcquisitionAppByUri(string appUri);
    public void UpsertAcquisitionApp(AcquisitionApp app);
}
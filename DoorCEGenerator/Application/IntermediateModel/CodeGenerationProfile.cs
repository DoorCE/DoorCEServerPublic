namespace DoorCEGenerator.Application.IntermediateModel;

public class CodeGenerationProfile(ICodeGenerator codeGenerator, INamingConverter namingConverter)
{
    public ICodeGenerator Generator { get; } = codeGenerator;
    public INamingConverter NamingConverter { get; } = namingConverter;
}
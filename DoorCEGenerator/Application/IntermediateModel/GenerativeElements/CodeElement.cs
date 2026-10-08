namespace DoorCEGenerator.Application.IntermediateModel.GenerativeElements;

public class CodeElement(CodeGenerationProfile codeGenerationProfile)
{
    protected readonly CodeGenerationProfile GenerationProfile = codeGenerationProfile;

    // METHODS
    public string ToCode(int tabs = 0)
    {
        return GenerationProfile.Generator.GetCode((dynamic)this, tabs);
    }
}
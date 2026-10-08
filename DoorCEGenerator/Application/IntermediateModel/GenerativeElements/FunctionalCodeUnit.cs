namespace DoorCEGenerator.Application.IntermediateModel.GenerativeElements;

public abstract class FunctionalCodeUnit(CodeGenerationProfile codeGenerationProfile)
    : DistinguishableCodeUnit(codeGenerationProfile)
{
    public string GetInterfaceName()
    {
        return GenerationProfile.NamingConverter.GetInterfaceName(this);
    }
}
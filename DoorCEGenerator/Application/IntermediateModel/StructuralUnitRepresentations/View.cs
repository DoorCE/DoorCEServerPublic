using DoorCEGenerator.Application.IntermediateModel.DataRepresentations;
using DoorCEGenerator.Application.IntermediateModel.GenerativeElements;

namespace DoorCEGenerator.Application.IntermediateModel.StructuralUnitRepresentations;

public class View(CodeGenerationProfile codeGenerationProfile) : FunctionalCodeUnit(codeGenerationProfile)
{
    // ATTRIBUTES
    public required ViewModel ViewModel;
    public required Controller Controller;
    public required Presenter Presenter;
    public readonly List<Trigger> Triggers = [];
    public string Type = "form";
    public string? Label;
}
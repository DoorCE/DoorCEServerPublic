using DoorCEGenerator.Application.Interfaces;
using Antlr4.Runtime;
using DoorCEGenerator.Application.Domain.CodeGenerators;
using DoorCEGenerator.Application.Domain.NamingConverters;
using DoorCEGenerator.Application.IntermediateModel;
using DoorCEGenerator.Application.IntermediateModel.DataRepresentations;
using DoorCEGenerator.Application.IntermediateModel.StructuralUnitRepresentations;
using DoorCEGenerator.Infrastructure.DataAccess;
using DoorCEGenerator.WebApi.Controllers;
using DoorCEModel.Infrastructure.DataModel.Applications;
using DoorCEModel.Infrastructure.DataModel.CodeContents;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEServer.Infrastructure.DataModel.DataSchemas;

namespace DoorCEGenerator.Application.Domain;

public class MAppGeneration(ILogger<AppGenerationController> logger, IDataTemplates templateService, ICodeRepository codeService) : AppGenerationAPI
{
    public string GenerateCodeFromTemplate(string templateId, string codeFramework)
    {
        AppTemplate template = templateService.GetAppTemplateWithSchema(templateId)
                               ?? throw new ArgumentException($"Template with ID {templateId} not found.");

        string input = CreateInput(template);
        
        Dictionary<string,string> conceptUriMap = CreateConceptUriMap(template);
        
        AntlrInputStream inputStream = new AntlrInputStream(input);
        DScriptLexer rslBisLexer = new DScriptLexer(inputStream);
        CommonTokenStream commonTokenStream = new CommonTokenStream(rslBisLexer);
        DScriptParser rslBisParser = new DScriptParser(commonTokenStream);
        
        // Setup error handling for the parser
        var errorListener = new CollectingErrorListener();
        //rslBisParser.RemoveErrorListeners();
        rslBisParser.AddErrorListener(errorListener);

        // Run the parser - generate the abstract syntax tree
        DScriptParser.StartContext startContext = rslBisParser.start();
        
        // Exit with an exception in case of syntax errors
        if (errorListener.HasErrors)
            throw new InvalidOperationException(
                "There were syntax errors in scenarios:\n" + string.Join("\n", errorListener.Errors));
        
        // Setup the intermediate representation generator
        INamingConverter namingConverter = new DefaultNamingConverter();
        ICodeGenerator codeGenerator = codeFramework.ToUpper() switch {
            "RCT" => new ReactCodeGenerator(),
            "FLT" => new FlutterCodeGenerator(namingConverter),
            _ => throw new ArgumentException($"Unsupported code framework: {codeFramework}")
        };
        CodeGenerationProfile generationProfile = new CodeGenerationProfile(codeGenerator, namingConverter);
        IntermediateRepresentationFactory factory = new IntermediateRepresentationFactory(generationProfile);
        IntermediateRepresentationGenerator visitor = 
            new IntermediateRepresentationGenerator(logger, factory, conceptUriMap,template.Title)
            { Verbose = true };
        
        // Generate the intermediate representation 
        IntermediateRepresentation result = visitor.Visit(startContext); // Generate the intermediate representation

        // Generate code from the intermediate representation to database
        try {
            codeService.BeginTransaction();
            // Setup the code package
            CodePackage package = new CodePackage
            {
                Uri = "codepackage://" + templateId + "/" + codeFramework,
                Template = template,
                CodeFramework = codeFramework
            };

            // Generate consecutive code files with respective paths
            List<CodeFile> files = [];
            CodeFile? file;
            foreach (View view in result.Views)
                if (null != (file = view.ToCodeFile("view"))) files.Add(file);
            
            foreach (ViewModel vm in result.ViewModelUnit.Models)
                if (null != (file = vm.ToCodeFile("view/viewmodel"))) files.Add(file);

            foreach (Controller cntrl in result.Controllers)
                if (null != (file = cntrl.ToCodeFile("view/controllers")))
                    files.Add(file);

            foreach (Presenter pc in result.Presenters)
                if (null != (file = pc.ToCodeFile("view/presenters"))) files.Add(file);

            foreach (UseCase uc in result.UseCases)
                if (null != (file = uc.ToCodeFile("usecases"))) files.Add(file);

            foreach (Service svc in result.Services)
                if (null != (file = svc.ToCodeFile("services"))) files.Add(file);
            
            foreach (DataTransferObject dto in result.DataTransferObjectUnit.Objects)
                if (null != (file = dto.ToCodeFile("dtos"))) files.Add(file);
            
            foreach (ResultEnumeration dto in result.DataTransferObjectUnit.Enums)
                if (null != (file = dto.ToCodeFile("dtos"))) files.Add(file);

            if (null != (file = result.DataTransferObjectUnit.ToCodeFile("viewmodel"))) files.Add(file);
            if (null != (file = result.ViewModelUnit.ToCodeFile("viewmodel"))) files.Add(file);

            files.AddRange(codeGenerator.GetAuxiliaryFiles());

            files.Add(result.ToMainCodeFile());

            // Add generated code files the the code package
            package.Files = files;

            // Save the package to the database
            string packageId = codeService.UpsertCodePackage(package);
            codeService.CommitTransaction();
            // If requested - save the files to a local disk
            SaveFilesToDisk(files);
            return packageId;
        } catch (Exception) {
            codeService.RollbackTransaction();
            throw;
        }
    }
    
    private static void SaveFilesToDisk(IEnumerable<CodeFile> files)
    {
        string? rootDirectory = Environment.GetEnvironmentVariable("CODE_OUTPUT_DIR");
        if (null == rootDirectory) return;
        // Remove existing directory and all its contents before writing new files
        if (Directory.Exists(rootDirectory))
            Directory.Delete(rootDirectory, recursive: true);
        foreach (CodeFile file in files) {
            string fullPath = rootDirectory + file.Path.Replace('/', Path.DirectorySeparatorChar);
            string? directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(fullPath, file.CodeContents);
        }
    }

    private string CreateInput(AppTemplate template)
    {

        string input = string.Join("\n\n", template.UseCases
            .Select(uc => $"Use case {uc.UseCaseName} {uc.ScenariosContents}"));
        
        input += "\n\n" + string.Join("\n\n", template.Schema.Concepts.Select(c => c.ToDScript()));
        input += "\n\n" + string.Join("\n\n", template.AuxiliaryConcepts.Select(c => c.ToDScript(true)));
        
        return input;
    }
    
    private Dictionary<string, string> CreateConceptUriMap(AppTemplate template)
    {
        Dictionary<string, string> conceptUriMap = template.Schema.Concepts.Where(c => null != c.Uri)
            .ToDictionary(c => c.Name, c => c.Uri!);
        foreach (Concept auxConcept in template.AuxiliaryConcepts)
            if (1 == auxConcept.Properties.Count && auxConcept.Properties.First().Multiple && 
                auxConcept.Properties.First() is Reference { Type.Uri: not null } reference)
                conceptUriMap[auxConcept.Name] = reference.Type.Uri;
        return conceptUriMap;
    }
    
    private class CollectingErrorListener : BaseErrorListener
    {
        private readonly List<string> _errors = [];

        public bool HasErrors => _errors.Count > 0;
        public IReadOnlyList<string> Errors => _errors;

        public override void SyntaxError(
            TextWriter output, IRecognizer recognizer,
            IToken offendingSymbol, int line, int charPositionInLine,
            string msg, RecognitionException e)
        {
            _errors.Add($"Line {line}:{charPositionInLine} - {msg}");
        }
    }
}


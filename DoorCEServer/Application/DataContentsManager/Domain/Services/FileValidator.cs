using System.Text.Json.Nodes;
using DoorCEModel.Infrastructure;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEServer.Application.DataContentsManager.Dtos;
using DoorCEServer.Infrastructure.DataModel.DataSchemas;
using Json.Schema;
using System.Text.Json;

namespace DoorCEServer.Application.DataContentsManager.Domain.Services;

public class FileValidator(ApplicationDbContext dbContext, UnifiedFile file, Dictionary<string, JsonSchema> sheetSchemas)
{
    private Dictionary<string, Dictionary<string,List<string>>> Errors { get; set; } = new();
    private Dictionary<string, JsonSchema> SheetSchemas { get; set; } = sheetSchemas;

    public XFileValidationResult CheckFileValidity()
    {
        //TODO - result in a format of ordered dict (now some results get mixed)
        ValidateSheetNames();
        ValidateWithSchemas();
        ValidateUniqueFields();
        ValidateIdField();
        ValidateReferences();
        
        return new XFileValidationResult(Errors.Count == 0, Errors);
    }
    
    /// <summary>
    /// Checks for missing sheets.
    /// </summary>
    private void ValidateSheetNames()
    {
        var requiredSheets = SheetSchemas.Keys;
        var requiredSet = new HashSet<string>(requiredSheets, StringComparer.OrdinalIgnoreCase);

        // Check if there are all required sheets
        var existing = file.Sheets.Select(s => s.Concept!.Uri).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var required in requiredSet.Where(required => !existing.Contains(required)))
        {
            AddErrors(required, "sheet names", ["Required sheet not present in a file"]);
        }
    }

    /// <summary>
    /// Validate sheets against per-sheet JSON Schemas
    /// </summary>
    private void ValidateWithSchemas()
    {
        if (file.Sheets.Count == 1 && SheetSchemas.Count == 1)
        {
            ValidateSheet(file.Sheets[0], SheetSchemas.ElementAt(0).Value);
            return;
        }
        
        foreach (var sheet in file.Sheets)
        {
            if (!SheetSchemas.TryGetValue(sheet.Concept!.Uri!, out var schema))
            {
                AddErrors(sheet.Concept.Uri!, "json schemas", ["No schema found for sheet"]);
                continue;
            }

            ValidateSheet(sheet, schema);
        }
    }

    /// <summary>
    /// Custom validation: check uniqueness of a field in a sheet
    /// </summary>
    private void ValidateUniqueFields()
    {
        foreach (var sheet in file.Sheets)
        {
            // skip sheets without unique properties
            if (sheet.UniqueProperties.Count == 0)
                continue;

            foreach (var field in sheet.UniqueProperties)
            {
                var seen = new HashSet<string?>();
                
                // get existing item values 
                seen.UnionWith(file.Dataset!.Items
                    .Where(i => i.Concept.Uri == sheet.Concept!.Uri)
                    .Select(i => i.Values.TryGetValue(field.Name, out var value) 
                        ? value.ToString() : null));

                int rowIndex = 1;
                foreach (var row in sheet.Rows)
                {
                    row.TryGetValue(field.Name, out var value);

                    if (null != value)
                    {
                        String valueString = value is IList<object> listValue
                            ? string.Join(",", listValue)
                            : value.ToString() ?? "";

                        if (!seen.Add(valueString))
                            AddErrors(sheet.Concept!.Uri!, $"Row {rowIndex}",
                            [$"unique: Duplicate value [\"{valueString}\"] in unique property: {field.Name}"]);
                    }

                    rowIndex++;
                }
            }
        }
    }

    /// <summary>
    /// Custom validation: check if reference properties point to existing items
    /// </summary>
    private void ValidateReferences()
    {
        var sheetsWithReferences = file.Sheets.Where(s => s.ReferenceProperties.Count > 0);
        
        foreach (var sheet in sheetsWithReferences)
        {
            foreach (var referenceProp in sheet.ReferenceProperties)
            {
                if (referenceProp is not Reference reference) 
                    throw new InvalidOperationException($"ValidateReferences: Property {referenceProp.Name} is not a reference");

                var targetConcept = reference.Type;
                
                // Get all data item identifiers (already in the db) for the target concept
                var targetIds = file.Dataset!.Items
                    .Where(di => di.ConceptUri == targetConcept.Uri)
                    .Select(di => di.Identifier.ToLowerInvariant())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                
                // If file contains the target concept sheet, include its ids as well
                var targetSheet = file.Sheets.FirstOrDefault(s => s.Concept!.Uri == targetConcept.Uri);
                if (targetSheet != null)
                {
                    foreach (var row in targetSheet.Rows)
                    {
                        if (row.TryGetValue(targetSheet.IdFieldName!, out var idValue))
                        {
                            var idString = idValue.ToString();
                            if (idString != null) targetIds.Add(idString.ToLowerInvariant());
                        }
                    }
                }

                int rowIndex = 1;
                foreach (var row in sheet.Rows)
                {
                    var referenceValue = row.TryGetValue(referenceProp.Name, out var refValue) 
                        ? refValue.ToString() ?? "" 
                        : null;
                    if (referenceValue != null && !targetIds.Contains(referenceValue.ToLowerInvariant()))
                    {
                        AddErrors(sheet.Concept!.Uri!, $"Row {rowIndex}",
                            [$"reference: Invalid reference value [\"{referenceValue}\"] in property: {referenceProp.Name}"]);
                    }
                    rowIndex++;
                }
            }
        }
    }

    /// <summary>
    /// Custom validation: check if id fields are valid and unique within dataset
    /// </summary>
    private void ValidateIdField()
    {
        // get all ids in dataset
        var ids = file.Dataset!.Items.Select(i => i.Identifier.ToString()).ToHashSet();
        
        foreach (var sheet in file.Sheets )
        {
            if (null == sheet.IdFieldName)
                continue;

            var concept = sheet.Concept;
            
            // check custom id field against schema
            if (null != concept!.DefaultIdentifier)
            {
                Property? idProperty = concept.Properties.FirstOrDefault(p => p.Name == sheet.IdFieldName);
                if (null == idProperty)
                {
                    AddErrors(concept.Uri!, "id field", 
                        ["No property has a user-defined id field name"]);
                } else if (!idProperty.Unique || !idProperty.Required)
                {
                    AddErrors(concept.Uri!, "id field",
                        ["User-defined id field is not unique or required"]);
                }
            }

            // check if ids are unique within dataset
            int rowIndex = 1;
            foreach (var row in sheet.Rows)
            {
                if (row.TryGetValue(sheet.IdFieldName, out var idValue))
                {
                    var idString = idValue.ToString();
                    if (idString != null && !ids.Add(idString))
                    {
                        AddErrors(sheet.Concept!.Uri!, $"Row {rowIndex}", 
                            [$"id field: Duplicate id value [\"{idString}\"] found in dataset"]);
                    }
                }
                rowIndex++;
            }
        }
    }
    
    /// <summary>
    /// Validates single sheet with a json schema
    /// </summary>
    private void ValidateSheet(UnifiedSheet sheet, JsonSchema schema)
    {
        // Validate rows one by one
        int index = 1;
        if (sheet.Rows.Count == 0)
        {
            AddErrors(sheet.Concept!.Uri!, $"no data", ["No rows found"]);
            return;
        }
        
        foreach (var row in sheet.Rows) {
            
            // Prepare options
            var options = new EvaluationOptions 
            {
                OutputFormat = OutputFormat.List,
                //ProcessCustomKeywords = true,
                //ValidateAgainstMetaSchema = false, 
            };
            
            // Convert row to JsonNode
            JsonNode instance = JsonSerializer.SerializeToNode(row)!;
            
            // Validate
            var result = schema.Evaluate(instance, options);

            if (!result.IsValid)
            {
                List<string> errorList = [];
                
                foreach (var detail in result.Details)
                {
                    if (detail.Errors is { Count: > 0 })
                    {
                        var errors = detail.Errors.Select(kvp 
                            => $"{(detail.EvaluationPath.Count > 0 
                                ? detail.EvaluationPath.ToString()[1..] 
                                : kvp.Key)}: {kvp.Value}").ToList();
                        errorList.AddRange(errors);
                    }
                }
                AddErrors(sheet.Concept!.Uri!, $"Row {index}", errorList.ToHashSet().ToList());
            }

            index++;
        }
    }
    
    
    private void AddErrors(string sheetName, string key, List<string> errors)
    {
        var errorDict = new Dictionary<string, List<string>>()
        {
            {key, errors}
        };
        
        if (!Errors.TryAdd(sheetName, errorDict))
        {
            if (!Errors[sheetName].TryAdd(key, errors))
                Errors[sheetName][key].AddRange(errors);
        }
    }
}
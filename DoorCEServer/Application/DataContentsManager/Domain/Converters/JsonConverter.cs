using System.Text.Json;
using DoorCEServer.Application.DataContentsManager.Interfaces;

namespace DoorCEServer.Application.DataContentsManager.Domain.Converters;

public class JsonConverter : IFormatConverter
{
    public UnifiedFile ReadFile(Stream fileStream, string? singleSheetName)
    {
        using var doc = JsonDocument.ParseAsync(fileStream).GetAwaiter().GetResult();

        var root = doc.RootElement;

        return root.ValueKind switch
        {
            //TODO - handle files with different structures?
            JsonValueKind.Array => ConvertSingleSheet(root, singleSheetName!),
            JsonValueKind.Object => ConvertMultipleSheets(root),
            _ => throw new FormatException("JSON must be an array or an object.")
        };
    }

    // --------------------------------------------------------------------
    // Format A: [ { row }, { row } ]
    // --------------------------------------------------------------------
    private UnifiedFile ConvertSingleSheet(JsonElement arrayRoot, string singleSheetName)
    {
        var sheet = new UnifiedSheet() {
            Name = singleSheetName,
            Rows = EnumerateRows(arrayRoot)
        };

        return new UnifiedFile(){Sheets = new List<UnifiedSheet> { sheet }};
    }

    // --------------------------------------------------------------------
    // Format B: { "sheet1": [rows], "sheet2": [rows] }
    // --------------------------------------------------------------------
    private UnifiedFile ConvertMultipleSheets(JsonElement objectRoot)
    {
        var sheets = new List<UnifiedSheet>();

        foreach (var property in objectRoot.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Array)
                throw new FormatException($"Sheet '{property.Name}' must contain an array of rows.");

            sheets.Add(new UnifiedSheet() {
                Name = property.Name.ToLowerInvariant(),
                Rows = EnumerateRows(property.Value)
            });
        }

        if (sheets.Count == 0)
        {
            sheets.Add(new UnifiedSheet() {Name = string.Empty, Rows = []});
        }

        return new UnifiedFile(){Sheets = sheets};
    }
    
    private static List<Dictionary<string, object>> EnumerateRows(JsonElement array)
    {
        var list = new List<Dictionary<string, object>>();
        
        foreach (var rowElement in array.EnumerateArray())
        {
            if (rowElement.ValueKind != JsonValueKind.Object)
                throw new FormatException("Each row must be an object.");

            var dict = new Dictionary<string, object>();

            foreach (var prop in rowElement.EnumerateObject())
                if (!string.IsNullOrWhiteSpace(prop.Value.ToString()))
                    dict[prop.Name.ToLowerInvariant()] = ConvertJsonValue(prop.Value);

            list.Add(dict);
        }

        return list;
    }

    // --------------------------------------------------------------------
    // Convert JsonElement -> CLR object
    // --------------------------------------------------------------------
    private static object ConvertJsonValue(JsonElement val)
    {
        
        return val.ValueKind switch
        {
            JsonValueKind.String => val.GetString()!,
            JsonValueKind.Number => val.TryGetInt64(out var l) ? l : val.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            JsonValueKind.Array => val.EnumerateArray().Select(ConvertJsonValue).ToList(),
            JsonValueKind.Object => val.EnumerateObject()
                                       .ToDictionary(p => p.Name, p => ConvertJsonValue(p.Value)),
            _ => val.ToString()
        };
    }
}

    //public List<XDataItem> ToXDataItemList(string jsonContent, string datasetUri, string conceptUri, long? identifier)
    //{
    //    var jsonArray = ToDictList(jsonContent);
    //    var xDataItems = jsonArray.Select(dict 
    //        => new XDataItem 
    //            { DataSetUri = datasetUri, Identifier = identifier, ConceptUri = conceptUri, Values = dict }).ToList();
        
    //    return xDataItems;
    //}

    //public List<Dictionary<string, object>> ToDictList(string jsonContent)
    //{
    //    var jsonArray = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonContent);
        
    //    return jsonArray ?? throw new FormatException("Invalid JSON format.");
    //}
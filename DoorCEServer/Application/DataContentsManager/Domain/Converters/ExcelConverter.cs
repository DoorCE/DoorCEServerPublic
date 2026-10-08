using System.Data;
using DoorCEServer.Application.DataContentsManager.Common;
using DoorCEServer.Application.DataContentsManager.Interfaces;
using ExcelDataReader;

namespace DoorCEServer.Application.DataContentsManager.Domain.Converters;

public class ExcelConverter : IFormatConverter
{
    public UnifiedFile ReadFile(Stream fileStream, string? singleSheetName)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        using var reader = ExcelReaderFactory.CreateReader(fileStream);

        // Read file as dataset
        var dataSet = reader.AsDataSet();
        
        // Create sheets
        var sheets = new List<UnifiedSheet>();
        
        foreach (DataTable table in dataSet.Tables)
        {
            var rows = new List<Dictionary<string, object>>();
            
            if (table.Rows.Count == 0)
            {
                rows = [];
            }
            else
            {
                // First row as headers
                var headers = table.Rows[0].ItemArray.Select(x => x!.ToString()!.ToLowerInvariant()).ToList();
            
                // The rest of rows
                for (int i = 1; i < table.Rows.Count; i++)
                {
                    var rowDict = new Dictionary<string, object>();
                    for (int j = 0; j < table.Columns.Count; j++)
                    {
                        var raw = table.Rows[i][j];
                        var parsed = ParseCellValue(raw);
                        // skip empty/null/whitespace string values
                        if (parsed == null) continue;
                        if (parsed is string s && string.IsNullOrWhiteSpace(s)) continue;

                        var key = headers.ElementAtOrDefault(j) ?? string.Empty;
                        rowDict[key] = parsed;
                    }
                    rows.Add(rowDict);
                }
            }
            var sheet = new UnifiedSheet()
            {
                Name = singleSheetName ?? table.TableName.ToLowerInvariant(), 
                Rows = rows
            };
            sheets.Add(sheet);
        }
        return new UnifiedFile(){Sheets = sheets};
    }
    
    // TODO - handle files without headers
    
    private static object? ParseCellValue(object? value)
    {
        if (value == null || value == DBNull.Value) return null;

        // If original is already a numeric or bool type, return as-is
        switch (value)
        {
            case int _:
            case long _:
            case double _:
            case float _:
            case decimal _:
            case bool _:
                return value;
        }

        var s = value.ToString()?.Trim();
        if (string.IsNullOrEmpty(s)) return null;

        // Detect array pattern [a,b,c]
        if (s.StartsWith("[") && s.EndsWith("]"))
        {
            var inner = s.Substring(1, s.Length - 2).Trim();
            if (string.IsNullOrEmpty(inner)) return new List<object>();

            var parts = MDataContentsManagerCommon.SplitArrayElements(inner);
            var list = parts.Select(MDataContentsManagerCommon.ParsePrimitive)!.ToList<object>();
            return list;
        }

        return MDataContentsManagerCommon.ParsePrimitive(s);
    }
}
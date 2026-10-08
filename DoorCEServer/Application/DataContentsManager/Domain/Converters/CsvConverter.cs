using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using DoorCEServer.Application.DataContentsManager.Common;
using DoorCEServer.Application.DataContentsManager.Interfaces;

namespace DoorCEServer.Application.DataContentsManager.Domain.Converters;

public class CsvConverter : IFormatConverter
{

    public UnifiedFile ReadFile(Stream fileStream, string? singleSheetName)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        
        using var reader = CreateReader(fileStream);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = true,
            BadDataFound = null,
            MissingFieldFound = null,
            
            DetectDelimiter = true,
            DetectDelimiterValues = new[] { ",", ";" }
        };

        using var csv = new CsvReader(reader, config);

        var rows = new List<Dictionary<string, object>>();

        foreach (var record in csv.GetRecords<dynamic>())
        {
            var dict = new Dictionary<string, object>();

            foreach (var kv in (IDictionary<string, object>)record)
            {
                var raw = kv.Value?.ToString();
                var parsed = ParseCellValue(raw);

                if (parsed == null) continue;
                if (parsed is string s && string.IsNullOrWhiteSpace(s)) continue;

                dict[kv.Key.ToLowerInvariant()] = parsed;
            }

            rows.Add(dict);
        }

        return new UnifiedFile
        {
            Sheets = new List<UnifiedSheet>
            {
                new()
                {
                    Name = singleSheetName!,
                    Rows = rows
                }
            }
        };
    }

    private static StreamReader CreateReader(Stream stream)
    {
        stream.Position = 0;

        // chekc BOM
        var bom = new byte[4];
        stream.ReadExactly(bom, 0, 4);
        stream.Position = 0;

        // UTF-8 BOM
        if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
        {
            return new StreamReader(stream, Encoding.UTF8);
        }

        // UTF-16 LE BOM
        if (bom[0] == 0xFF && bom[1] == 0xFE)
        {
            return new StreamReader(stream, Encoding.Unicode);
        }

        // UTF-16 BE BOM
        if (bom[0] == 0xFE && bom[1] == 0xFF)
        {
            return new StreamReader(stream, Encoding.BigEndianUnicode);
        }

        // default to Windows-1252
        return new StreamReader(stream, Encoding.GetEncoding(1252));
    }

    private static object? ParseCellValue(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var s = raw.Trim();

        // Detect array pattern [a,b,c]
        if (s.StartsWith("\"[") && s.EndsWith("]\"") ||
            s.StartsWith("'[") && s.EndsWith("]'") ||
            s.StartsWith("[") && s.EndsWith("]"))
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

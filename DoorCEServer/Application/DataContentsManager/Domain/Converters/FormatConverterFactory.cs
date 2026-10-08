using DoorCEServer.Application.DataContentsManager.Interfaces;

namespace DoorCEServer.Application.DataContentsManager.Domain.Converters;

public class FormatConverterFactory
{
    private readonly Dictionary<string, IFormatConverter> _converters;

    public FormatConverterFactory()
    {
        _converters = new()
        {
            ["csv"] = new CsvConverter(),
            ["xlsx"] = new ExcelConverter(),
            ["xls"] = new ExcelConverter(),
            ["xml"] = new XmlConverter(),
            ["json"] = new JsonConverter()
        };
    }

    public IFormatConverter GetConverter(string extension)
    {
        if (!_converters.TryGetValue(extension.ToLower(), out var converter))
            throw new NotSupportedException($"Unsupported file type: {extension}");

        return converter;
    }
}

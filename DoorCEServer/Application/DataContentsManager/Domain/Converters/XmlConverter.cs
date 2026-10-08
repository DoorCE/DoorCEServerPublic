using System.Xml.Linq;
using DoorCEServer.Application.DataContentsManager.Common;
using DoorCEServer.Application.DataContentsManager.Interfaces;

namespace DoorCEServer.Application.DataContentsManager.Domain.Converters;

public class XmlConverter : IFormatConverter
{
    public UnifiedFile ReadFile(Stream fileStream, string? singleSheetName)
    {
        var doc = XDocument.Load(fileStream);
        var root = doc.Root ?? throw new InvalidDataException("XML has no root element.");

        var sheetElementGroups = new Dictionary<string, List<XElement>>(StringComparer.OrdinalIgnoreCase);

        var directGroups = root.Elements().GroupBy(e => e.Name.LocalName.ToLowerInvariant());
        foreach (var g in directGroups)
        {
            if (g.Count() > 1)
            {
                if (!sheetElementGroups.TryGetValue(g.Key, out var list))
                {
                    list = new List<XElement>();
                    sheetElementGroups[g.Key] = list;
                }
                list.AddRange(g);
            }
        }

        foreach (var child in root.Elements())
        {
            var grandchildGroups = child.Elements().GroupBy(e => e.Name.LocalName.ToLowerInvariant());

            foreach (var gg in grandchildGroups)
            {
                if (gg.Count() > 1)
                {
                    var sheetName = gg.Key;
                    if (!sheetElementGroups.TryGetValue(sheetName, out var list))
                    {
                        list = new List<XElement>();
                        sheetElementGroups[sheetName] = list;
                    }
                    list.AddRange(gg);
                }
            }
        }

        if (!sheetElementGroups.Any())
        {
            var repeated = root.Descendants()
                               .GroupBy(e => e.Name.LocalName.ToLowerInvariant())
                               .Where(g => g.Count() > 1);

            foreach (var g in repeated)
                sheetElementGroups[g.Key] = g.ToList();
        }

        if (!sheetElementGroups.Any())
        {
            var fallbackName = (root.Name.LocalName + "_items").ToLowerInvariant();
            sheetElementGroups[fallbackName] = root.Elements().ToList();
        }

        var sheets = sheetElementGroups.Select(kvp =>
        {
            var name = kvp.Key.ToLowerInvariant();
            var elements = kvp.Value;

            IEnumerable<Dictionary<string, object>> RowIterator()
            {
                foreach (var el in elements)
                    yield return ConvertElementToDictionary(el);
            }

            return new UnifiedSheet()
            {
                Name = singleSheetName ?? name, 
                Rows = RowIterator().ToList()
            };
        }).ToList();

        return new UnifiedFile(){Sheets = sheets};
    }

    private static Dictionary<string, object> ConvertElementToDictionary(XElement element)
    {
        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // TODO - handle null values?
        // attributes
        foreach (var attr in element.Attributes())
        {
            if (!string.IsNullOrWhiteSpace(attr.Value))
                dict["@" + attr.Name.LocalName.ToLowerInvariant()] = MDataContentsManagerCommon.ParsePrimitive(attr.Value)!;
        }

        if (!element.HasElements)
        {
            if (dict.Count > 0)
            {
                dict["#text"] = MDataContentsManagerCommon.ParsePrimitive(element.Value)!;
            }
            else
            {
                dict["value"] = MDataContentsManagerCommon.ParsePrimitive(element.Value)!;
            }
            return dict;
        }

        var childGroups = element.Elements().GroupBy(e => e.Name.LocalName.ToLowerInvariant());
        foreach (var g in childGroups)
        {
            var childName = g.Key;

            if (g.Count() == 1)
            {
                var child = g.First();
                if (!child.HasElements && !child.HasAttributes)
                {
                    if (!string.IsNullOrWhiteSpace(child.Value))
                        dict[childName] = MDataContentsManagerCommon.ParsePrimitive(child.Value)!;
                }
                else
                {
                    dict[childName] = ConvertElementToDictionary(child);
                }
            }
            else
            {
                var list = new List<object?>();

                foreach (var child in g)
                {
                    if (!child.HasElements && !child.HasAttributes)
                    {
                        list.Add(MDataContentsManagerCommon.ParsePrimitive(child.Value));
                    }
                    else
                    {
                        list.Add(ConvertElementToDictionary(child));
                    }
                }

                dict[childName] = list;
            }
        }

        return dict;
    }
}
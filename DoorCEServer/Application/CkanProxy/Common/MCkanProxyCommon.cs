using System.Text.RegularExpressions;
using DoorCEModel.Infrastructure.DataModel;
using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEModel.Infrastructure.DataModel.Datasets;

namespace DoorCEServer.Application.CkanProxy.Common;

public static partial class MCkanProxyCommon {
    public static string CreateCkanName(IdentifiableElement element) {
        string? prefix = element switch {
            Dataset => "dat/",
            Organisation => "org/",
            Distribution => "dis/",
            _ => null
        };

        return NormalizeCkanInput(element.Uri, prefix);
    }

    public static string CreateCkanName(Concept concept) {
        return concept.Name + " Data Items";
    }

    public static string CreateDatastoreAlias(string datasetUri, string conceptName) {
        string normalizedConceptName = NormalizeCkanInput(conceptName);
        string normalizedDatasetName = NormalizeCkanInput(datasetUri, "dat/");
        
        string alias = normalizedConceptName + "_" + normalizedDatasetName;
        
        if (string.IsNullOrEmpty(alias))
        {
            throw new ArgumentException("Cannot create alias for " + conceptName);
        }
        
        if (!char.IsLetter(alias[0])) // Alias has to start with a letter
            alias = "a_" + alias;
        
        if (alias.Length > 63) // PostgrSQL limit for view identifier
            alias = alias[..63].TrimEnd('-');

        return alias;
    }
    
    public static string NormalizeCkanInput(string input, string? prefix=null) {
        if (null != prefix && input.StartsWith(prefix)) input = input[prefix.Length..]; // remove the prefix
        string name = input.ToLower();
        // Replace all the whitespace strings with "-" signs
        name = WhitespaceRegex().Replace(name, "-");
        // Replace all the special characters with "_"
        name = SpecialCharRegex().Replace(name, "_");
        // Concatenate to 100 characters max
        if (100 < name.Length) name = name[..100];
        return name;
    }
    
    public static string GetPreferredTranslation(Dictionary<string,string> title)
    {
        // TODO - if possible, first determine the CKAN UI language and select such title first
        return title.TryGetValue("en", out var value) ? value : title.Values.First();
    }

    public static string GetUdasFileDistributionUrl(IConfiguration configuration, int? fileId) {
        if (fileId == null)
            throw new ArgumentNullException(nameof(fileId));
        return configuration["UDAS_EXTERNAL_URL"] + "/storage/GetFile?identifier=" + fileId;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
    [GeneratedRegex(@"[^a-zA-Z0-9\-]+")]
    private static partial Regex SpecialCharRegex();
}
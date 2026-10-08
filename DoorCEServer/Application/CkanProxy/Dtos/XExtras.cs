using System.Text.Json.Serialization;

namespace DoorCEServer.Application.CkanProxy.Dtos;

public class XExtras {
    public string? Identifier { get; init; }
    
    [JsonPropertyName("title_translated")]
    public Dictionary<string,string>? Title { get; init; }
    
    [JsonPropertyName("notes_translated")]
    public Dictionary<string, string>? Description { get; init; }
    
    [JsonPropertyName("access_rights")]
    public string? AccessRights { get; init; }
    
    public ICollection<string>? Languages { get; init; }
    
    public ICollection<string>? Themes { get; init; }
    
    [JsonPropertyName("applicable_legislation")]
    public ICollection<string>? ApplicableLegislations { get; init; }
    
    [JsonPropertyName("licence")]
    public ICollection<string>? Licences { get; init; }
    
    [JsonPropertyName("geographical_coverage")]
    public ICollection<string>? GeographicalCoverage { get; init; }

    [JsonPropertyName("spatial_uri")] 
    public ICollection<string>? SpatialUri => GeographicalCoverage;
    
    public string? Frequency { get; set; }
    
    [JsonPropertyName("temporal_coverage")]
    public ICollection<Dictionary<string, object>>? TemporalCoverage { get; init; }

    [JsonPropertyName("temporal_start")]
    public string? TemporalStart
        => TemporalCoverage?.Count > 0
            ? TemporalCoverage.Select(se => se.ElementAt(0)!.Value).Min()!.ToString()
            : null;

    [JsonPropertyName("temporal_end")]
    public string? TemporalEnd
        => TemporalCoverage?.Count > 0
            ? TemporalCoverage.Select(se => se.ElementAt(1)!.Value).Max()!.ToString()
            : null;
    
    [JsonPropertyName("release_date")]
    public DateTime? ReleaseDate { get; init; }
    
    [JsonPropertyName("modification_date")]
    public DateTime? ModificationDate { get; init; }
    
    [JsonPropertyName("dcat_type")]
    public ICollection<string>? DcatType { get; init; }
    
    public ICollection<string>? Documentation { get; init; }
    
    public ICollection<string>? Provenance { get; init; }
    
    [JsonPropertyName("version_notes")]
    public string? VersionNotes { get; init; }
    
    [JsonPropertyName("conforms_to")]
    public ICollection<string>? ConformsTo { get; init; }
    
    [JsonPropertyName("version_of")]
    public string? VersionOf { get; init; }

}
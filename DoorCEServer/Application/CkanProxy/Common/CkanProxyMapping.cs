using System.Globalization;
using AutoMapper;
using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEModel.Infrastructure.DataModel.DatasetContents;
using DoorCEModel.Infrastructure.DataModel.Datasets;
using DoorCEModel.Utils.Extensions;
using DoorCEServer.Application.CkanProxy.Dtos;

namespace DoorCEServer.Application.CkanProxy.Common;

public class DataItemToDatastoreRecord : ITypeConverter<DataItem, Dictionary<string, object?>>
{
    public Dictionary<string, object?> Convert(DataItem dataItem, Dictionary<string, object?> destination,
        ResolutionContext context) {
        
        // restore original property names
        Dictionary<string, string> propertyNames = dataItem.Concept.Properties
            .ToDictionary(p => p.Name.ToCamelCase(), p => p.Name);
        
        var record = new Dictionary<string, object?>();

        foreach (var (key, rawValue) in dataItem.Values) {
            if (!propertyNames.TryGetValue(key, out var name))
                throw new ArgumentException($"Key {key} is not a known property name");
                
            record[name] = rawValue;
        }

        // ensure PK exists without mutating EF entity
        var primaryKeyName = dataItem.Concept.GetDatastorePrimaryKeyFieldName();
        
        // add data item identifier as PK only if concept has no default id
        record.TryAdd(primaryKeyName, dataItem.Identifier); 
            
        // ensure datastore id is text to prevent type errors
        record[primaryKeyName] = record[primaryKeyName]!.ToString();

        return record;
    }
}

public class CkanProxyMapping : Profile {

    public CkanProxyMapping() {
        
        AllowNullCollections = true;

        CreateMap<Dataset, XExtras>()
            .ForMember(xce => xce.Identifier,
                opt => opt
                    .MapFrom(src => "data: " + MCkanProxyCommon.CreateCkanName(src)))
            .ForMember(xce => xce.AccessRights,
                opt => opt
                    .MapFrom(src => src.AccessRights.ToString()))
            .ForMember(xce => xce.DcatType,
                opt => opt
                    .MapFrom(src => src.Type.Select(t => t.ToString())))
            .ForMember(xce => xce.ConformsTo,
                opt => opt
                    .MapFrom(src => src.ConformsTo.Select(t => t.Title)))
            .ForMember(xce => xce.VersionOf,
                opt => opt
                    .MapFrom(src => null != src.VersionOf
                        ? MCkanProxyCommon.CreateCkanName(src.VersionOf) : null))
            .ForMember(xce => xce.TemporalCoverage,
                opt => opt
                    .MapFrom(src => src.TemporalCoverage.Select(tc 
                        => new Dictionary<string,object>{{"start", tc.start}, {"end", tc.end}})));

        CreateMap<Distribution, XCkanResource>()
            .ForMember(xcd => xcd.CkanName,
                opt => opt
                    .MapFrom(src => MCkanProxyCommon.CreateCkanName(src)))
            .ForMember(xcd => xcd.UdasType,
                opt => opt
                    .MapFrom(src => "distribution"))
            .ForMember(xcd => xcd.Created,
                opt => opt
                    .MapFrom(src =>
                        src.ReleaseDate.HasValue
                            ? src.ReleaseDate.Value.ToString("s", CultureInfo.InvariantCulture)
                            : null));

        CreateMap<Dataset, XCkanPackage>()
            .ForMember(xcp => xcp.MainTitle,
                opt => opt
                    .MapFrom(src => MCkanProxyCommon.GetPreferredTranslation(src.Title)))
            .ForMember(xcp => xcp.Notes,
                opt => opt
                    .MapFrom(src => MCkanProxyCommon.GetPreferredTranslation(src.Description)))
            .ForMember(xcp => xcp.Tags,
                opt => opt
                    .MapFrom(src => src.Keywords.Select(k => new Dictionary<string, string> {
                        { "name", MCkanProxyCommon.NormalizeCkanInput(k, null) }, { "display_name", k },
                        { "state", "active" }
                    }).ToList()))
            .ForMember(xcp => xcp.Extras,
                opt => opt
                    .MapFrom(src => src));
        
        CreateMap<Organisation, XCkanOrganisation>()
            .ForMember(xco => xco.Title,
                opt => opt
                    .MapFrom(src => src.Name));

        CreateMap<Property, XField>()
            .ForMember(xdtf => xdtf.Type,
                opt => opt
                    .MapFrom(src => src.GetDatastoreTypeName()));
        
        CreateMap<Concept, XDatastoreResource>()
            .ForMember(xdt => xdt.Fields, 
                opt => opt
                    .MapFrom(src => src.Properties))
            .ForMember(xdt => xdt.PrimaryKey,
                opt => opt
                    .MapFrom(src=> new List<string> {src.GetDatastorePrimaryKeyFieldName()}));

        CreateMap<DataItem, Dictionary<string, object?>>()
            .ConvertUsing<DataItemToDatastoreRecord>();
    }

}
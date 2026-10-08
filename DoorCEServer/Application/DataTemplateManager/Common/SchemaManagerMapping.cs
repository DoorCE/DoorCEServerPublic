using AutoMapper;
using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEModel.Infrastructure.DataModel.Applications;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEServer.Application.DataTemplateManager.Dtos;
using DoorCEServer.Infrastructure.DataModel.DataSchemas;
using Attribute = DoorCEModel.Infrastructure.DataModel.DataSchemas.Attribute;

namespace DoorCEServer.Application.DataTemplateManager.Common;

public class XConceptToConcept : ITypeConverter<ICollection<XConcept>, ICollection<Concept>>
{
    public ICollection<Concept> Convert(ICollection<XConcept> source, ICollection<Concept> destination,
        ResolutionContext context) => DoConvert(source, [], destination);

    public static ICollection<Concept> Convert(ICollection<XConcept> source, ICollection<Concept> referenceConcepts)
        => DoConvert(source, referenceConcepts);

    private static List<Concept> DoConvert(ICollection<XConcept> source, ICollection<Concept> referenceConcepts,
        ICollection<Concept>? destination = null)
    {
        Dictionary<string,Concept> concepts = new();
        foreach (var xConcept in source) {
            concepts.Add(xConcept.Name,new Concept
            {
                Name = xConcept.Name,
                Description = xConcept.Description
            });
        }

        foreach (var xConcept in source) {
            foreach (var xProperty in xConcept.Properties) {
                PrimitiveType? type = null;
                string? target = null;
                bool isMultiple = false;
                switch (xProperty.Value.Type) {
                    case "reference":
                        target = xProperty.Value.Target;
                        break;
                    case "array":
                        if (null == xProperty.Value.Items)
                            throw new ArgumentNullException(
                                $"Items for array property '{xProperty.Key}' cannot be null.");
                        isMultiple = true;
                        if ("reference" == xProperty.Value.Items.Type) {
                            target = xProperty.Value.Items.Target;
                        } else if ("array" == xProperty.Value.Items.Type) {
                            throw new NotImplementedException("Nested arrays are not supported.");
                        } else type = PrimitiveTypeExtensions.FromSchemaType(xProperty.Value.Items.Type);
                        break;
                    default:
                        type = PrimitiveTypeExtensions.FromSchemaType(xProperty.Value.Type);
                        break;
                }

                Property property;
                if (null != type) {
                    property = new Attribute {
                        Name = xProperty.Key,
                        Type = (PrimitiveType)type,
                        Unique = xConcept.Unique.Contains(xProperty.Key),
                        Concept = concepts[xConcept.Name],
                        DefaultIdentifier = xConcept.DefaultIdentifier == xProperty.Key
                    };
                } else {
                    if (null == target)
                        throw new ArgumentNullException(
                            $"Target for reference property cannot be null.");
                    Concept? typeConcept;
                    if (!concepts.ContainsKey(target)) {
                        typeConcept = referenceConcepts.SingleOrDefault(c => c.Name == target);
                        if (null == typeConcept)
                            throw new KeyNotFoundException($"Concept '{target}' not found in schema.");
                    } else typeConcept = concepts[target];

                    property = new Reference
                    {
                        Type = typeConcept,
                        Name = xProperty.Key,
                        Unique = xConcept.Unique.Contains(xProperty.Key)
                    };
                }

                property.Description = xProperty.Value.Description;
                property.Required = xConcept.Required.Contains(xProperty.Key);
                property.Multiple = isMultiple;
                concepts[xConcept.Name].Properties.Add(property);
            }
        }

        return concepts.Values.ToList();
    }
}

public class XNamespaceToNamespace : ITypeConverter<ICollection<XNamespace>, ICollection<Namespace>>
{
    public ICollection<Namespace> Convert(ICollection<XNamespace> source, ICollection<Namespace> destination,
        ResolutionContext context)
    {
        List<Namespace> namespaces = [];
        foreach (var xNamespace in source) {
            if (xNamespace.IsCustom) {
                namespaces.Add(new CustomNamespace() {
                    Iri = xNamespace.Iri ?? throw new ArgumentNullException(nameof(xNamespace.Iri),
                        "Custom namespace IRI has to be specified."),
                    Prefix = xNamespace.Prefix,
                    IsDefault = false
                });
            }
        }
        return namespaces;
    }
}

public class ConceptToXConcept : ITypeConverter<Concept, XConcept>
{
    public XConcept Convert(Concept source, XConcept destination, ResolutionContext context)
    {
        Dictionary<string,XPropertyValue> properties = new();
        foreach (var property in source.Properties)
        {
            XPropertyValue propertyValue = new();
            if (property is Reference reference) {
                propertyValue.Target = reference.Type.Name;
                propertyValue.Type = "reference";
            } else propertyValue.Type = ((Attribute)property).Type.ToSchemaType();
            
            if (property.Multiple)
            {
                XPropertyValue itemValue = propertyValue; 
                propertyValue = new XPropertyValue {
                    Type = "array",
                    Items = itemValue
                };
            }
            propertyValue.Description = property.Description;
            propertyValue.NamespacePrefix = property.Namespace?.Prefix;
            properties.Add(property.Name, propertyValue);
        }
        
        XConcept xConcept = new XConcept()
        {
            Name = source.Name,
            Description = source.Description,
            Properties = properties,
            Required = source.Properties.Where(p => p.Required).Select(p => p.Name).ToList(),
            Unique = source.Properties.Where(p => p.Unique).Select(p => p.Name).ToList(),
            NamespacePrefix = source.Namespace?.Prefix,
            Uri = source.Uri,
            DefaultIdentifier = source.DefaultIdentifier?.Name
        };
        return xConcept;
    }
}

public class SchemaManagerMapping : Profile
{
    public SchemaManagerMapping()
    {
        CreateMap<DataSchema, XDataSchema>()
            .ForMember(d => d.SeriesUri,
                opt => opt
                    .MapFrom(src => src.InSeries.Uri))
            .ForMember(d => d.SeriesTitle,
                opt => opt
                    .MapFrom(src => src.InSeries.Title))
            .ForMember(d => d.DefaultNamespacePrefix,
                opt => opt
                    .MapFrom(src => null != src.DefaultNamespace ? src.DefaultNamespace.Prefix : string.Empty))
            .ForMember(d => d.DefaultNamespaceIri,
                opt => opt
                    .MapFrom(src => null != src.DefaultNamespace ? src.DefaultNamespace.Iri : string.Empty))
            .ForMember(d => d.MainConceptName,
                opt => opt
                    .MapFrom(src => null != src.MainConcept ? src.MainConcept.Name : string.Empty))
            .ForMember(d => d.MainConceptPrefix,
                opt => opt
                    .MapFrom(src => null != src.MainConcept ? ( null != src.MainConcept.Namespace ?
                        src.MainConcept.Namespace.Prefix : (null != src.DefaultNamespace ? 
                            src.DefaultNamespace.Prefix : string.Empty)) : string.Empty));
        
        CreateMap<XDataSchema, DataSchema>();
        CreateMap<Namespace, XNamespace>()
            .ForMember(n => n.IsCustom, opt => opt
                .MapFrom(src => src is CustomNamespace));
        CreateMap<ICollection<XNamespace>, ICollection<Namespace>>()
            .ConvertUsing<XNamespaceToNamespace>();
        
        CreateMap<ICollection<XConcept>, ICollection<Concept>>()
            .ConvertUsing<XConceptToConcept>();
        CreateMap<Concept, XConcept>()
            .ConvertUsing<ConceptToXConcept>();

        CreateMap<SchemaSeries, XSchemaSeries>()
            .ForMember(s => s.CurrentSchemaUri,
                opt => opt
                    .MapFrom(src => src.Current!.Uri))
            .ForMember(s => s.CurrentSchemaTitle,
                opt => opt
                    .MapFrom(src => src.Current!.Title))
            .ForMember(xc => xc.EditorsUris,
                opt => opt
                    .MapFrom(src => src.EditorshipsLink.Editors.Select(e => e.Uri)))
            .ForMember(xc => xc.EditorsRoles,
                opt => opt
                    .MapFrom(src => new Dictionary<string, ICollection<EditorRole>>(
                        src.EditorshipsLink.EditorRoles.Where(er => 0 != er.Roles.Count).Select(er =>
                            new KeyValuePair<string, ICollection<EditorRole>>(er.Agent!.Uri, er.Roles)))));

        CreateMap<XSchemaSeries, SchemaSeries>();

        CreateMap<AcquisitionApp, XAcquisitionApp>()
            .ForMember(xd => xd.EditorsUris,
                opt => opt
                    .MapFrom(src => src.EditorshipsLink.Editors.Select(e => e.Uri)))
            .ForMember(xc => xc.EditorsRoles,
                opt => opt
                    .MapFrom(src => new Dictionary<string, ICollection<EditorRole>>(
                        src.EditorshipsLink.EditorRoles.Where(er => 0 != er.Roles.Count).Select(er =>
                            new KeyValuePair<string, ICollection<EditorRole>>(er.Agent!.Uri, er.Roles)))))
            .ForMember(a => a.TemplateUri,
            opt => opt
                .MapFrom(src => src.Template.Uri))
            .ForMember(a => a.TemplateTitle,
                opt => opt
                    .MapFrom(src => src.Template.Title))
            .ForMember(a => a.ActiveResourceUri,
                opt => opt
                    .MapFrom(src => src.ActiveResource.Uri))
            .ForMember(a => a.ActiveResourceTitle,
                opt => opt
                    .MapFrom(src => src.ActiveResource.Title))
            .ForMember(xc => xc.SourceResourceUris,
                opt => opt
                    .MapFrom(src => src.SourceResources.Select(e => e.Uri)))
            .ForMember(xc => xc.SourceResourceTitles,
                opt => opt
                    .MapFrom(src => new Dictionary<string, Dictionary<string,string>>(
                        src.SourceResources.Select(d =>
                            new KeyValuePair<string, Dictionary<string,string>>(d.Uri, d.Title)))));

        CreateMap<XAcquisitionApp, AcquisitionApp>();
        
        CreateMap<AppTemplate, XAppTemplate>()
            .ForMember(xd => xd.EditorsUris,
                opt => opt
                    .MapFrom(src => src.EditorshipsLink.Editors.Select(e => e.Uri)))
            .ForMember(xc => xc.EditorsRoles,
                opt => opt
                    .MapFrom(src => new Dictionary<string, ICollection<EditorRole>>(
                        src.EditorshipsLink.EditorRoles.Where(er => 0 != er.Roles.Count).Select(er =>
                            new KeyValuePair<string, ICollection<EditorRole>>(er.Agent!.Uri, er.Roles)))))
            .ForMember(a => a.SchemaUri,
                opt => opt
                    .MapFrom(src => src.Schema.Uri))
            .ForMember(a => a.SchemaTitle,
                opt => opt
                    .MapFrom(src => src.Schema.Title))
            .ForMember(a => a.VersionOfUri,
                opt => opt
                    .MapFrom(src => null != src.VersionOf ? src.VersionOf.Uri : null))
            .ForMember(a => a.VersionOfName,
                opt => opt
                    .MapFrom(src => null != src.VersionOf ? src.VersionOf.Title : null))
            .ForMember(a => a.AppDataSpecification,
                opt => opt
                    .MapFrom(src => src.DataSpecification.Contents))
            .ForMember(xc => xc.UseCaseScenarios,
                opt => opt
                    .MapFrom(src => new Dictionary<string, string>(
                        src.UseCases.Select(ucs =>
                            new KeyValuePair<string, string>(ucs.UseCaseName, ucs.ScenariosContents)))));
        
        CreateMap<XAppTemplate, AppTemplate>()
            .ForMember(a => a.DataSpecification,
                opt => opt
                    .MapFrom(src => new AppDataSpecification
                    {
                        Contents = src.AppDataSpecification ?? ""
                    }))
            .ForMember(a => a.UseCases,
                opt => opt
                    .MapFrom(src => src.UseCaseScenarios.Select(ucs => new UseCaseScenarios()
                    {
                        UseCaseName = ucs.Key,
                        ScenariosContents = ucs.Value
                    })))
            .ForMember(a => a.AuxiliaryConcepts,
                opt => opt
                    .Ignore());
    }
}
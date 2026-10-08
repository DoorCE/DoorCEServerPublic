using AutoMapper;
using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEModel.Infrastructure.DataModel.Datasets;
using DoorCEServer.Application.CataloguingManager.Dtos;

namespace DoorCEServer.Application.CataloguingManager.Common;

/// <summary>
/// AutoMapper profile for mapping between the CataloguingManager domain model and DTOs.
/// This is automatically registered in the Dependency Injection container.
/// </summary>
public class CataloguingManagerMapping : Profile
{
    public CataloguingManagerMapping()
    {
        CreateMap<Catalogue, XCatalogueElement>()
            .ForMember(xce => xce.ElementType,
                act => act
                    .MapFrom(src => XCatalogueElementType.Catalogue))
            .ForMember(xce => xce.Path,
                opt => opt
                    .MapFrom(src => src.GetPath()));
        CreateMap<Dataset, XCatalogueElement>()
            .ForMember(xce => xce.ElementType,
                act => act
                    .MapFrom(src => XCatalogueElementType.Dataset))
            .ForMember(xce => xce.Path,
                opt => opt
                    .MapFrom(src => src.GetPath()))
            .ForMember(xd => xd.SeriesUris,
                opt => opt
                    .MapFrom(src => src.Series.Select(s => s.Uri)))
            .ForMember(xd => xd.SeriesTitles,
                opt => opt
                    .MapFrom(src => new Dictionary<string, Dictionary<string, string>>(
                        src.Series.Select(s => new KeyValuePair<string, Dictionary<string, string>>(s.Uri, s.Title)))
                    ))
            .ForMember(xd => xd.SchemaUri,
                opt => opt
                    .MapFrom(src => src.Schema!.Uri))
            .ForMember(xd => xd.SchemaTitle,
                opt => opt
                    .MapFrom(src => src.Schema!.Title))
            .ForMember(xd => xd.Type,
                opt => opt
                    .MapFrom(src => src.Type));
        CreateMap<DatasetSeries, XCatalogueElement>()
            .ForMember(xce => xce.ElementType,
                act => act
                    .MapFrom(src => XCatalogueElementType.DatasetSeries))
            .ForMember(xce => xce.Path,
                opt => opt
                    .MapFrom(src => src.GetPath()));
        CreateMap<DataService, XCatalogueElement>()
            .ForMember(xce => xce.ElementType,
                act => act
                    .MapFrom(src => XCatalogueElementType.DataService))
            .ForMember(xce => xce.Path,
                opt => opt
                    .MapFrom(src => src.GetPath()));
        
        ValueTransformers.Add<DateTime?>(d => 
            (null == d || ((DateTime)d).Kind == DateTimeKind.Utc)? d : ((DateTime)d).ToUniversalTime());
        // TODO - check why this conversion doesn't work
        // config.ValueTransformers.Add<ValueTuple<DateTime,DateTime>>(vt => 
        //     new ValueTuple<DateTime,DateTime>((vt.Item1.Kind == DateTimeKind.Utc)? vt.Item1 : vt.Item1.ToUniversalTime(),
        //         (vt.Item2.Kind == DateTimeKind.Utc)? vt.Item2 : vt.Item2.ToUniversalTime()));

        CreateMap<Catalogue, XCatalogue>()
            .ForMember(xc => xc.ResponsiblePersonName,
                opt => opt
                    .MapFrom(src => null != src.ResponsiblePerson ? src.ResponsiblePerson.FullName : null))
            .ForMember(xc => xc.EditorsUris,
                opt => opt
                    .MapFrom(src => src.EditorshipsLink.Editors.Select(e => e.Uri)))
            .ForMember(xc => xc.ContactsUris,
                opt => opt
                    .MapFrom(src => src.Contacts.Select(c => c.Uri)))
            .ForMember(xc => xc.EditorsRoles,
                opt => opt
                    .MapFrom(src => new Dictionary<string, ICollection<EditorRole>>(
                        src.EditorshipsLink.EditorRoles.Where(er => 0 != er.Roles.Count).Select(er =>
                            new KeyValuePair<string, ICollection<EditorRole>>(er.Agent!.Uri, er.Roles)))))
            .ForMember(xc => xc.Path,
                opt => opt
                    .MapFrom(src => src.GetPath()));
        CreateMap<Dataset, XDataset>()
            .ForMember(xc => xc.ResponsiblePersonName,
                opt => opt
                    .MapFrom(src => null != src.ResponsiblePerson ? src.ResponsiblePerson.FullName : null))
            .ForMember(xd => xd.SeriesUris,
                opt => opt
                    .MapFrom(src => src.Series.Select(s => s.Uri)))
            .ForMember(xd => xd.SeriesTitles,
                opt => opt
                    .MapFrom(src => new Dictionary<string, Dictionary<string, string>>(
                        src.Series.Select(s => new KeyValuePair<string, Dictionary<string, string>>(s.Uri, s.Title)))
                    ))
            .ForMember(xd => xd.EditorsUris,
                opt => opt
                    .MapFrom(src => src.EditorshipsLink.Editors.Select(e => e.Uri)))
            .ForMember(xd => xd.ContactsUris,
                opt => opt
                    .MapFrom(src => src.Contacts.Select(c => c.Uri)))
            .ForMember(xd => xd.SchemaUri,
                opt => opt
                    .MapFrom(src => src.Schema!.Uri))
            .ForMember(xd => xd.SchemaTitle,
                opt => opt
                    .MapFrom(src => src.Schema!.Title))
            .ForMember(xc => xc.EditorsRoles,
                opt => opt
                    .MapFrom(src => new Dictionary<string, ICollection<EditorRole>>(
                        src.EditorshipsLink.EditorRoles.Where(er => 0 != er.Roles.Count).Select(er =>
                            new KeyValuePair<string, ICollection<EditorRole>>(er.Agent!.Uri, er.Roles)))))
            .ForMember(xd => xd.Path,
                opt => opt
                    .MapFrom(src => src.GetPath()))
            .ForMember(xd => xd.TargetDatasetUri,
                opt => opt
                    .MapFrom(src => null != src.Target ? src.Target.Uri : null));
            
        CreateMap<DatasetSeries, XDatasetSeries>()
            .ForMember(xc => xc.ResponsiblePersonName,
                opt => opt
                    .MapFrom(src => null != src.ResponsiblePerson ? 
                        src.ResponsiblePerson.FullName : null))
            .ForMember(xds => xds.EditorsUris,
                opt => opt
                    .MapFrom(src => src.EditorshipsLink.Editors.Select(e => e.Uri)))
            .ForMember(xds => xds.ContactsUris,
                opt => opt
                    .MapFrom(src => src.Contacts.Select(c => c.Uri)))
            .ForMember(xc => xc.EditorsRoles,
                opt => opt
                    .MapFrom(src => new Dictionary<string, ICollection<EditorRole>>(
                        src.EditorshipsLink.EditorRoles.Where(er => 0 != er.Roles.Count).Select(er =>
                            new KeyValuePair<string, ICollection<EditorRole>>(er.Agent!.Uri, er.Roles)))))
            .ForMember(xds => xds.Path,
                opt => opt
                    .MapFrom(src => src.GetPath()));
        
        CreateMap<DataService, XDataService>()
            .ForMember(xds => xds.StandardUris,
                opt => opt
                    .MapFrom(src => src.ConformsTo.Select(s => s.Uri)))
            .ForMember(xds => xds.StandardTitles,
                opt => opt
                    .MapFrom(src => src.ConformsTo.Select(s => s.Title)))
            .ForMember(xds => xds.ContactsUris,
                opt => opt
                    .MapFrom(src => src.Contacts.Select(c => c.Uri)))
            .ForMember(xc => xc.ResponsiblePersonName,
                opt => opt
                    .MapFrom(src => null != src.ResponsiblePerson ? 
                        src.ResponsiblePerson.FullName : null))
            .ForMember(xds => xds.EditorsUris,
                opt => opt
                    .MapFrom(src => src.EditorshipsLink.Editors.Select(e => e.Uri)))
            .ForMember(xc => xc.EditorsRoles,
                opt => opt
                    .MapFrom(src => new Dictionary<string, ICollection<EditorRole>>(
                        src.EditorshipsLink.EditorRoles.Where(er => 0 != er.Roles.Count).Select(er =>
                            new KeyValuePair<string, ICollection<EditorRole>>(er.Agent!.Uri, er.Roles)))))
            .ForMember(xds => xds.Path,
                opt => opt
                    .MapFrom(src => src.GetPath()));
        
        CreateMap<XDataService, DataService>();
        CreateMap<XCatalogue, Catalogue>();
        CreateMap<XDataset, Dataset>();
        CreateMap<XDatasetSeries, DatasetSeries>();
        
        CreateMap<Distribution, XDistribution>()
            .ForMember(xds => xds.DatasetUri,
                opt => opt
                    .MapFrom(src => src.Dataset.Uri))
            .ForMember(xds => xds.DatasetTitle,
                opt => opt
                    .MapFrom(src => src.Dataset.Title))
            .ForMember(xds => xds.DataServiceUri,
                opt => opt
                    .MapFrom(src => src.DataService!.Uri))
            .ForMember(xds => xds.DataServiceTitle,
                opt => opt
                    .MapFrom(src => src.DataService!.Title))
            .ForMember(xds => xds.Status,
                opt => opt
                    .MapFrom(src => MaturityStatusHelper.ToDescriptionString(src.Status)))
            .ForMember(xds => xds.SchemaUri,
                opt => opt
                    .MapFrom(src => src.Schema!.Uri))
            .ForMember(xds => xds.SchemaTitle,
                opt => opt
                    .MapFrom(src => src.Schema!.Title));
        
        // CreateMap<XDistribution, FileDistribution>()
        //    .ForMember(xd => xd.Status, opt => opt
        //        .MapFrom(src => MaturityStatusHelper.FromDescriptionString(src.Status)));
        // TODO - check if this is needed
        CreateMap<XDistribution, Distribution>()
            .ForMember(xd => xd.Status, opt => opt
            .MapFrom(src => MaturityStatusHelper.FromDescriptionString(src.Status)));
        CreateMap<XContactData, ContactData>();
        CreateMap<ContactData, XContactData>()
            .ForMember(xc => xc.AgentName,
                opt => opt
                    .MapFrom(src => null != src.Agent ? 
                        src.Agent.FullName : null));
        CreateMap<XOrganisation, Organisation>();
        CreateMap<Organisation, XOrganisation>()
            .ForMember(xo => xo.ContactsUris,
                opt => opt
                    .MapFrom(src => src.Contacts.Select(c => c.Uri)))
            .ForMember(xo => xo.MembersUris,
                opt => opt
                    .MapFrom(src => src.Members.Select(p => p.Uri)))
            .ForMember(xo => xo.MembersRoles,
                opt => opt
                    .MapFrom(src => src.MemberRoles.Where(m => 0!= m.Roles.Count).Select(m => 
                        new KeyValuePair<string, ICollection<ManagerRole>>(m.Person.Uri, m.Roles))));
        CreateMap<XPerson, Person>();
        CreateMap<Person, XPerson>()
            .ForMember(xp => xp.ContactsUris,
                opt => opt
                    .MapFrom(src => src.Contacts.Select(c => c.Uri)))
            .ForMember(xp => xp.OrganisationsUris,
                opt => opt
                    .MapFrom(src => src.Organisations.Select(o => o.Uri)))
            .ForMember(xp => xp.OrganisationsNames,
                opt => opt
                    .MapFrom(src => new Dictionary<string, string>(
                        src.Organisations.Select(o => 
                            new KeyValuePair<string, string>(o.Uri, o.Name)))))
            .ForMember(xp => xp.RolesInOrganisations,
                opt => opt
                    .MapFrom(src => new Dictionary<string, ICollection<ManagerRole>>(
                        src.MemberRoles.Where(m => 0 != m.Roles.Count).Select(m => 
                            new KeyValuePair<string, ICollection<ManagerRole>>(m.Organisation.Uri, m.Roles)))))
            .ForMember(xp => xp.UserId,
                opt => opt
                    .MapFrom(src => null != src.Account ? src.Account.UserId : null));
        CreateMap<XUserAccount, UserAccount>();
        CreateMap<UserAccount, XUserAccount>()
            .ForMember(xua => xua.PersonUri,
                opt => opt
                    .MapFrom(src => null != src.Person ? src.Person.Uri : null))
            .ForMember(xua => xua.PersonFullName,
                opt => opt
                    .MapFrom(src => null != src.Person ? src.Person.FullName : null));
    }
}
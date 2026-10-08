using AutoMapper;
using DoorCEModel.Infrastructure.DataModel.BinaryContents;
using DoorCEServer.Application.StorageManager.Dtos;

namespace DoorCEServer.Application.StorageManager.Common;

/// <summary>
/// AutoMapper profile for mapping between the StorageManager domain model and DTOs.
/// This is automatically registered in the Dependency Injection container.
/// </summary>
public class StorageManagerMapping : Profile
{
    public StorageManagerMapping()
    {
        CreateMap<Icon, XIconData>()
            .ForMember(id => id.IconUri,
                opt => opt
                    .MapFrom(src => src.Uri))
            .ForMember(id => id.IconName,
                opt => opt
                    .MapFrom(src => src.Name));

        CreateMap<XIconData, Icon>();

    }
}
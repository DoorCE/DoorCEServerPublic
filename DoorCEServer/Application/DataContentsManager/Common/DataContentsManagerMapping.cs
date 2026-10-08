using AutoMapper;
using DoorCEModel.Infrastructure.DataModel.DatasetContents;
using DoorCEServer.Application.DataContentsManager.Dtos;

namespace DoorCEServer.Application.DataContentsManager.Common;

/// <summary>
/// AutoMapper profile for mapping between the DataContentsManager domain model and DTOs.
/// This is automatically registered in the Dependency Injection container.
/// </summary>
public class DataContentsManagerMapping : Profile
{
    public DataContentsManagerMapping()
    {
        CreateMap<DataItem, XDataItem>();
    }
}
using AutoMapper;
using DoorCEServer.Application.VocabularyManager.Dtos;

namespace DoorCEServer.Application.VocabularyManager.Domain;

public class VocabularyManagerMapping : Profile
{
    public VocabularyManagerMapping()
    {
        CreateMap<Language, XLanguage>();
        CreateMap<Format, XFormat>();
    }
}
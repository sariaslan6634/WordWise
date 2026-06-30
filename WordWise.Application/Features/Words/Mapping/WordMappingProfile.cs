using AutoMapper;
using System.Text.Json;
using WordWise.Application.Features.Words.Dtos;
using WordWise.Domain.Entities;

namespace WordWise.Application.Features.Words.Mapping
{
    public class WordMappingProfile : Profile
    {
        public WordMappingProfile()
        {
            CreateMap<Word, WordDetailDto>()
                .ForMember(dest => dest.CefrLevel,
                    opt => opt.MapFrom(src => src.CefrLevel != null ? src.CefrLevel.ToString() : null))
                .ForMember(dest => dest.ExampleSentences,
                    opt => opt.MapFrom(src => DeserializeJsonList(src.ExampleSentencesJson)))
                .ForMember(dest => dest.Synonyms,
                    opt => opt.MapFrom(src => DeserializeJsonList(src.SynonymsJson)))
                .ForMember(dest => dest.Antonyms,
                    opt => opt.MapFrom(src => DeserializeJsonList(src.AntonymsJson)))
                .ForMember(dest => dest.Videos,
                    opt => opt.MapFrom(src => src.Videos));

            CreateMap<Video, WordVideoDto>();
        }
        private static List<string> DeserializeJsonList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch (JsonException)
            {
                return new List<string>();
            }
        }
    }
}

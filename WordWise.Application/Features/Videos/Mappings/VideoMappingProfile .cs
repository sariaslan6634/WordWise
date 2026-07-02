using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordWise.Application.Features.Videos.Dtos;
using WordWise.Domain.Entities;

namespace WordWise.Application.Features.Videos.Mappings
{
    public class VideoMappingProfile : Profile
    {
        public VideoMappingProfile()
        {
            CreateMap<VideoCandidate, VideoCandidateDto>()
               .ForMember(dest => dest.WordText,
                   opt => opt.MapFrom(src => src.Word != null ? src.Word.Text : string.Empty));
        }
    }
}

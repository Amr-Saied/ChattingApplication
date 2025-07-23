using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ChattingApplicationProject.DTO;
using ChattingApplicationProject.Models;
using ChattingApplicationProject.Services;

namespace ChattingApplicationProject.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<AppUser, MemeberDTO>()
                .ForMember(
                    dest => dest.PhotoUrl,
                    opt => opt.MapFrom(src => src.Photos.FirstOrDefault(x => x.IsMain).Url)
                )
                .ForMember(
                    dest => dest.Age,
                    opt => opt.MapFrom(src => new GetAgeService().CalculateAge(src.DateOfBirth))
                );

            CreateMap<Photo, PhotoDTO>();
        }
    }
}

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
                    dest => dest.age,
                    opt => opt.MapFrom(src => new GetAgeService().CalculateAge(src.DateOfBirth))
                );

            CreateMap<Photo, PhotoDTO>();
            CreateMap<PhotoDTO, Photo>();
            CreateMap<MemeberDTO, AppUser>();

            // Add mapping from RegisterDTO to AppUser
            CreateMap<RegisterDTO, AppUser>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Username.ToLower()))
                .ForMember(dest => dest.Created, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.LastActive, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => "User"));
        }
    }
}

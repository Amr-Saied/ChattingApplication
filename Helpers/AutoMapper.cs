using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ChattingApplicationProject.DTO;
using ChattingApplicationProject.Models;

namespace ChattingApplicationProject.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<AppUser, MemeberDTO>()
                .ForMember(
                    dest => dest.PhotoUrl,
                    opt => opt.MapFrom(src => GetMainPhotoUrl(src.Photos))
                )
                .ForMember(dest => dest.age, opt => opt.MapFrom(src => src.GetAge()));

            CreateMap<Photo, PhotoDTO>();
            CreateMap<PhotoDTO, Photo>();
            CreateMap<MemeberDTO, AppUser>();

            // Add mapping from RegisterDTO to AppUser
            CreateMap<RegisterDTO, AppUser>()
                .ForMember(
                    dest => dest.UserName,
                    opt => opt.MapFrom(src => (src.Username ?? string.Empty).ToLower())
                )
                .ForMember(dest => dest.Created, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.LastActive, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => "User"));

            CreateMap<Message, MessageDto>();
            CreateMap<MessageDto, Message>();
        }

        private static string GetMainPhotoUrl(ICollection<Photo>? photos)
        {
            if (photos == null || !photos.Any())
                return string.Empty;

            var mainPhoto = photos.FirstOrDefault(x => x.IsMain);
            return mainPhoto?.Url ?? string.Empty;
        }
    }
}

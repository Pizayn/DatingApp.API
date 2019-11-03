using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Dtos;
using DatingApp.API.Models;

namespace DatingApp.API.Helpers
{
    public class AutoMapperProfiles:Profile
    {
        public AutoMapperProfiles()
        {
            //source first,after destination
            CreateMap<User, UserForListDto>()
                .ForMember(dest => dest.PhotoUrl,
                    opt => { opt.MapFrom(src => src.Photos.FirstOrDefault(p => p.IsMain).Url); })
                .ForMember(dest => dest.Age, opt =>
                {
                    opt.MapFrom(d=>d.DateOfBirth.CalculateAge());
                });

            CreateMap<User, UserForDetailedDto>().ForMember(dest => dest.PhotoUrl,
                opt => { opt.MapFrom(src => src.Photos.FirstOrDefault(p => p.IsMain).Url); }).ForMember(dest => dest.Age, opt =>
            {
                opt.MapFrom(d => d.DateOfBirth.CalculateAge());
            });
            CreateMap<Photo, PhotosForDetailedDto>();
            CreateMap<UserForUpdateDto, User>();
            CreateMap<Photo, PhotosForReturnDto>();
            CreateMap<PhotoForCreationDto, Photo>();
            CreateMap<UserForRegisterDto, User>();
            CreateMap<MessageForCreationDto, Message>().ReverseMap();
            CreateMap<Message, MessageToReturnDto>()
                .ForMember(dest => dest.SenderPhotoUrl, opt => opt.MapFrom(u => u.Sender.Photos.FirstOrDefault(p => p.IsMain).Url))
                .ForMember(dest => dest.RecipientPhotoUrl, opt => opt.MapFrom(u => u.Recipient.Photos.FirstOrDefault(p => p.IsMain).Url));

        }
    }
}

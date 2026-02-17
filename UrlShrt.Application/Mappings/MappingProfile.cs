using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShrt.Application.DTOs.Admin;
using UrlShrt.Application.DTOs.ApiKey;
using UrlShrt.Application.DTOs.Auth;
using UrlShrt.Application.DTOs.Url;
using UrlShrt.Domain.Entities;

namespace UrlShrt.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User mappings
            CreateMap<ApplicationUser, UserDto>()
                .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"))
                .ForMember(d => d.Roles, o => o.Ignore()); // Roles set manually

            CreateMap<ApplicationUser, AdminUserDto>()
                .ForMember(d => d.Roles, o => o.Ignore())
                .ForMember(d => d.TotalUrls, o => o.Ignore())
                .ForMember(d => d.TotalClicks, o => o.Ignore());

            // ShortenedUrl mappings
            CreateMap<ShortenedUrl, UrlResponseDto>()
                .ForMember(d => d.Tags, o => o.MapFrom(s =>
                    s.Tags != null
                        ? JsonSerializer.Deserialize<List<string>>(s.Tags)
                        : null));

            CreateMap<CreateUrlDto, ShortenedUrl>()
                .ForMember(d => d.Tags, o => o.MapFrom(s =>
                    s.Tags != null ? JsonSerializer.Serialize(s.Tags) : null))
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.ShortCode, o => o.Ignore())
                .ForMember(d => d.ShortUrl, o => o.Ignore())
                .ForMember(d => d.TotalClicks, o => o.Ignore())
                .ForMember(d => d.UniqueClicks, o => o.Ignore())
                .ForMember(d => d.IsPasswordProtected, o => o.MapFrom(s => s.Password != null))
                .ForMember(d => d.Password, o => o.Ignore()) // Hashed separately
                .ForMember(d => d.User, o => o.Ignore())
                .ForMember(d => d.Clicks, o => o.Ignore())
                .ForMember(d => d.Analytics, o => o.Ignore());

            // ApiKey mappings
            CreateMap<ApiKey, ApiKeyDto>()
                .ForMember(d => d.Key, o => o.MapFrom(s => MaskApiKey(s.Key)));
        }

        private static string MaskApiKey(string key)
        {
            if (key.Length <= 10) return "****";
            return key[..6] + "..." + key[^4..];
        }
    }
}

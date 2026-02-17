using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrlShrt.Application.DTOs.Url
{
    public record CreateUrlDto(
      string OriginalUrl,
      string? CustomAlias = null,
      string? Title = null,
      string? Description = null,
      DateTime? ExpiresAt = null,
      string? Password = null,
      int ClickLimit = 0,
      List<string>? Tags = null,
      string? UtmSource = null,
      string? UtmMedium = null,
      string? UtmCampaign = null
  );

    public record UpdateUrlDto(
        string? Title = null,
        string? Description = null,
        DateTime? ExpiresAt = null,
        bool? IsActive = null,
        List<string>? Tags = null,
        string? UtmSource = null,
        string? UtmMedium = null,
        string? UtmCampaign = null
    );

    public record BulkCreateUrlDto(List<CreateUrlDto> Urls);

    public record VerifyUrlPasswordDto(string Password);
}

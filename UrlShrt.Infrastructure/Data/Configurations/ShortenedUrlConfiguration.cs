using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShrt.Domain.Entities;

namespace UrlShrt.Infrastructure.Data.Configurations
{
    public class ShortenedUrlConfiguration : IEntityTypeConfiguration<ShortenedUrl>
    {
        public void Configure(EntityTypeBuilder<ShortenedUrl> builder)
        {
            builder.ToTable("ShortenedUrls");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.OriginalUrl).IsRequired().HasMaxLength(2048);
            builder.Property(x => x.ShortCode).IsRequired().HasMaxLength(20);
            builder.Property(x => x.ShortUrl).IsRequired().HasMaxLength(2100);
            builder.Property(x => x.CustomAlias).HasMaxLength(50);
            builder.Property(x => x.Title).HasMaxLength(255);
            builder.Property(x => x.Description).HasMaxLength(1000);
            builder.Property(x => x.Password).HasMaxLength(512);
            builder.Property(x => x.Tags).HasMaxLength(2000);

            builder.HasIndex(x => x.ShortCode).IsUnique();
            builder.HasIndex(x => x.CustomAlias).IsUnique().HasFilter("[CustomAlias] IS NOT NULL");
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.CreatedAt);
            builder.HasIndex(x => x.ExpiresAt);

            builder.HasOne(x => x.User)
                .WithMany(x => x.ShortenedUrls)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.Clicks)
                .WithOne(x => x.ShortenedUrl)
                .HasForeignKey(x => x.ShortenedUrlId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class UrlClickConfiguration : IEntityTypeConfiguration<UrlClick>
    {
        public void Configure(EntityTypeBuilder<UrlClick> builder)
        {
            builder.ToTable("UrlClicks");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.IpAddress).HasMaxLength(50);
            builder.Property(x => x.UserAgent).HasMaxLength(512);
            builder.Property(x => x.Referer).HasMaxLength(2048);
            builder.Property(x => x.Country).HasMaxLength(100);
            builder.Property(x => x.City).HasMaxLength(100);
            builder.Property(x => x.DeviceType).HasMaxLength(20);
            builder.Property(x => x.Browser).HasMaxLength(100);
            builder.Property(x => x.OperatingSystem).HasMaxLength(100);

            builder.HasIndex(x => x.ShortenedUrlId);
            builder.HasIndex(x => x.ClickedAt);
            builder.HasIndex(x => new { x.ShortenedUrlId, x.IpAddress });
        }
    }

    public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
    {
        public void Configure(EntityTypeBuilder<ApiKey> builder)
        {
            builder.ToTable("ApiKeys");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Key).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
            builder.Property(x => x.UserId).IsRequired();

            builder.HasIndex(x => x.Key).IsUnique();
            builder.HasIndex(x => x.UserId);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

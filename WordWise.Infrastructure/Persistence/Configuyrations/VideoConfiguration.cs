using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordWise.Domain.Entities;
using WordWise.Infrastructure.Persistence.Configurations;

namespace WordWise.Infrastructure.Persistence.Configuyrations
{
    public class VideoConfiguration : IEntityTypeConfiguration<Video>
    {
        public void Configure(EntityTypeBuilder<Video> builder)
        {
            BaseEntityConfiguration.Configure(builder);

            builder.ToTable("Videos");

            builder.Property(x => x.YoutubeId)
                .IsRequired()
                .HasMaxLength(20); // YouTube ID'leri 11 karakter, tolerans için 20

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.ChannelTitle)
                .HasMaxLength(200)
                .IsRequired(false);

            builder.Property(x => x.ThumbnailUrl)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(x => x.StartSec)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.EndSec)
                .IsRequired()
                .HasDefaultValue(60);

            builder.Property(x => x.DisplayOrder)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.Transcript)
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);

            builder.Property(x => x.IsPublished)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.SourceCandidateId)
                .IsRequired(false);

            builder.HasIndex(x => new { x.WordId, x.YoutubeId })
                .IsUnique()
                .HasDatabaseName("IX_Videos_WordId_YoutubeId");

            builder.HasIndex(x => new { x.WordId, x.DisplayOrder })
                .HasDatabaseName("IX_Videos_WordId_DisplayOrder");

            builder.HasOne(x => x.Word)
                .WithMany(x => x.Videos)
                .HasForeignKey(x => x.WordId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.SourceCandidate)
                .WithMany()
                .HasForeignKey(x => x.SourceCandidateId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.QuizQuestions)
                .WithOne(x => x.Video)
                .HasForeignKey(x => x.VideoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

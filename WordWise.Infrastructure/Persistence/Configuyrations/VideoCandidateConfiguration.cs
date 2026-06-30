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
    public class VideoCandidateConfiguration : IEntityTypeConfiguration<VideoCandidate>
    {
        public void Configure(EntityTypeBuilder<VideoCandidate> builder)
        {
            BaseEntityConfiguration.Configure(builder);

            builder.ToTable("VideoCandidates");

            builder.Property(x => x.YoutubeId)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.ChannelTitle)
                .HasMaxLength(200)
                .IsRequired(false);

            builder.Property(x => x.ThumbnailUrl)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(x => x.Description)
                .HasMaxLength(2000)
                .IsRequired(false);

            builder.Property(x => x.DurationSeconds)
                .IsRequired(false);

            builder.Property(x => x.FetchedAt)
                .IsRequired();

            builder.Property(x => x.IsApproved)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.IsRejected)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.ApprovedByUserId)
                .IsRequired(false);

            builder.Property(x => x.ApprovedAt)
                .IsRequired(false);

            builder.HasIndex(x => new { x.WordId, x.YoutubeId })
                .IsUnique()
                .HasDatabaseName("IX_VideoCandidates_WordId_YoutubeId");

            builder.HasIndex(x => new { x.IsApproved, x.IsRejected })
                .HasDatabaseName("IX_VideoCandidates_ApprovalStatus");

            builder.HasOne(x => x.Word)
                .WithMany(x => x.VideoCandidates)
                .HasForeignKey(x => x.WordId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}

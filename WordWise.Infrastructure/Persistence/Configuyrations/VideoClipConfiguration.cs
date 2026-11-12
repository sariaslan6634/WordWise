using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordWise.Domain.Entities;
using WordWise.Infrastructure.Persistence.Configurations;

namespace WordWise.Infrastructure.Persistence.Configuyrations
{
    public class VideoClipConfiguration : IEntityTypeConfiguration<VideoClip>
    {
        public void Configure(EntityTypeBuilder<VideoClip> builder)
        {
            BaseEntityConfiguration.Configure(builder);

            builder.ToTable("VideoClips");

            builder.Property(x => x.YoutubeId).IsRequired().HasMaxLength(20);
            builder.Property(x => x.Text).IsRequired().HasMaxLength(1000);
            builder.Property(x => x.WordTimingsJson).IsRequired().HasColumnType("nvarchar(max)");
            builder.Property(x => x.QcReasons).HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(x => x.QcWarnings).HasColumnType("nvarchar(max)").IsRequired(false);

            builder.Property(x => x.SourceTitle).HasMaxLength(500).IsRequired(false);
            builder.Property(x => x.SourceChannel).HasMaxLength(200).IsRequired(false);
            builder.Property(x => x.SourceDescription).HasMaxLength(1000).IsRequired(false);

            builder.Property(x => x.IsPublished).IsRequired().HasDefaultValue(false);
            builder.Property(x => x.IsRejected).IsRequired().HasDefaultValue(false);
            builder.Property(x => x.HasBurnedInCaptions).IsRequired().HasDefaultValue(false);

            builder.HasIndex(x => x.YoutubeId);

            builder.HasOne(x => x.VideoCandidate)
                .WithMany()
                .HasForeignKey(x => x.VideoCandidateId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.VideoCandidateId);
        }
    }
}

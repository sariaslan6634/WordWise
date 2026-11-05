using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordWise.Domain.Entities;
using WordWise.Infrastructure.Persistence.Configurations;

namespace WordWise.Infrastructure.Persistence.Configuyrations
{
    public class DiscoveredVideoConfiguration : IEntityTypeConfiguration<DiscoveredVideo>
    {
        public void Configure(EntityTypeBuilder<DiscoveredVideo> builder)
        {
            BaseEntityConfiguration.Configure(builder);

            builder.ToTable("DiscoveredVideos");

            builder.Property(x => x.YoutubeId).IsRequired().HasMaxLength(20);
            builder.Property(x => x.Title).IsRequired().HasMaxLength(500);
            builder.Property(x => x.SearchQuery).IsRequired().HasMaxLength(300);
            builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
            builder.Property(x => x.Error).HasMaxLength(1000).IsRequired(false);

            builder.HasIndex(x => x.YoutubeId).IsUnique();
        }
    }
}

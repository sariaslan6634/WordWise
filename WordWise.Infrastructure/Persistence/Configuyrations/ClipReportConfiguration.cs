using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordWise.Domain.Entities;
using WordWise.Infrastructure.Persistence.Configurations;

namespace WordWise.Infrastructure.Persistence.Configuyrations
{
    public class ClipReportConfiguration : IEntityTypeConfiguration<ClipReport>
    {
        public void Configure(EntityTypeBuilder<ClipReport> builder)
        {
            BaseEntityConfiguration.Configure(builder);

            builder.ToTable("ClipReports");

            builder.Property(x => x.Note).HasMaxLength(500).IsRequired(false);
            builder.Property(x => x.Status).IsRequired().HasMaxLength(20);

            builder.HasOne(x => x.Clip)
                .WithMany()
                .HasForeignKey(x => x.ClipId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.ClipId, x.UserId }).IsUnique();
            builder.HasIndex(x => x.UserId);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordWise.Domain.Entities;
using WordWise.Infrastructure.Persistence.Configurations;

namespace WordWise.Infrastructure.Persistence.Configuyrations
{
    public class UserClipConfiguration : IEntityTypeConfiguration<UserClip>
    {
        public void Configure(EntityTypeBuilder<UserClip> builder)
        {
            BaseEntityConfiguration.Configure(builder);

            builder.ToTable("UserClips");

            builder.HasOne(x => x.Clip)
                .WithMany()
                .HasForeignKey(x => x.ClipId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.UserId, x.ClipId }).IsUnique();
        }
    }
}

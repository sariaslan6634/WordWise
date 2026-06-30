using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordWise.Domain.Entities;

namespace WordWise.Infrastructure.Persistence.Configurations
{
    public static class BaseEntityConfiguration
    {
        public static void Configure<T>(EntityTypeBuilder<T> builder) where T : BaseEntity
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever(); 

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired(false);

            builder.Property(x => x.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);
        }
    }
}

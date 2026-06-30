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
    public class UserWordConfiguration : IEntityTypeConfiguration<UserWord>
    {
        public void Configure(EntityTypeBuilder<UserWord> builder)
        {
            BaseEntityConfiguration.Configure(builder);

            builder.ToTable("UserWords");

            builder.HasIndex(x => new { x.UserId, x.WordId })
                .IsUnique()
                .HasDatabaseName("IX_UserWords_UserId_WordId");

            builder.Property(x => x.KnownLevel)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.NextReviewAt)
                .IsRequired(false);

            builder.Property(x => x.ReviewCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.PersonalNote)
                .HasMaxLength(1000)
                .IsRequired(false);

            // Review sorgularında sık kullanılacak
            builder.HasIndex(x => new { x.UserId, x.NextReviewAt })
                .HasDatabaseName("IX_UserWords_UserId_NextReviewAt");
        }
    }
}

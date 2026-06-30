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
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {

            BaseEntityConfiguration.Configure(builder);

            builder.ToTable("Users");

            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(256);

            builder.HasIndex(x => x.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.PasswordHash)
                .IsRequired()
                .HasMaxLength(512);

            builder.Property(x => x.Role)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.IsEmailConfirmed)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.EmailConfirmationTokenHash)
                .HasMaxLength(512)
                .IsRequired(false);

            builder.Property(x => x.EmailConfirmationTokenExpiresAt)
                .IsRequired(false);

            builder.Property(x => x.EmailConfirmedAt)
                .IsRequired(false);

            // Relations
            builder.HasMany(x => x.UserWords)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.XpHistory)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.QuizAnswers)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.ApprovedCandidates)
                .WithOne(x => x.ApprovedByUser)
                .HasForeignKey(x => x.ApprovedByUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}

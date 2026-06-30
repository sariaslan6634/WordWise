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
    public class UserXpHistoryConfiguration : IEntityTypeConfiguration<UserXpHistory>
    {
        public void Configure(EntityTypeBuilder<UserXpHistory> builder)
        {
            BaseEntityConfiguration.Configure(builder);

            builder.ToTable("UserXpHistory");

            builder.Property(x => x.XpChange)
                .IsRequired();

            builder.Property(x => x.TotalXpAfterChange)
                .IsRequired();

            builder.Property(x => x.Reason)
                .IsRequired()
                .HasConversion<int>();

            builder.HasIndex(x => new { x.UserId, x.CreatedAt })
                .HasDatabaseName("IX_UserXpHistory_UserId_CreatedAt");
        }
    }
}

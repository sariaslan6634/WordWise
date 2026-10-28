using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordWise.Domain.Entities;
using WordWise.Infrastructure.Persistence.Configurations;

namespace WordWise.Infrastructure.Persistence.Configuyrations
{
    public class UserLoginEventConfiguration : IEntityTypeConfiguration<UserLoginEvent>
    {
        public void Configure(EntityTypeBuilder<UserLoginEvent> builder)
        {
            BaseEntityConfiguration.Configure(builder);
            builder.ToTable("UserLoginEvents");
            builder.Property(x => x.UserId).IsRequired();
            builder.Property(x => x.LoggedInAt).IsRequired();
            builder.HasIndex(x => x.LoggedInAt);
            builder.HasIndex(x => new { x.UserId, x.LoggedInAt });
            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

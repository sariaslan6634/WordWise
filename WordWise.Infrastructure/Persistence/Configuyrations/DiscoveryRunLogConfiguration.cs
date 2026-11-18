using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordWise.Domain.Entities;
using WordWise.Infrastructure.Persistence.Configurations;

namespace WordWise.Infrastructure.Persistence.Configuyrations
{
    public class DiscoveryRunLogConfiguration : IEntityTypeConfiguration<DiscoveryRunLog>
    {
        public void Configure(EntityTypeBuilder<DiscoveryRunLog> builder)
        {
            BaseEntityConfiguration.Configure(builder);
            builder.ToTable("DiscoveryRunLogs");
            builder.HasIndex(x => x.CreatedAt);
        }
    }
}

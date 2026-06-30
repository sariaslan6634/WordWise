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
    public class WordConfiguration : IEntityTypeConfiguration<Word>
    {
        public void Configure(EntityTypeBuilder<Word> builder)
        {
            BaseEntityConfiguration.Configure(builder);

            builder.ToTable("Words");

            builder.Property(x => x.Text)
                .IsRequired()
                .HasMaxLength(100);

            // Arama her zaman lowercase ile yapılacak, unique index de lowercase
            builder.HasIndex(x => x.Text)
                .IsUnique()
                .HasDatabaseName("IX_Words_Text");

            builder.Property(x => x.Definition)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(x => x.Ipa)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(x => x.PartOfSpeech)
                .HasMaxLength(50)
                .IsRequired(false);

            builder.Property(x => x.CefrLevel)
                .HasConversion<int>()
                .IsRequired(false);

            builder.Property(x => x.ExampleSentencesJson)
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);

            builder.Property(x => x.SynonymsJson)
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);

            builder.Property(x => x.AntonymsJson)
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);

            builder.Property(x => x.Category)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(x => x.SourceProvider)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(x => x.SourceUrl)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(x => x.ImportedAt)
                .IsRequired(false);

            builder.Property(x => x.IsPublished)
                .IsRequired()
                .HasDefaultValue(false);

            builder.HasMany(x => x.Videos)
                .WithOne(x => x.Word)
                .HasForeignKey(x => x.WordId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.VideoCandidates)
                .WithOne(x => x.Word)
                .HasForeignKey(x => x.WordId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.UserWords)
                .WithOne(x => x.Word)
                .HasForeignKey(x => x.WordId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

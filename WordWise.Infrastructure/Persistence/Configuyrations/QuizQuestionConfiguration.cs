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
    public class QuizQuestionConfiguration : IEntityTypeConfiguration<QuizQuestion>
    {
        public void Configure(EntityTypeBuilder<QuizQuestion> builder)
        {
            BaseEntityConfiguration.Configure(builder);

            builder.ToTable("QuizQuestions");

            builder.Property(x => x.QuestionText)
                .IsRequired()
                .HasMaxLength(1000);

            // ["Option A", "Option B", "Option C", "Option D"] JSON array
            builder.Property(x => x.OptionsJson)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            builder.Property(x => x.CorrectAnswer)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.IsFreeText)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.IsPublished)
                .IsRequired()
                .HasDefaultValue(true);

            builder.HasIndex(x => x.VideoId)
                .HasDatabaseName("IX_QuizQuestions_VideoId");

            // Relations
            builder.HasOne(x => x.Video)
                .WithMany(x => x.QuizQuestions)
                .HasForeignKey(x => x.VideoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.UserAnswers)
                .WithOne(x => x.QuizQuestion)
                .HasForeignKey(x => x.QuizQuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

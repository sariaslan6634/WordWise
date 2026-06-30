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
    public class UserQuizAnswerConfiguration : IEntityTypeConfiguration<UserQuizAnswer>
    {
        public void Configure(EntityTypeBuilder<UserQuizAnswer> builder)
        {
            BaseEntityConfiguration.Configure(builder);

            builder.ToTable("UserQuizAnswers");

            builder.Property(x => x.GivenAnswer)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.IsCorrect)
                .IsRequired();

            builder.Property(x => x.XpChange)
                .IsRequired();

            builder.Property(x => x.AnsweredAt)
                .IsRequired();

            // 24 saatlik XP cooldown kontrolü için kritik index:
            // "Bu kullanıcı bu soruyu bugün cevapladı mı?" sorgusu buraya düşer
            builder.HasIndex(x => new { x.UserId, x.QuizQuestionId, x.AnsweredAt })
                .HasDatabaseName("IX_UserQuizAnswers_UserId_QuestionId_AnsweredAt");
        }
    }
}

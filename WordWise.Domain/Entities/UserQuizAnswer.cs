using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Domain.Entities
{
    public class UserQuizAnswer: BaseEntity
    {
        public Guid UserId { get; set; }

        public User User { get; set; } = null!;

        public Guid QuizQuestionId { get; set; }

        public QuizQuestion QuizQuestion { get; set; } = null!;

        public string GivenAnswer { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }

        public int XpChange { get; set; }

        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
    }
}

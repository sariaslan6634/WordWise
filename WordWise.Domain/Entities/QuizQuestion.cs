using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Domain.Entities
{
    public class QuizQuestion : BaseEntity
    {
        public Guid VideoId { get; set; }

        public Video Video { get; set; } = null!;

        public string QuestionText { get; set; } = string.Empty;

        public string OptionsJson { get; set; } = string.Empty;

        public string CorrectAnswer { get; set; } = string.Empty;

        public bool IsFreeText { get; set; } = false;

        public bool IsPublished { get; set; } = true;

        public ICollection<UserQuizAnswer> UserAnswers { get; set; } = new List<UserQuizAnswer>();
    }
}

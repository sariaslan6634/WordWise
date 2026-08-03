using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Features.Quizzes.Dtos
{
    public class QuizQuestionDto
    {
        public Guid Id { get; set; }
        public Guid VideoId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public bool IsFreeText { get; set; }
    }
}

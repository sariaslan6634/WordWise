using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Features.Quizzes.Dtos
{
    public class CreateQuizQuestionDto
    {
        public Guid VideoId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public string CorrectAnswer { get; set; } = string.Empty;
        public bool IsFreeText { get; set; } = false;
        public bool IsPublished { get; set; } = true;
    }
}

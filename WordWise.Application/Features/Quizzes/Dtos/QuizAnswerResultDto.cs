using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Features.Quizzes.Dtos
{
    public class QuizAnswerResultDto
    {
        public bool IsCorrect { get; set; }
        public string CorrectAnswer { get; set; } = string.Empty;
        public int XpChange { get; set; }
        public int TotalXp { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool XpAwarded { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Features.Admin.Dtos
{
    public class HardQuizDto
    {
        public Guid QuizQuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public int TotalAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public double WrongRate { get; set; } 
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordWise.Domain.Enums;

namespace WordWise.Domain.Entities
{
    public class User : BaseEntity
    {
        public string Email { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.User;

        public bool IsEmailConfirmed { get; set; } = false;

        public string? EmailConfirmationTokenHash { get; set; }

        public DateTime? EmailConfirmationTokenExpiresAt { get; set; }

        public DateTime? EmailConfirmedAt { get; set; }

        public ICollection<UserWord> UserWords { get; set; } = new List<UserWord>();
        public ICollection<UserXpHistory> XpHistory { get; set; } = new List<UserXpHistory>();
        public ICollection<UserQuizAnswer> QuizAnswers { get; set; } = new List<UserQuizAnswer>();
        public ICollection<VideoCandidate> ApprovedCandidates { get; set; } = new List<VideoCandidate>();
    }
}

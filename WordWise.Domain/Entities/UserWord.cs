using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Domain.Entities
{
    public class UserWord : BaseEntity
    {
        public Guid UserId { get; set; }

        public User User { get; set; } = null!;

        public Guid WordId { get; set; }

        public Word Word { get; set; } = null!;

        public int KnownLevel { get; set; } = 0;

        public DateTime? NextReviewAt { get; set; }

        public int ReviewCount { get; set; } = 0;

        public string? PersonalNote { get; set; }
    }
}

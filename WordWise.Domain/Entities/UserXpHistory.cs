using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordWise.Domain.Enums;

namespace WordWise.Domain.Entities
{
    public class UserXpHistory : BaseEntity
    {
        public Guid UserId { get; set; }

        public User User { get; set; } = null!;

        public int XpChange { get; set; }

        public int TotalXpAfterChange { get; set; }

        public XpReason Reason { get; set; }
    }
}

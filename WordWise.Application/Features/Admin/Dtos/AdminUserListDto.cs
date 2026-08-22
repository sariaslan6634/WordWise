using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Features.Admin.Dtos
{
    public class AdminUserListDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int TotalXp { get; set; }
        public DateTime MemberSince { get; set; }
        public DateTime? LastActiveAt { get; set; }
    }
}

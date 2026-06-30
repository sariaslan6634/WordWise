using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordWise.Domain.Enums;

namespace WordWise.Application.Common.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(Guid Id, string email,UserRole role);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Common.Interfaces
{
    public interface IEmailConfirmationService
    {
        string GenerateToken();
        string HashToken(string token);

        bool VerifyToken(string token,string storedHash);

    }
}

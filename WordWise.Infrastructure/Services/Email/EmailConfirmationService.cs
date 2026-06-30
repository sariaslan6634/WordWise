using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WordWise.Application.Common.Interfaces;

namespace WordWise.Infrastructure.Services.Email
{
    public class EmailConfirmationService : IEmailConfirmationService
    {
        public string GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        public string HashToken(string token)
        {
            var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }

        public bool VerifyToken(string token, string storedHash)
        {
            var computedHash = HashToken(storedHash);

            return CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(computedHash),
                Convert.FromBase64String(storedHash)
                );
        }
    }
}

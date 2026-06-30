using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Common.Interfaces
{
    public interface IEmailSender
    {
        Task SendAsync(string toEmail, string Subject, string htmlBody, CancellationToken cancellationToken = default);
    }
}

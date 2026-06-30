using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Common.Exceptions
{
    public class ConflictException : Exception
    {
        public ConflictException(string entityName, string conflictDetail)
            : base($"'{entityName}' için çakışma oluştu: {conflictDetail}") { }

        public ConflictException(string message)
            : base(message) { }
    }
}

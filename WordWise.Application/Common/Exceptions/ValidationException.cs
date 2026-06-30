using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Common.Exceptions
{
    public class ValidationException : Exception
    {
        public IReadOnlyDictionary<string, string[]> Errors { get; }

        public ValidationException(IDictionary<string, string[]> failures)
            : base("Doğrulama başarısız oldu.")
        {
            Errors = failures.AsReadOnly();
        }

        public ValidationException(string field, string error)
            : base("Doğrulama başarısız oldu.")
        {
            Errors = new Dictionary<string, string[]>
            {
                { field, new[] { error } }
            }.AsReadOnly();
        }
    }
}

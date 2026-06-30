using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace WordWise.Application.Common.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string entityName, object key)
            : base($"'{entityName}' adlı varlık, '{key}' anahtarıyla bulunamadı") { }

        public NotFoundException(string message)
            : base(message) { }
    }
}

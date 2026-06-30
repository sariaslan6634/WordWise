using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Common.Exceptions
{
    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message = "Bu işlemi gerçekleştirme yetkiniz bulunmamaktadır.") : base(message){ }
    }
}

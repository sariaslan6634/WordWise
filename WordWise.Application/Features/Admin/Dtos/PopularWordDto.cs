using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Features.Admin.Dtos
{
    public class PopularWordDto
    {
        public string Text { get; set; } = string.Empty;
        public int SaveCount { get; set; }
    }
}

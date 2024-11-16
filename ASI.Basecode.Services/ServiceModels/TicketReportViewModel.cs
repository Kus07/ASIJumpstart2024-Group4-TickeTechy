using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.ServiceModels
{
    public class TicketReportViewModel
    {
        public Dictionary<string, int> StatusCounts { get; set; }
        public Dictionary<string, int> CategoryCounts { get; set; }
    }

}

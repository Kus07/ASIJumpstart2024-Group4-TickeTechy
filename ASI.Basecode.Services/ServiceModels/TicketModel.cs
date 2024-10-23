using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.ServiceModels
{
    public class TicketModel
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string CreatedBy { get; set; }
        public string AssignedTo { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Priority { get; set; }
        public string AttachmentPath { get; set; }
        public string Status { get; set; }
    }
}

using ASI.Basecode.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.ServiceModels
{
    public class ViewTicketModel
    {
        public Ticket Ticket { get; set; }
        public string Agent { get; set; }
        public string Customer { get; set; }
        public List<User> Agents { get; set; }
        public List<TicketMessage> Messages { get; set; }
    }
}

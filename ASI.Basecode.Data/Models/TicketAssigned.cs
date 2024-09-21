using System;
using System.Collections.Generic;

namespace ASI.Basecode.Data.Models
{
    public partial class TicketAssigned
    {
        public int Id { get; set; }
        public int? TicketId { get; set; }
        public int? AgentId { get; set; }

        public virtual User Agent { get; set; }
        public virtual Ticket Ticket { get; set; }
    }
}

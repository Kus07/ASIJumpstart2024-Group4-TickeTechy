using System;
using System.Collections.Generic;

namespace ASI.Basecode.Data.Models
{
    public partial class Feedback
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string Comments { get; set; }
        public int? Star { get; set; }
        public int? AgentId { get; set; }
        public int? TicketId { get; set; }

        public virtual User Agent { get; set; }
        public virtual Ticket Ticket { get; set; }
        public virtual User User { get; set; }
    }
}

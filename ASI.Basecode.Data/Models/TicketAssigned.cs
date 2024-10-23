﻿using System;
using System.Collections.Generic;

namespace ASI.Basecode.Data.Models
{
    public partial class TicketAssigned
    {
        public int Id { get; set; }
        public int? TicketId { get; set; }
        public int? AgentId { get; set; }
        public int? ReassignedToId { get; set; }
        public string Status { get; set; }

        public virtual User Agent { get; set; }
        public virtual User ReassignedTo { get; set; }
        public virtual Ticket Ticket { get; set; }
    }
}

using System;
using System.Collections.Generic;

namespace ASI.Basecode.Data.Models
{
    public partial class Status
    {
        public Status()
        {
            Tickets = new HashSet<Ticket>();
        }

        public int Id { get; set; }
        public string StatusName { get; set; }

        public virtual ICollection<Ticket> Tickets { get; set; }
    }
}

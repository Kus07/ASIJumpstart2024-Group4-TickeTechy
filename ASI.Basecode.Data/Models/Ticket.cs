using System;
using System.Collections.Generic;

namespace ASI.Basecode.Data.Models
{
    public partial class Ticket
    {
        public Ticket()
        {
            Notifications = new HashSet<Notification>();
            TicketAssigneds = new HashSet<TicketAssigned>();
            TicketMessages = new HashSet<TicketMessage>();
        }

        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? CategoryId { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public string Attachments { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? StatusId { get; set; }
        public int? Reassigned { get; set; }

        public virtual Category Category { get; set; }
        public virtual Status Status { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual ICollection<TicketAssigned> TicketAssigneds { get; set; }
        public virtual ICollection<TicketMessage> TicketMessages { get; set; }
    }
}

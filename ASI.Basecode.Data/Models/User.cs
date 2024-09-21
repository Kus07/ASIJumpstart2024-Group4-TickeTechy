using System;
using System.Collections.Generic;

namespace ASI.Basecode.Data.Models
{
    public partial class User
    {
        public User()
        {
            FeedbackAgents = new HashSet<Feedback>();
            FeedbackUsers = new HashSet<Feedback>();
            NotificationFromUsers = new HashSet<Notification>();
            NotificationToUsers = new HashSet<Notification>();
            TicketAssigneds = new HashSet<TicketAssigned>();
            TicketMessages = new HashSet<TicketMessage>();
            Tickets = new HashSet<Ticket>();
            UserDetails = new HashSet<UserDetail>();
        }

        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public int? RoleId { get; set; }
        public int? DepartmentId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string ForgotPassOtp { get; set; }

        public virtual Department Department { get; set; }
        public virtual Role Role { get; set; }
        public virtual ICollection<Feedback> FeedbackAgents { get; set; }
        public virtual ICollection<Feedback> FeedbackUsers { get; set; }
        public virtual ICollection<Notification> NotificationFromUsers { get; set; }
        public virtual ICollection<Notification> NotificationToUsers { get; set; }
        public virtual ICollection<TicketAssigned> TicketAssigneds { get; set; }
        public virtual ICollection<TicketMessage> TicketMessages { get; set; }
        public virtual ICollection<Ticket> Tickets { get; set; }
        public virtual ICollection<UserDetail> UserDetails { get; set; }
    }
}

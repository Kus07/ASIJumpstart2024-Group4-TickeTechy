using System;
using System.Collections.Generic;

namespace ASI.Basecode.WebApp.Models
{
    public partial class Notification
    {
        public int Id { get; set; }
        public int? FromUserId { get; set; }
        public int? ToUserId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime? DateCreated { get; set; }
        public string Status { get; set; }
        public int? TicketId { get; set; }
    }
}

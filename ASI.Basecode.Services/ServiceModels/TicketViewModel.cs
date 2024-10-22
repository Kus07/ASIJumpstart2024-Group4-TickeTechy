using ASI.Basecode.Data.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace ASI.Basecode.Services.ServiceModels
{
    public class TicketViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CategoryId { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public IFormFile Attachments { get; set; }
        public string AttachmentPath{ get; set; }
        public List<Category> Categories { get; set; } = new List<Category>();
        public string Status { get; set; }
        public IEnumerable<Ticket> Tickets { get; set; }
        public string CategoryName { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ArticleViewModel> Articles { get; set; }
    }

}

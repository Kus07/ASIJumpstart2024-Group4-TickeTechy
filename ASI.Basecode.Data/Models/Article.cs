using System;
using System.Collections.Generic;

namespace ASI.Basecode.Data.Models
{
    public partial class Article
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Author { get; set; }
        public string Status { get; set; }
        public DateTime? PublishDate { get; set; }
        public DateTime? LastmodifiedDate { get; set; }
        public int? CategoryId { get; set; }
        public int? UserDetailId { get; set; }
        public string Attachments { get; set; }

        public virtual Category Category { get; set; }
        public virtual UserDetail UserDetail { get; set; }
    }
}

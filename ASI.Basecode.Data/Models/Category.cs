using System;
using System.Collections.Generic;

namespace ASI.Basecode.Data.Models
{
    public partial class Category
    {
        public Category()
        {
            Articles = new HashSet<Article>();
            Tickets = new HashSet<Ticket>();
        }

        public int Id { get; set; }
        public string CategoryName { get; set; }

        public virtual ICollection<Article> Articles { get; set; }
        public virtual ICollection<Ticket> Tickets { get; set; }
    }
}

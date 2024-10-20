﻿using System;
using System.Collections.Generic;

namespace ASI.Basecode.Data.Models
{
    public partial class UserDetail
    {
        public UserDetail()
        {
            Articles = new HashSet<Article>();
        }

        public int Id { get; set; }
        public int? UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ContactNo { get; set; }
        public string ProfilePicturePath { get; set; }

        public virtual User Users { get; set; }
        public virtual ICollection<Article> Articles { get; set; }
    }
}

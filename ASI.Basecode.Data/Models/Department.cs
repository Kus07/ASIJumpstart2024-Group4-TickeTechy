using System;
using System.Collections.Generic;

namespace ASI.Basecode.Data.Models
{
    public partial class Department
    {
        public Department()
        {
            Users = new HashSet<User>();
        }

        public int Id { get; set; }
        public string DepartmentName { get; set; }

        public virtual ICollection<User> Users { get; set; }
    }
}

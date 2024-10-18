using ASI.Basecode.Data.Models;
using System.Collections.Generic;

namespace ASI.Basecode.Services.ServiceModels
{
    public class AdminViewModel
    {
        public List<UserDetail> Customers { get; set; }
        public List<UserDetail> Agents { get; set; }
        public List<UserDetail> Admins { get; set; }
        public List<Article> Articles { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Models;

namespace ASI.Basecode.Services.ServiceModels
{
    public class ArticleViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Author { get; set; }
        public string Status { get; set; }
        public DateTime? PublishDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public int? CategoryId { get; set; }
        public string ProfilePicturePath { get; set; }
        public List<Category> Categories { get; set; }
        public virtual Category Category { get; set; }
        public virtual UserDetail UserDetail { get; set; }
        public ArticleViewModel()
        {
            Categories = new List<Category>();
            // Populate the Categories list with the available categories
            // For example, you can fetch the categories from a database or any other data source
            // Categories = _categoryRepo.GetAllCategories();
        }

    }
}

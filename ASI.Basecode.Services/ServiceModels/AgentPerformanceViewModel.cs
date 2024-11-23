using ASI.Basecode.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.ServiceModels
{
    public class AgentPerformanceViewModel
    {
        public double AverageTicketResolutionTime { get; set; }
        public double? AverageFeedbackRating { get; set; }
        public List<CategoryTicketCount> AssignedTicketsByCategory { get; set; }
        public int TicketsAssigned { get; set; }
        public int TicketsWithFeedback { get; set; }
        public int TicketsOngoing { get; set; }
        public int TicketsResolved { get; set; }


        //public Dictionary<string, int> StatusCounts { get; set; }
        //public int TotalTickets { get; set; }
        //public int CustomerCount { get; set; }
        //public int AgentCount { get; set; }
        //public int AdminCount { get; set; }
        //public Dictionary<string, int> CategoryCounts { get; set; }
        //public Dictionary<string, int> PriorityCounts { get; set; }
    }
    public class CategoryTicketCount
    {
        public Category Category { get; set; }
        public int Count { get; set; }
    }
}

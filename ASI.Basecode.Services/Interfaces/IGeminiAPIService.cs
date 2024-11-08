using ASI.Basecode.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IGeminiAPIService
    {
        Task<int> AssignTicket(string description, string category);
        Task<string> GenerateTicketSummary(string ticketDescription, string ticketCategory, IEnumerable<TicketMessage> conversationHistory, IFormFile image);

    }
}

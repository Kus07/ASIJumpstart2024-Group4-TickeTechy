using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Data;

namespace TickeTechy.Services.Implementations
{
    public class AgentService
    {
        public int GetUserWithSmallestLoad(int departmentId)
        {
            // Get all users within the department
            using (var db = new AllianceJumpstartContext())
            {
                if(departmentId == 0)
                {
                    departmentId = 7;
                }
                var usersInDepartment = db.Users
                .Where(u => u.DepartmentId == departmentId)
                .ToList();

                // For each user, count the number of assigned tickets
                var userWithTicketCount = usersInDepartment
                    .Select(user => new
                    {
                        UserId = user.Id,
                        TicketCount = db.Tickets.Count(t => t.UserId == user.Id)
                    })
                    .OrderBy(ut => ut.TicketCount) // Sort by the smallest number of tickets
                    .FirstOrDefault(); // Get the user with the smallest load
                return userWithTicketCount?.UserId ?? -1;
            }
        }
    }
}

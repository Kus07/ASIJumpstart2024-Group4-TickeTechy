using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.Manager;
using ASI.Basecode.Services.ServiceModels;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using static ASI.Basecode.Resources.Constants.Enums;
using static ASI.Basecode.Resources.Messages.Common;

namespace ASI.Basecode.WebApp.Controllers
{
    [Authorize(Roles = "1,2")]
    public class TicketController : BaseController
    {

        private readonly IUserService _userService;
        private readonly IGeminiAPIService _geminiService;
        private readonly IAgentService _agentService;

        public TicketController(MailManager mailManager, IUserService userService, IGeminiAPIService geminiAPIService, IAgentService agentService, IHttpContextAccessor httpContextAccessor) : base(mailManager, httpContextAccessor)
        {
            _userService = userService;
            _geminiService = geminiAPIService;
            _agentService = agentService;
        }


        public int GetUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        [HttpGet]
        public IActionResult Create()
        {
            var viewModel = new TicketViewModel
            {
                UserId = GetUserId(),
                Categories = _categoryRepo.GetAll().ToList()
            };
            return View("CustomerDashboard", viewModel);
        }

        [HttpGet]
        public IActionResult View(int id)
        {
            var ticket = _ticketRepo.Get(id);
            int userId = GetUserId();

            // trappings
            if (ticket == null)
            {
                TempData["error"] = "Invalid ticket ID";
                if (User.IsInRole("1"))
                {
                    return RedirectToAction("CustomerDashboard", "Home");
                }
                else if (User.IsInRole("2"))
                {
                    return RedirectToAction("AgentDashboard", "Home");
                }
            }

            var ticketAssigned = _ticketAssignedRepo.Table.Where(m => m.TicketId == ticket.Id)
                .Include(m => m.Ticket)
                .FirstOrDefault();
            if (User.IsInRole("2"))
            {
                if (ticketAssigned.AgentId != userId)
                {
                    TempData["error"] = "Unauthorized access";
                    return RedirectToAction("AgentDashboard", "Home");
                }
            }
            else if (User.IsInRole("1"))
            {
                if (ticket.UserId != userId)
                {
                    TempData["error"] = "Unauthorized access";
                    return RedirectToAction("CustomerDashboard", "Home");
                }
            }

            var customerFirstName = _userDetailRepo.Table.Where(m => m.UserId == ticket.UserId).Select(m => m.FirstName).FirstOrDefault();
            var agentName = _userDetailRepo.Table.Where(m => m.UserId == ticketAssigned.AgentId).Select(m => m.FirstName).FirstOrDefault();
            var agents = _userRepo.Table.Where(m => m.RoleId == 2).Include(m => m.Department).ToList();
            var messages = _ticketMessageRepo.Table
                .Where(m => m.TicketId == ticket.Id)
                .OrderBy(m => m.CreatedAt)  
                .ToList(); 
            
            var model = new ViewTicketModel()
            {
                Ticket = ticket,
                Customer = customerFirstName,
                Agent = agentName,
                Agents = agents,
                Messages = messages
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult ReassignTicket(int ticketId, int newAgent)
        {
            var ticket = _ticketRepo.Table.FirstOrDefault(t => t.Id == ticketId);
            var newAgentUser = _userDetailRepo.Table.Where(m => m.UserId == newAgent).FirstOrDefault();
            var ticketAssigned = _ticketAssignedRepo.Table.Where(m => m.TicketId == ticketId).FirstOrDefault();
            if (ticket != null && ticketAssigned != null)
            {
                ticketAssigned.AgentId = newAgent;
                _ticketAssignedRepo.Update(ticketAssigned.Id, ticketAssigned);
                TempData["message"] = $"Successfully reassigned ticket to {newAgentUser.FirstName}!";
                return RedirectToAction("AgentDashboard", "Home");

            }

            TempData["error"] = "Invalid ID.";
            return RedirectToAction("View", "Ticket", new {id = ticket.Id});
        }

        private void CheckAndCloseTicketIfNeeded(Ticket ticket)
        {
           
            if (ticket.CreatedAt.HasValue)
            {
                // Check if the ticket has been open for more than 12 hours
                if (ticket.CreatedAt.Value.AddHours(12) < DateTime.Now && ticket.StatusId == Convert.ToInt32(TicketStatus.OPEN))
                {
                    ticket.StatusId = Convert.ToInt32(TicketStatus.CLOSED);
                    _ticketRepo.Update(ticket.Id, ticket);
                }
            }
        }


        [HttpPost]
        public IActionResult SendMessage(int ticketId, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return Json(new { success = false, responseText = "Message cannot be empty" });
            }

            var ticket = _ticketRepo.Get(ticketId);
            int currUserId = GetUserId();

            var ticketMessage = new TicketMessage()
            {
                CreatedAt = DateTime.Now,
                Message = message,
                TicketId = ticketId,
                UserId = currUserId,
            };

            _ticketMessageRepo.Create(ticketMessage);

            var ticketAssigned = _ticketAssignedRepo.Table.Where(m => m.TicketId == ticket.Id).FirstOrDefault();

            ticket.StatusId = Convert.ToInt32(TicketStatus.ONGOING);

            var notif = new Notification()
            {
                ToUserId = ticketAssigned.AgentId,
                FromUserId = ticket.UserId,
                Content = NotifToAgent + ticket.Id,
                DateCreated = DateTime.Now,
                Title = $"Ticket #{ticket.Id}"
            };

            _notificationRepo.Create(notif);


            _ticketRepo.Update(ticket.Id, ticket);
            return Json(new { success = true, responseText = "Message sent successfully!" });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TicketViewModel viewModel)
        {
            string attachmentPath = "";
            if (viewModel.Attachments != null && viewModel.Attachments.Length > 0)
            {
                // Create the path for the Attachments folder inside wwwroot
                var attachmentsPicturesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Attachments");

                // Ensure the folder exists
                if (!Directory.Exists(attachmentsPicturesFolder))
                {
                    Directory.CreateDirectory(attachmentsPicturesFolder);
                }

                // Create a unique file name to avoid overwriting files with the same name
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + viewModel.Attachments.FileName;

                // Full file path
                string filePath = Path.Combine(attachmentsPicturesFolder, uniqueFileName);

                // Copy the file to the target location
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await viewModel.Attachments.CopyToAsync(fileStream);
                }

                // Save the relative path to the file (relative to wwwroot)
                attachmentPath = Path.Combine("Attachments", uniqueFileName).Replace("\\", "/");
            }
            var ticket = new Ticket
            {
                UserId = viewModel.UserId,
                CategoryId = viewModel.CategoryId,
                Description = viewModel.Description,
                Priority = viewModel.Priority,
                Attachments = attachmentPath,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                StatusId = Convert.ToInt32(TicketStatus.OPEN),
            };

            var result = _ticketRepo.Create(ticket);
            if (result == ErrorCode.Success)
            {
                var category = _categoryRepo.Get(viewModel.CategoryId);
                var newTicket = _ticketRepo.Table.Where(m => m.Description.Equals(ticket.Description) && m.CategoryId == ticket.CategoryId && m.UserId == ticket.UserId).FirstOrDefault();

                var departmentIdResponse = await _geminiService.AssignTicket(viewModel.Description, category.CategoryName);


                int agentId = _agentService.GetUserWithSmallestLoad(departmentIdResponse);
                var assignedTicket = new TicketAssigned()
                {
                    TicketId = newTicket.Id,
                    AgentId = agentId
                };
                _ticketAssignedRepo.Create(assignedTicket);
                return RedirectToAction("Tickets", "Home");

            }

            ModelState.AddModelError("", "There was an error creating the ticket.");

            viewModel.Categories = _categoryRepo.GetAll().ToList();
            return RedirectToAction("CustomerDashboard", "Home", new { viewModel = viewModel }); // Return the current view with the error
        }



        [HttpGet]
        public IActionResult Edit(int id)
        {
            var ticket = _ticketRepo.FindByCondition(t => t.Id == id && t.UserId == GetUserId()).FirstOrDefault();
            if (ticket == null)
            {
                return NotFound();
            }

            var viewModel = new TicketViewModel
            {
                Id = ticket.Id,
                UserId = (int)ticket.UserId,
                CategoryId = (int)ticket.CategoryId,
                Description = ticket.Description,
                Priority = ticket.Priority,
                AttachmentPath = ticket.Attachments,
                Categories = _categoryRepo.GetAll().ToList()
            };

            return View("Tickets", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(TicketViewModel viewModel)
        {
            Console.WriteLine($"Editing Ticket: Id={viewModel.Id}, UserId={viewModel.UserId}");
            var ticket = _ticketRepo.FindByCondition(t => t.Id == viewModel.Id && t.UserId == viewModel.UserId).FirstOrDefault();
            if (ticket == null)
            {
                return NotFound();
            }


            ticket.CategoryId = viewModel.CategoryId;
            ticket.Description = viewModel.Description;
            ticket.Priority = viewModel.Priority;
            ticket.Attachments = viewModel.AttachmentPath;
            ticket.UpdatedAt = DateTime.Now;

            var result = _ticketRepo.Update(ticket.Id, ticket);
            if (result == ErrorCode.Success)
            {
                return RedirectToAction("Tickets", "Home");
            }

            ModelState.AddModelError("", "There was an error updating the ticket.");


            viewModel.Categories = _categoryRepo.GetAll().ToList();
            return View("Tickets", viewModel);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var ticket = _ticketRepo.FindByCondition(t => t.Id == id && t.UserId == GetUserId()).FirstOrDefault();
            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed([FromBody] int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid ticket ID.");
            }

            var ticket = _ticketRepo.FindByCondition(t => t.Id == id).FirstOrDefault();
            if (ticket == null)
            {
                return NotFound();
            }

            var result = _ticketRepo.Delete(ticket.Id);
            if (result == ErrorCode.Success)
            {
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Error deleting ticket." });
        }

    }
}

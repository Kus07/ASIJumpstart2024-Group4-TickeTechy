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
using static ASI.Basecode.Resources.Messages.Errors;
using System.Text.RegularExpressions;
using ASI.Basecode.Services.Services;
using Newtonsoft.Json;
using System.Net.Http;
using Microsoft.Extensions.FileProviders;

namespace ASI.Basecode.WebApp.Controllers
{
    [Authorize(Roles = "1,2")]
    public class TicketController : BaseController
    {

        private readonly IUserService _userService;
        private readonly IGeminiAPIService _geminiService;
        private readonly IAgentService _agentService;
        private readonly IFileProvider _fileProvider;

        public TicketController(MailManager mailManager, IUserService userService, IGeminiAPIService geminiAPIService, IAgentService agentService, IHttpContextAccessor httpContextAccessor, IFileProvider fileProvider) : base(mailManager, httpContextAccessor)
        {
            _userService = userService;
            _geminiService = geminiAPIService;
            _agentService = agentService;
            _fileProvider = fileProvider;

        }

        public static DateTime utcNow = DateTime.UtcNow;

        // Define the timezone offset for UTC+08:00
        public static TimeSpan utcOffset = TimeSpan.FromHours(8); // UTC+08:00

        // Apply the timezone offset to get the local time in UTC+08:00
        Nullable<DateTime> PHTIME = utcNow + utcOffset;

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

            // Handle invalid ticket case
            if (ticket == null)
            {
                TempData["error"] = "Invalid ticket ID";
                return RedirectToAction(User.IsInRole("1") ? "CustomerDashboard" : "AgentDashboard", "Home");
            }

            var ticketAssigned = _ticketAssignedRepo.Table.Where(m => m.TicketId == ticket.Id)
                .Include(m => m.Ticket)
                .FirstOrDefault();

            if (User.IsInRole("2") && ticketAssigned.AgentId != userId && ticketAssigned.ReassignedToId != userId)
            {
                TempData["error"] = Resources.Messages.Errors.Unauthorized;
                return RedirectToAction("AgentDashboard", "Home");
            }
            else if (User.IsInRole("1") && ticket.UserId != userId)
            {
                TempData["error"] = "Unauthorized access";
                return RedirectToAction("CustomerDashboard", "Home");
            }

            var customerFirstName = _userDetailRepo.Table.Where(m => m.UserId == ticket.UserId).Select(m => m.FirstName).FirstOrDefault();
            var agentName = _userDetailRepo.Table.Where(m => m.UserId == ticketAssigned.AgentId).Select(m => m.FirstName).FirstOrDefault();
            var agents = _userRepo.Table.Where(m => m.RoleId == 2).Include(m => m.Department).ToList();
            var messages = _ticketMessageRepo.Table.Where(m => m.TicketId == ticket.Id).OrderBy(m => m.CreatedAt).ToList();

            var feedBack = _feedbackRepo.Table.Where(m => m.TicketId == ticket.Id).FirstOrDefault();

            // Set ViewBag to control buttons in the view
            ViewBag.alreadyFeedback = feedBack != null;

            // Pass feedback to ViewModel
            var model = new ViewTicketModel()
            {
                Ticket = ticket,
                Customer = customerFirstName,
                Agent = agentName,
                Agents = agents,
                Messages = messages,
                TicketAssigned = ticketAssigned,
                Feedback = feedBack // Set the feedback in the model
            };

            if (UnreadNotifications())
            {
                TempData["notifications"] = "true";
            }

            return View(model);
        }



        private IActionResult RedirectToDashboard()
        {
            if (User.IsInRole("1"))
            {
                return RedirectToAction("CustomerDashboard", "Home");
            }
            else if (User.IsInRole("2"))
            {
                return RedirectToAction("AgentDashboard", "Home");
            }
            return RedirectToAction("Index", "Home"); 
        }



        [HttpPost]
        public IActionResult ReassignTicket(int ticketId, int newAgent)
        {
            var ticket = _ticketRepo.Table.FirstOrDefault(t => t.Id == ticketId);
            var newAgentUser = _userDetailRepo.Table.Where(m => m.UserId == newAgent).FirstOrDefault();
            var ticketAssigned = _ticketAssignedRepo.Table.Where(m => m.TicketId == ticketId).FirstOrDefault();
            if (ticket != null && ticketAssigned != null)
            {
                ticketAssigned.ReassignedToId = newAgent;
                ticket.Reassigned = (int) TicketReassigned.TRUE;
                _ticketAssignedRepo.Update(ticketAssigned.Id, ticketAssigned);
                _ticketRepo.Update(ticket.Id, ticket);
                TempData["message"] = SuccessReassign + " " + newAgentUser.FirstName +"!";
                return RedirectToAction("AgentDashboard", "Home");
            }

            TempData["error"] = InvalidIDError;
            return RedirectToAction("View", "Ticket", new {id = ticket.Id});
        }

        private void CheckAndCloseTicketIfNeeded(Ticket ticket)
        {
           
            if (ticket.CreatedAt.HasValue)
            {
                if (ticket.CreatedAt.Value.AddHours(12) < PHTIME && ticket.StatusId == Convert.ToInt32(TicketStatus.OPEN))
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


            var lastThreeMessages = _ticketMessageRepo.Table
                .Where(m => m.TicketId == ticketId)  
                .OrderByDescending(m => m.CreatedAt) 
                .Take(3)  
                .ToList();

            bool areAllFromUser = lastThreeMessages.All(m => m.UserId == currUserId);

            if (lastThreeMessages.Count == 3 && (areAllFromUser && User.IsInRole("1")) && ticket.StatusId != (int) TicketStatus.RESOLVED)
            {
                return Json(new { success = false, responseText = LimitMessage });
            }

            var ticketMessage = new TicketMessage()
            {
                CreatedAt = PHTIME,
                Message = message,
                TicketId = ticketId,
                UserId = currUserId,
            };

            _ticketMessageRepo.Create(ticketMessage);

            var ticketAssigned = _ticketAssignedRepo.Table.Where(m => m.TicketId == ticket.Id).FirstOrDefault();

            if (User.IsInRole("2"))
            {
                ticket.StatusId = Convert.ToInt32(TicketStatus.WAITINGRESPONSE);
            }
            else
            {
                ticket.StatusId = Convert.ToInt32(TicketStatus.ONGOING);
            }

            var notif = new Notification()
            {
                ToUserId = ticketAssigned.AgentId,
                FromUserId = ticket.UserId,
                Content = NotifToAgent + ticket.Id,
                DateCreated = PHTIME,
                Status = "UNREAD", 
                TicketId = ticketId, 
                Title = $"Ticket #{ticket.Id}"
            };

            _notificationRepo.Create(notif);

            _ticketRepo.Update(ticket.Id, ticket);

            string errResponse = string.Empty;
            bool emailSent = false;

            if (User.IsInRole("1")) 
            {
                var agent = _userRepo.Get(ticketAssigned.AgentId);
                var agentDetails = _userDetailRepo.Table.Where(m => m.UserId == agent.Id).FirstOrDefault();

                if (!string.IsNullOrEmpty(agent.Email) && agent.EmailNotificationSetting == 1)
                {
                    emailSent = _mailManager.EmailRespond(
                        recipientEmail: agent.Email,
                        firstName: agentDetails?.FirstName ?? "Agent",
                        ticketId: ticketId, 
                        ticketMessage: message, 
                        ref errResponse
                    );
                }
            }
            else if (User.IsInRole("2")) 
            {
                var customer = _userRepo.Get(ticket.UserId);
                var customerDetails = _userDetailRepo.Table.Where(m => m.UserId == customer.Id).FirstOrDefault();

                if (!string.IsNullOrEmpty(customer.Email) && customer.EmailNotificationSetting == 1)
                {
                    emailSent = _mailManager.EmailRespond(
                        recipientEmail: customer.Email,
                        firstName: customerDetails?.FirstName ?? "Customer",
                        ticketId: ticketId, 
                        ticketMessage: message, 
                        ref errResponse
                    );
                }
            }

            if (!emailSent && !string.IsNullOrEmpty(errResponse))
            {
                return Json(new { success = true, responseText = "Message sent, but failed to send email: " + errResponse });
            }

            return Json(new { success = true, responseText = "Message sent successfully!" });
        }


        [HttpPost]
        public async Task<IActionResult> SendMessageAndAccept(int ticketId, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return Json(new { success = false, responseText = "Message cannot be empty" });
            }

            var ticket = _ticketRepo.Get(ticketId);
            int currUserId = GetUserId();


            var lastThreeMessages = _ticketMessageRepo.Table
                .Where(m => m.TicketId == ticketId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(3)
                .ToList();

            bool areAllFromUser = lastThreeMessages.All(m => m.UserId == currUserId);

            if (lastThreeMessages.Count == 3 && (areAllFromUser && User.IsInRole("1")) && ticket.StatusId != (int)TicketStatus.RESOLVED)
            {
                return Json(new { success = false, responseText = LimitMessage });
            }

            var ticketMessage = new TicketMessage()
            {
                CreatedAt = PHTIME,
                Message = message,
                TicketId = ticketId,
                UserId = currUserId,
            };

            _ticketMessageRepo.Create(ticketMessage);

            var ticketAssigned = _ticketAssignedRepo.Table.Where(m => m.TicketId == ticket.Id).FirstOrDefault();

            if (User.IsInRole("2"))
            {
                ticket.StatusId = Convert.ToInt32(TicketStatus.CLOSED);
            }
            else
            {
                ticket.StatusId = Convert.ToInt32(TicketStatus.CLOSED);
            }

            var notif = new Notification()
            {
                ToUserId = ticketAssigned.AgentId,
                FromUserId = ticket.UserId,
                Content = NotifToAgent + ticket.Id,
                DateCreated = PHTIME,
                Status = "UNREAD",
                TicketId = ticketId,
                Title = $"Ticket #{ticket.Id}"
            };

            _notificationRepo.Create(notif);

            _ticketRepo.Update(ticket.Id, ticket);

            string errResponse = string.Empty;
            bool emailSent = false;

            if (User.IsInRole("1"))
            {
                var agent = _userRepo.Get(ticketAssigned.AgentId);
                var agentDetails = _userDetailRepo.Table.Where(m => m.UserId == agent.Id).FirstOrDefault();

                if (!string.IsNullOrEmpty(agent.Email) && agent.EmailNotificationSetting == 1)
                {
                    emailSent = _mailManager.EmailRespond(
                        recipientEmail: agent.Email,
                        firstName: agentDetails?.FirstName ?? "Agent",
                        ticketId: ticketId,
                        ticketMessage: message,
                        ref errResponse
                    );
                }
            }
            else if (User.IsInRole("2"))
            {
                var customer = _userRepo.Get(ticket.UserId);
                var customerDetails = _userDetailRepo.Table.Where(m => m.UserId == customer.Id).FirstOrDefault();

                if (!string.IsNullOrEmpty(customer.Email) && customer.EmailNotificationSetting == 1)
                {
                    emailSent = _mailManager.EmailRespond(
                        recipientEmail: customer.Email,
                        firstName: customerDetails?.FirstName ?? "Customer",
                        ticketId: ticketId,
                        ticketMessage: message,
                        ref errResponse
                    );
                }
            }

            var ticketSummary = await GetTicketSummary(ticketId);
            ticket.Summary = ticketSummary;
            _ticketRepo.Update(ticketId, ticket);

            if (!emailSent && !string.IsNullOrEmpty(errResponse))
            {
                return Json(new { success = true, responseText = "Message sent, but failed to send email: " + errResponse });
            }

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
            var descriptionWithoutTags = Regex.Replace(viewModel.Description, "<.*?>", string.Empty); // Strips all HTML tags

            var ticket = new Ticket
            {
                UserId = viewModel.UserId,
                CategoryId = viewModel.CategoryId,
                Description = descriptionWithoutTags,
                Priority = viewModel.Priority,
                Attachments = attachmentPath,
                CreatedAt = PHTIME,
                UpdatedAt = PHTIME,
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
                    AgentId = agentId,
                    Status = "PENDING"
                };
                _ticketAssignedRepo.Create(assignedTicket);
                TempData["message"] = SuccessCreateTicket;
                return RedirectToAction("Tickets", "Home");
            }

            ModelState.AddModelError("", CreatingTicketError);
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
            ticket.UpdatedAt = PHTIME;

            var result = _ticketRepo.Update(ticket.Id, ticket);
            if (result == ErrorCode.Success)
            {
                TempData["message"] = SuccessEditTicket;
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

            //var result = _ticketRepo.Delete(ticket.Id);
            ticket.StatusId = 6; // DELETED
            var result = _ticketRepo.Update(ticket.Id, ticket);
            if (result == ErrorCode.Success)
            {
                TempData["message"] = SuccessDeleteTicket;
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Error deleting ticket." });
        }


        [Authorize(Roles = "2")] 
        [HttpPost]
        public IActionResult MarkedAsResolved(int ticketId)
        {
            var ticket = _ticketRepo.Get(ticketId);
            if (ticket == null)
            {
                return Json(new { success = false, message = "Invalid ticket ID." });
            }

            ticket.StatusId = Convert.ToInt32(TicketStatus.RESOLVED);
            ticket.UpdatedAt = PHTIME;

            var customer = _userRepo.Get(ticket.UserId);
            var userDetails = _userDetailRepo.Table.FirstOrDefault(m => m.UserId == customer.Id);

            string errResponse = string.Empty;
            bool emailSent = false;

            if (!string.IsNullOrEmpty(customer.Email) && customer.EmailNotificationSetting == 1)
            {
                emailSent = _mailManager.ResolveNotif(
                    recipientEmail: customer.Email,
                    firstName: userDetails?.FirstName ?? "Customer",
                    ticketId: ticketId,
                    ticketMessage: "Your ticket has been marked as resolved. Please confirm if the issue is resolved.",
                    ref errResponse
                );
            }


            _ticketRepo.Update(ticketId, ticket);

            if (!emailSent)
            {
                return Json(new { success = true, message = "Successfully marked as resolved, but failed to send email: " + errResponse });
            }

            return Json(new { success = true, message = "Successfully marked as resolved! Waiting for customer's confirmation." });
        }


        [Authorize(Roles = "1")] 
        [HttpPost]
        public async Task<IActionResult> AcceptResolution(int ticketId)
        {
            var ticket = _ticketRepo.Get(ticketId);
            if (ticket == null)
            {
                return Json(new { success = false, message = "Invalid ticket ID." });
            }


            ticket.StatusId = Convert.ToInt32(TicketStatus.CLOSED);
            ticket.UpdatedAt = PHTIME;


            var ticketAssigned = _ticketAssignedRepo.Table.FirstOrDefault(m => m.TicketId == ticketId);
            if (ticketAssigned == null)
            {
                return Json(new { success = false, message = "Agent assignment not found." });
            }

            var agent = _userRepo.Get(ticketAssigned.AgentId); 
            if (agent == null)
            {
                return Json(new { success = false, message = "Agent not found." });
            }

            var agentDetails = _userDetailRepo.Table.FirstOrDefault(m => m.UserId == agent.Id);

            string errResponse = string.Empty;
            bool emailSent = false;

            if (!string.IsNullOrEmpty(agent.Email) && agent.EmailNotificationSetting == 1)
            {

                emailSent = _mailManager.ResolveNotifToClient(
                    recipientEmail: agent.Email,
                    firstName: agentDetails?.FirstName ?? "Agent", 
                    ticketId: ticketId,
                    ticketMessage: "The customer has confirmed that the problem has been resolved.",
                    ref errResponse
                );
            }
            var ticketSummary = await GetTicketSummary(ticketId);
            ticket.Summary = ticketSummary;
            _ticketRepo.Update(ticketId, ticket);


            if (!emailSent)
            {
                return Json(new { success = true, message = "Ticket officially resolved, but failed to send email to the agent: " + errResponse });
            }

            return Json(new { success = true, message = "Ticket officially resolved and the agent has been notified." });
        }

        public bool UnreadNotifications()
        {
            int currentUserId = GetUserId();
            bool unreadNotificationsAvailable = false;

            var user = _userRepo.Get(currentUserId);
            var notifications = _notificationRepo.Table.Where(m => m.ToUserId == user.Id && m.Status.Equals("UNREAD")).ToList();

            if (notifications.Count > 0)
            {
                unreadNotificationsAvailable = true;
            }

            return unreadNotificationsAvailable;

        }


        [HttpPost]
        public IActionResult SubmitFeedback(int ticketId, string comment, int rating)
        {
            var ticket = _ticketRepo.Table.Where(m => m.Id == ticketId).Include(m => m.User).FirstOrDefault();
            var ticketAssigned = _ticketAssignedRepo.Table.Where(m => m.TicketId == ticket.Id).FirstOrDefault();
            if (ticket == null || ticketAssigned == null)
            {
                TempData["error"] = "Invalid Ticket";
                return RedirectToAction("CustomerDashboard", "Home");
            }

            if (ModelState.IsValid)
            {
                // Assuming you have a Feedback model to save the data
                Feedback feedback = new Feedback
                {
                    Comments = comment,
                    Star = rating,
                    AgentId = ticketAssigned.AgentId,
                    UserId = GetUserId(),
                    TicketId = ticket.Id
                };

                _feedbackRepo.Create(feedback);


                TempData["message"] = "Feedback submitted!";
                // Redirect or return a response
                return RedirectToAction("View", "Ticket", new { id = ticketId }); // Redirect to the ticket details page after submission
            }

            TempData["message"] = "Error submitting feedback!";
            return RedirectToAction("View", "Ticket", new { id = ticketId }); // Redirect to the ticket details page after submission

        }
        [HttpPost]
        public async Task<String> GetTicketSummary(int ticketId)
        {
            var ticket = _ticketRepo.Get(ticketId);
            var ticketDescription = ticket.Description;
            var ticketCategory = ticket.Category.CategoryName;
            var conversationHistory = _ticketMessageRepo.Table.Where(m => m.TicketId == ticket.Id).OrderBy(m => m.CreatedAt).ToList();
            var content = new MultipartFormDataContent();
            var attachment = ticket.Attachments;
            //declare image as null
            IFormFile image = null;
            if (!String.IsNullOrEmpty(attachment))
            {

                var filePath = Path.Combine(attachment);

                // Get the file info
                var fileInfo = _fileProvider.GetFileInfo(filePath);

                // Check if file exists
                if (!fileInfo.Exists)
                {
                    throw new FileNotFoundException("File not found", attachment);
                }

                // Open a stream to read the file
                using var stream = new FileStream(fileInfo.PhysicalPath, FileMode.Open, FileAccess.Read);

                // Create an IFormFile using a MemoryStream
                var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0;

                image = new FormFile(memoryStream, 0, memoryStream.Length, "file", fileInfo.Name);

            }
            var ticketSummary = await _geminiService.GenerateTicketSummary(ticketDescription, ticketCategory, conversationHistory, image);
            return ticketSummary;
        }

        

    }
}

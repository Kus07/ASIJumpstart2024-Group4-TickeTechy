using ASI.Basecode.Data.Models;
using ASI.Basecode.Resources.Messages;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.Manager;
using ASI.Basecode.Services.ServiceModels;
using ASI.Basecode.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Linq;
using System;
using System.Data.Entity;
using Microsoft.AspNetCore.Http;
using static ASI.Basecode.Resources.Constants.Enums;
using static ASI.Basecode.Resources.Messages.Errors;
using static ASI.Basecode.Resources.Messages.Common;
using AutoMapper;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace ASI.Basecode.WebApp.Controllers
{
    [Authorize(Roles = "3,4")]
    public class AdminController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public static DateTime utcNow = DateTime.UtcNow;

        // Define the timezone offset for UTC+08:00
        public static TimeSpan utcOffset = TimeSpan.FromHours(8); // UTC+08:00

        // Apply the timezone offset to get the local time in UTC+08:00
        Nullable<DateTime> PHTIME = utcNow + utcOffset;

        public AdminController(IMapper mapper, MailManager mailManager, IUserService userService, IHttpContextAccessor httpContextAccessor) : base(mailManager, httpContextAccessor)
        {
            _userService = userService;
            _mapper = mapper;
        }
        public IActionResult AdminDashboard(DateTime? startDate, DateTime? endDate)
        {
            // var tickets = _ticketRepo.GetAll().ToList();
            var tickets = _ticketRepo.GetAll()
                             .Where(t => (startDate == null || t.CreatedAt >= startDate) && (endDate == null || t.CreatedAt <= endDate))
                             .ToList();

            var statusCounts = tickets.GroupBy(t => t.Status.StatusName)
                                      .ToDictionary(g => g.Key, g => g.Count());

            var categoryCounts = tickets.GroupBy(t => t.Category.CategoryName)
                                      .ToDictionary(g => g.Key, g => g.Count());
            var priorityCounts = tickets.GroupBy(t => t.Priority).ToDictionary( g=> g.Key, g=> g.Count());
            var customers = _userRepo.Table.Where(u => u.RoleId == 1).Count();
            var agents = _userRepo.Table.Where(u => u.RoleId == 2).Count();
            var admins = _userRepo.Table.Where(u => u.RoleId == 4 || u.RoleId == 5).Count();
            var totalTickets = tickets.Count();
            var dashboardViewModel = new AdminDashboardViewModel
            {
                StatusCounts = statusCounts,
                TotalTickets = totalTickets,
                CustomerCount = customers,
                AgentCount = agents,
                AdminCount = admins,
                CategoryCounts = categoryCounts,
                PriorityCounts = priorityCounts
            };

            return View(dashboardViewModel);
        }

        [HttpGet]
        public IActionResult UpdateChart(DateTime startDate, DateTime endDate)
        {
            var tickets = _ticketRepo.GetAll()
                             .Where(t => (startDate == null || t.CreatedAt >= startDate) && (endDate == null || t.CreatedAt <= endDate))
                             .ToList();

            var statusCounts = tickets.GroupBy(t => t.Status.StatusName)
                                      .ToDictionary(g => g.Key, g => g.Count());
            
            return Json(statusCounts);
        }
        [HttpGet]
        public IActionResult UpdateCategoryChart(DateTime startDate, DateTime endDate)
        {
            var tickets = _ticketRepo.GetAll()
                             .Where(t => (startDate == null || t.CreatedAt >= startDate) && (endDate == null || t.CreatedAt <= endDate))
                             .ToList();

            var categoryCounts = tickets.GroupBy(t => t.Category.CategoryName)
                                      .ToDictionary(g => g.Key, g => g.Count());

            return Json(categoryCounts);
        }
        [HttpGet]
        public IActionResult UpdatePriorityChart(DateTime startDate, DateTime endDate)
        {
            var tickets = _ticketRepo.GetAll()
                             .Where(t => (startDate == null || t.CreatedAt >= startDate) && (endDate == null || t.CreatedAt <= endDate))
                             .ToList();

            var priorityCounts = tickets.GroupBy(t => t.Priority)
                                      .ToDictionary(g => g.Key, g => g.Count());

            return Json(priorityCounts);
        }
        // CUSTOMER SIDE

        public IActionResult Customers()
        {
            var modelView = new AdminViewModel();

            var customers = _userDetailRepo.Table.Where(m => m.Users.RoleId == 1).Include(m => m.Users).ToList();

            modelView.Customers = customers;

            ViewData["Title"] = "Customers page";
            return View(modelView);
        }

        [HttpPost]
        public IActionResult AddCustomer(string firstName, string lastName, string contactNo, string email, string username)
        {
            // trappings
            if (String.IsNullOrEmpty(firstName) || String.IsNullOrEmpty(lastName) || String.IsNullOrEmpty(contactNo)
                || String.IsNullOrEmpty(email) || String.IsNullOrEmpty(username)
                )
            {
                TempData["error"] = "Fill all the given fields!";
                return RedirectToAction("Customers", "Admin");
            }

            var exist = _userRepo.Table.Where(m => m.Email.Equals(email)).FirstOrDefault();
            if (exist != null)
            {
                TempData["error"] = "Email already taken!";
                return RedirectToAction("Agents", "Admin");
            }

            var newCustomer = new User()
            {
                Email = email,
                Username = username,
                Password = lastName + contactNo,
                RoleId = 1,
                CreatedAt = PHTIME
            };

            _userRepo.Create(newCustomer);

            // get ID of the added user to insert it in UserDetails' UserId
            var createdUser = _userRepo.Table.Where(m => m.Email == email).FirstOrDefault();

            var newUserDetail = new UserDetail()
            {
                ContactNo = contactNo,
                FirstName = firstName,
                LastName = lastName,
                UserId = createdUser.Id
            };
            _userDetailRepo.Create(newUserDetail);

            TempData["message"] = $"Successfully created user {firstName}! The user's password is {newCustomer.Password}.";

            string errResponse = "";

            bool emailSent = _mailManager.SendWelcomeEmail(newCustomer.Email, firstName, newCustomer.Username, newCustomer.Password, "Customer", ref errResponse);

            if (!emailSent)
            {
                // Handle failure
                TempData["error"] = "Failed to send email. " + errResponse;
            }
            else
            {
                TempData["message"] = "Welcome email sent successfully.";
            }

            return RedirectToAction("Customers", "Admin");
        }

        [HttpPost]
        public IActionResult DeleteCustomer(int CustomerId)
        {
            // trappings
            var customer = _userRepo.Table.Where(m => m.Id == CustomerId && m.RoleId == 1).FirstOrDefault();
            if (customer == null)
            {
                TempData["error"] = "Invalid customer ID";
                return RedirectToAction("Customers", "Admin");
            }

            _userRepo.Delete(CustomerId);

            TempData["message"] = $"Successfully delete customer {customer.Username}.";
            return RedirectToAction("Customers", "Admin");
        }

        [HttpPost]
        public IActionResult EditCustomer(string firstName, string lastName, string contactNo, string email, string username, string password, int CustomerId)
        {
            // trappings
            if (String.IsNullOrEmpty(firstName) || String.IsNullOrEmpty(lastName) || String.IsNullOrEmpty(contactNo)
                || String.IsNullOrEmpty(email) || String.IsNullOrEmpty(username) || String.IsNullOrEmpty(CustomerId.ToString())
                )
            {
                TempData["error"] = "Fill all the given fields!";
                return RedirectToAction("Customers", "Admin");
            }

            var customer = _userRepo.Table.Where(m => m.Id == CustomerId && m.RoleId == 1).FirstOrDefault();
            if (customer == null)
            {
                TempData["error"] = "Invalid customer ID";
                return RedirectToAction("Customers", "Admin");
            }

            customer.Email = email;
            customer.Username = username;
            customer.Password = password;
            _userRepo.Update(CustomerId, customer);

            var customerDetails = _userDetailRepo.Table.Where(m => m.UserId == CustomerId).FirstOrDefault();
            if (customerDetails == null)
            {
                TempData["error"] = "Invalid customer ID";
                return RedirectToAction("Customers", "Admin");
            }

            customerDetails.FirstName = firstName;
            customerDetails.LastName = lastName;
            customerDetails.ContactNo = contactNo;
            _userDetailRepo.Update(customerDetails.Id, customerDetails);


            TempData["message"] = $"Successfully updated user {username}!";
            return RedirectToAction("Customers", "Admin");
        }


        // END OF CUSTOMER SIDE


        // AGENT SIDE

        public IActionResult Agents()
        {
            var modelView = new AdminViewModel();

            var agents = _userDetailRepo.Table.Where(m => m.Users.RoleId == 2).Include(m => m.Users).Include(m => m.Users.Department).ToList();

            modelView.Agents = agents;

            ViewData["Title"] = "Agents page";
            return View(modelView);
        }

        [HttpPost]
        public IActionResult AddAgent(string firstName, string lastName, string contactNo, string email, string username, int department)
        {
            // trappings
            if (String.IsNullOrEmpty(firstName) || String.IsNullOrEmpty(lastName) || String.IsNullOrEmpty(contactNo)
                || String.IsNullOrEmpty(email) || String.IsNullOrEmpty(username))
            {
                TempData["error"] = "Fill all the given fields!";
                return RedirectToAction("Agents", "Admin");
            }

            var exist = _userRepo.Table.Where(m => m.Email.Equals(email)).FirstOrDefault();
            if (exist != null)
            {
                TempData["error"] = "Email already taken!";
                return RedirectToAction("Agents", "Admin");
            }

            var newAgent = new User()
            {
                Email = email,
                Username = username,
                Password = lastName + contactNo,
                RoleId = 2,
                DepartmentId = 7,
                CreatedAt = PHTIME
            };

            if (department != 0)
            {
                newAgent.DepartmentId = department;
            }

            _userRepo.Create(newAgent);

            // get ID of the added user to insert it in UserDetails' UserId
            var createdUser = _userRepo.Table.Where(m => m.Email == email).FirstOrDefault();

            var newUserDetail = new UserDetail()
            {
                ContactNo = contactNo,
                FirstName = firstName,
                LastName = lastName,
                UserId = createdUser.Id
            };
            _userDetailRepo.Create(newUserDetail);

            TempData["message"] = $"Successfully created user {firstName}! The user's password is {newAgent.Password}.";

            return RedirectToAction("Agents", "Admin");
        }

        [HttpPost]
        public IActionResult DeleteAgent(int AgentId)
        {
            // trappings
            var agent = _userRepo.Table.Where(m => m.Id == AgentId && m.RoleId == 2).FirstOrDefault();
            if (agent == null)
            {
                TempData["error"] = "Invalid agent ID";
                return RedirectToAction("Agents", "Admin");
            }

            _userRepo.Delete(AgentId);

            TempData["message"] = $"Successfully delete agent {agent.Username}.";
            return RedirectToAction("Agents", "Admin");
        }

        [HttpPost]
        public IActionResult EditAgent(string firstName, string lastName, string contactNo, string email, string username, string password, int AgentId, int department)
        {
            // trappings
            if (String.IsNullOrEmpty(firstName) || String.IsNullOrEmpty(lastName) || String.IsNullOrEmpty(contactNo) 
                || String.IsNullOrEmpty(email) || String.IsNullOrEmpty(username) || String.IsNullOrEmpty(AgentId.ToString())
                )
            {
                TempData["error"] = "Fill all the given fields!";
                return RedirectToAction("Agents", "Admin");
            }

            var agent = _userRepo.Table.Where(m => m.Id == AgentId).FirstOrDefault();
            if (agent == null)
            {
                TempData["error"] = "Invalid agent ID";
                return RedirectToAction("Agents", "Admin");
            }

            agent.Email = email;
            agent.Username = username;
            agent.Password = password;
            agent.DepartmentId = department;
            _userRepo.Update(AgentId, agent);

            var agentDetails = _userDetailRepo.Table.Where(m => m.UserId == AgentId).FirstOrDefault();
            if (agentDetails == null)
            {
                TempData["error"] = "Invalid agent ID";
                return RedirectToAction("Agents", "Admin");
            }

            agentDetails.FirstName = firstName;
            agentDetails.LastName = lastName;
            agentDetails.ContactNo = contactNo;
            _userDetailRepo.Update(agentDetails.Id, agentDetails);


            TempData["message"] = $"Successfully updated user {username}!";
            return RedirectToAction("Agents", "Admin");
        }

        [HttpPost]
        public IActionResult GenerateAgentReport(int agentId)
        {
            // Get the agent by ID
            var agent = _userRepo.Table.Where(m => m.Id == agentId && m.RoleId == 2).FirstOrDefault();
            var agentDetails = _userDetailRepo.Table.Where(m => m.Id == agentId).FirstOrDefault();
            if (agent == null)
            {
                return NotFound();
            }

            // Get the agent's tickets
            var tickets = _ticketAssignedRepo.Table.Where(m => m.AgentId == agentId).ToList();

            // Calculate the number of tickets resolved
            var ticketsResolved = tickets.Count(t => t.TicketId.HasValue && _ticketRepo.Get(t.TicketId.Value).StatusId == 5);

            //Calculate the average resolution timeTotalMinutes)
            var totalResolutionTime = tickets.Where(t => t.TicketId.HasValue && _ticketRepo.Get(t.TicketId.Value).StatusId == 5)
    .Sum(t =>
    {
        var ticket = _ticketRepo.Get(t.TicketId.Value);
        var resolutionTime = ticket.UpdatedAt.Value - ticket.CreatedAt.Value;
        return resolutionTime.TotalHours;
    });
            var averageResolutionTime = tickets.Count(t => t.TicketId.HasValue && _ticketRepo.Get(t.TicketId.Value).StatusId == 5) > 0 ? totalResolutionTime / tickets.Count(t => t.TicketId.HasValue && _ticketRepo.Get(t.TicketId.Value).StatusId == 5) : 0;
            var formattedAverageResolutionTime = averageResolutionTime.ToString("F1");
            
            // Calculate the customer satisfaction score
            var feedbacks = _feedbackRepo.Table.Where(m => m.AgentId == agentId).ToList();
            var totalSatisfactionScore = feedbacks.Sum(f => f.Star);
            var customerSatisfactionScore = feedbacks.Count > 0 ? totalSatisfactionScore / feedbacks.Count : 0; 

            // Generate the report
            var report = $"<ul>" +
                 $"<li>Agent Name: {agentDetails.FirstName} {agentDetails.LastName}</li>" +
                 $"<li>Tickets Resolved: {ticketsResolved}</li>" +
                 $"<li>Average Resolution Time: {formattedAverageResolutionTime} hours</li>" +
                 $"<li>Customer Satisfaction Score: {customerSatisfactionScore}/5</li>" +
                 "</ul>";

            var report2 = new
            {
                AgentName = $"{agentDetails.FirstName} {agentDetails.LastName}",
                TicketsResolved = ticketsResolved,
                AverageResolutionTime = formattedAverageResolutionTime,
                CustomerSatisfactionScore = customerSatisfactionScore
            };

            return Json(report2);
        }


        // END OF AGENT SIDE


        // ADMIN SIDE
        [Authorize(Roles = "4")]
        public IActionResult Admins()
        {
            var modelView = new AdminViewModel();

            var admins = _userDetailRepo.Table.Where(m => m.Users.RoleId == 3).Include(m => m.Users).ToList();

            modelView.Admins = admins;

            ViewData["Title"] = "Admins page";
            return View(modelView);
        }

        [HttpPost]
        public IActionResult AddAdmin(string firstName, string lastName, string contactNo, string email, string username, int department)
        {
            // Check if fields are filled
            if (String.IsNullOrEmpty(firstName) || String.IsNullOrEmpty(lastName) || String.IsNullOrEmpty(contactNo)
                || String.IsNullOrEmpty(email) || String.IsNullOrEmpty(username))
            {
                TempData["error"] = "Fill all the given fields!";
                return RedirectToAction("Admins", "Admin");
            }

            // Check if email already exists
            var exist = _userRepo.Table.Where(m => m.Email.Equals(email)).FirstOrDefault();
            if (exist != null)
            {
                TempData["error"] = "Email already taken!";
                return RedirectToAction("Admins", "Admin");
            }

            // Create new admin user
            var newAdmin = new User()
            {
                Email = email,
                Username = username,
                Password = lastName + contactNo,
                RoleId = 3, // Assuming RoleId = 1 is for Admins
                CreatedAt = PHTIME
            };

            if (department != 0)
            {
                newAdmin.DepartmentId = department;
            }

            _userRepo.Create(newAdmin);

            // Get ID of the created admin user to insert it in UserDetails
            var createdUser = _userRepo.Table.Where(m => m.Email == email).FirstOrDefault();

            // Add user details
            var newUserDetail = new UserDetail()
            {
                ContactNo = contactNo,
                FirstName = firstName,
                LastName = lastName,
                UserId = createdUser.Id
            };
            _userDetailRepo.Create(newUserDetail);

            // Notify success and show password
            TempData["message"] = $"Successfully created admin {firstName}! The admin's password is {newAdmin.Password}.";

            return RedirectToAction("Admins", "Admin");
        }

        [HttpPost]
        public IActionResult DeleteAdmin(int AdminId)
        {
            // Check if the admin exists
            var admin = _userRepo.Table.Where(m => m.Id == AdminId && m.RoleId == 3).FirstOrDefault();
            if (admin == null)
            {
                TempData["error"] = "Invalid admin ID";
                return RedirectToAction("Admins", "Admin");
            }

            _userRepo.Delete(AdminId);

            TempData["message"] = $"Successfully deleted admin {admin.Username}.";
            return RedirectToAction("Admins", "Admin");
        }

        [HttpPost]
        public IActionResult EditAdmin(string firstName, string lastName, string contactNo, string email, string username, string password, int AdminId)
        {
            // Check if fields are filled
            if (String.IsNullOrEmpty(firstName) || String.IsNullOrEmpty(lastName) || String.IsNullOrEmpty(contactNo)
                || String.IsNullOrEmpty(email) || String.IsNullOrEmpty(username) || String.IsNullOrEmpty(AdminId.ToString()))
            {
                TempData["error"] = "Fill all the given fields!";
                return RedirectToAction("Admins", "Admin");
            }

            // Find the admin
            var admin = _userRepo.Table.Where(m => m.Id == AdminId && m.RoleId == 3).FirstOrDefault();
            if (admin == null)
            {
                TempData["error"] = "Invalid admin ID";
                return RedirectToAction("Admins", "Admin");
            }

            // Update admin details
            admin.Email = email;
            admin.Username = username;
            admin.Password = password;
            _userRepo.Update(AdminId, admin);

            // Update admin's user details
            var adminDetails = _userDetailRepo.Table.Where(m => m.UserId == AdminId).FirstOrDefault();
            if (adminDetails == null)
            {
                TempData["error"] = "Invalid admin ID";
                return RedirectToAction("Admins", "Admin");
            }

            adminDetails.FirstName = firstName;
            adminDetails.LastName = lastName;
            adminDetails.ContactNo = contactNo;
            _userDetailRepo.Update(adminDetails.Id, adminDetails);

            TempData["message"] = $"Successfully updated admin {username}!";
            return RedirectToAction("Admins", "Admin");
        }

        [HttpPost]
        public IActionResult EditUserRoleCustomer(int userId, int newRoleId)
        {
            // Find the user by ID and ensure the user exists
            var user = _userRepo.Table.Where(m => m.Id == userId).FirstOrDefault();

            if (user == null)
            {
                TempData["error"] = "Invalid user ID.";
                return RedirectToAction("Customers", "Admin");
            }

            // Update the user's role

            if (user.RoleId != 2)
            {
                user.RoleId = newRoleId;
                user.DepartmentId = 7;
            }

            _userRepo.Update(user.Id, user);

            TempData["message"] = $"Successfully updated role for user {user.Username}.";

            return RedirectToAction("Customers", "Admin");
        }

        [HttpPost]
        public IActionResult EditUserRoleAgent(int userId, int newRoleId)
        {
            // Find the user by ID and ensure the user exists
            var user = _userRepo.Table.Where(m => m.Id == userId).FirstOrDefault();

            if (user == null)
            {
                TempData["error"] = "Invalid user ID.";
                return RedirectToAction("Agents", "Admin");
            }

            // Update the user's role
            user.RoleId = newRoleId;

            if (user.RoleId != 2)
            {
                user.DepartmentId = 7;
            }

            _userRepo.Update(user.Id, user);

            TempData["message"] = $"Successfully updated role for user {user.Username}.";

            return RedirectToAction("Agents", "Admin");
        }

        [HttpPost]
        public IActionResult EditUserRoleAdmin(int userId, int newRoleId)
        {
            // Find the user by ID and ensure the user exists
            var user = _userRepo.Table.Where(m => m.Id == userId).FirstOrDefault();

            if (user == null)
            {
                TempData["error"] = "Invalid user ID.";
                return RedirectToAction("Admins", "Admin");
            }

            // Update the user's role
            user.RoleId = newRoleId;

            if (user.RoleId != 2)
            {
                user.DepartmentId = 7;
            }

            _userRepo.Update(user.Id, user);

            TempData["message"] = $"Successfully updated role for user {user.Username}.";

            return RedirectToAction("Admins", "Admin");
        }

        // END OF ADMIN SIDE



        // BEGINNING OF TICKETS SIDE
        public IActionResult Tickets()
        {
            var model = new AdminViewModel();

            var tickets = _ticketRepo.GetAll();

            model.Tickets = _mapper.Map<List<TicketModel>>(tickets);
            ViewBag.Status = _statusRepo.GetAll();
            ViewBag.Agents = _userRepo.Table.Where(m => m.RoleId == 2).ToList();
            ViewBag.Customers = _userRepo.Table.Where(m => m.RoleId == 1).ToList();
            ViewBag.Categories = _categoryRepo.GetAll();
            return View(model);
        }

        [HttpPost]
        public IActionResult DeleteTicket(int ticketId)
        {
            // Check if the admin exists
            var ticket = _ticketRepo.Table.Where(m => m.Id == ticketId).FirstOrDefault();
            if (ticket == null)
            {
                TempData["error"] = "Invalid ticket ID";
                return RedirectToAction("Tickets", "Admin");
            }

            _ticketRepo.Delete(ticketId);

            TempData["message"] = $"Successfully deleted ticket #{ticket.Id}.";
            return RedirectToAction("Tickets", "Admin");
        }

        [HttpPost]
        public async Task<IActionResult> AddTicket(int category, string description, int customerId, int agentId, int status, string priority, IFormFile attachment)
        {
            // Check if fields are filled
            if (category == 0 || String.IsNullOrEmpty(description.Trim()) || customerId == 0 || agentId == 0 || status == 0 || String.IsNullOrEmpty(priority.Trim()))
            {
                TempData["error"] = "Fill all the given fields!";
                return RedirectToAction("Tickets", "Admin");
            }

            // Initialize the ticket
            var newTicket = new Ticket()
            {
                CategoryId = category,
                Description = description,
                UserId = customerId, // Assuming this is the customer
                StatusId = status,
                Priority = priority,
                CreatedAt = PHTIME,
                UpdatedAt = PHTIME
            };

            // Handle file attachment if provided
            string attachmentPath = "";
            if (attachment != null && attachment.Length > 0)
            {
                // Create the path for the Attachments folder inside wwwroot
                var attachmentsPicturesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Attachments");

                // Ensure the folder exists
                if (!Directory.Exists(attachmentsPicturesFolder))
                {
                    Directory.CreateDirectory(attachmentsPicturesFolder);
                }

                // Create a unique file name to avoid overwriting files with the same name
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + attachment.FileName;

                // Full file path
                string filePath = Path.Combine(attachmentsPicturesFolder, uniqueFileName);

                // Copy the file to the target location
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await attachment.CopyToAsync(fileStream);
                }

                // Save the relative path to the file (relative to wwwroot)
                attachmentPath = Path.Combine("Attachments", uniqueFileName).Replace("\\", "/");
            }

            newTicket.Attachments = attachmentPath;
            _ticketRepo.Create(newTicket);

            // Assign the ticket to an agent if specified
            if (agentId != 0)
            {
                var createdTicket = _ticketRepo.Table.Where(m => m.Description.Equals(newTicket.Description) && m.CategoryId == newTicket.CategoryId && m.UserId == newTicket.UserId).FirstOrDefault();

                var newAssignment = new TicketAssigned
                {
                    TicketId = createdTicket.Id,
                    AgentId = agentId,
                    Status = "APPROVED"
                };
                _ticketAssignedRepo.Create(newAssignment);
            }

            // Notify success
            TempData["message"] = $"Successfully created a new ticket with ID {newTicket.Id}.";

            return RedirectToAction("Tickets", "Admin");
        }


        [HttpPost]
        public async Task<IActionResult> EditTicket(int ticketId, string description, string priority, string status, string category, IFormFile attachment)
        {
            // Check if fields are filled
            if (ticketId == 0 || String.IsNullOrEmpty(description) || String.IsNullOrEmpty(priority)
                || String.IsNullOrEmpty(status) || String.IsNullOrEmpty(category))
            {
                TempData["error"] = "Fill all the given fields!";
                return RedirectToAction("Tickets", "Admin");
            }

            // Find the ticket
            var ticket = _ticketRepo.Table.Where(t => t.Id == ticketId).FirstOrDefault();
            if (ticket == null)
            {
                TempData["error"] = "Invalid ticket ID";
                return RedirectToAction("Tickets", "Admin");
            }

            string attachmentPath = "";
            if (attachment != null && attachment.Length > 0)
            {
                // Create the path for the Attachments folder inside wwwroot
                var attachmentsPicturesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Attachments");

                // Ensure the folder exists
                if (!Directory.Exists(attachmentsPicturesFolder))
                {
                    Directory.CreateDirectory(attachmentsPicturesFolder);
                }

                // Create a unique file name to avoid overwriting files with the same name
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + attachment.FileName;

                // Full file path
                string filePath = Path.Combine(attachmentsPicturesFolder, uniqueFileName);

                // Copy the file to the target location
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await attachment.CopyToAsync(fileStream);
                }

                // Save the relative path to the file (relative to wwwroot)
                attachmentPath = Path.Combine("Attachments", uniqueFileName).Replace("\\", "/");
            }

            // Update ticket details
            ticket.Description = description;
            ticket.Priority = priority;
            ticket.Attachments = attachmentPath;

            // Update ticket category if it's changed
            var ticketCategory = _categoryRepo.Table.Where(c => c.CategoryName == category).FirstOrDefault();
            if (ticketCategory != null)
            {
                ticket.CategoryId = ticketCategory.Id;
            }
            else
            {
                TempData["error"] = "Invalid category";
                return RedirectToAction("Tickets", "Admin");
            }

            // Update ticket status if it's changed
            var ticketStatus = _statusRepo.Table.Where(s => s.StatusName == status).FirstOrDefault();
            if (ticketStatus != null)
            {
                ticket.StatusId = ticketStatus.Id;
            }
            else
            {
                TempData["error"] = "Invalid status";
                return RedirectToAction("Tickets", "Admin");
            }

            // Update the ticket in the repository
            _ticketRepo.Update(ticket.Id, ticket);

            TempData["message"] = $"Successfully updated ticket {ticket.Id}!";
            return RedirectToAction("Tickets", "Admin");
        }


        public IActionResult TicketsAssignment()
        {
            var model = new AdminViewModel();

            var ticketsAssigneds = _ticketAssignedRepo.Table.Where(m => m.Status.Equals("PENDING") && m.Ticket.StatusId != 6).Include(m => m.Ticket.User.Department).ToList();
            var agents = _userDetailRepo.Table.Where(m => m.Users.RoleId == 2).Include(m => m.Users).Include(m => m.Users.Department).ToList();

            model.Agents = agents;
            model.TicketsAssigneds = ticketsAssigneds;

            return View(model);
        }

        [HttpPost]
        public IActionResult ApproveTicket(int ticketId)
        {
            var ticket = _ticketRepo.Get(ticketId);

            if (ticket == null)
            {
                TempData["error"] = InvalidIDError;
                return RedirectToAction("TicketsAssignment", "Admin");
            }

            var ticketAssigned = _ticketAssignedRepo.Table.Where(m => m.TicketId == ticket.Id).FirstOrDefault();
            ticketAssigned.Status = "APPROVED";
            _ticketAssignedRepo.Update(ticketAssigned.Id, ticketAssigned);

            string errResponse = "";

            _mailManager.AssignedToAgentEmail(ticketAssigned.Agent.Email, ticketAssigned.Agent.UserDetails.FirstOrDefault().FirstName, ticket.Description, ref errResponse);

            TempData["message"] = SuccessApproveTicket;
            return RedirectToAction("TicketsAssignment", "Admin");
        }

        [HttpPost]
        public IActionResult DisapproveTicket(int ticketId, int agentId)
        {
            var ticket = _ticketRepo.Get(ticketId);
            var agent = _userRepo.Get(agentId);

            if(ticket == null || agent.RoleId != 2)
            {
                TempData["error"] = InvalidIDError;
                return RedirectToAction("TicketsAssignment", "Admin");
            }

            var ticketAssigned = _ticketAssignedRepo.Table.Where(m => m.TicketId == ticket.Id).FirstOrDefault();

            ticketAssigned.AgentId = agentId;
            ticketAssigned.Status = "APPROVED";
            _ticketAssignedRepo.Update(ticketAssigned.Id, ticketAssigned);

            string errResponse = "";

            _mailManager.AssignedToAgentEmail(ticketAssigned.Agent.Email, ticketAssigned.Agent.UserDetails.FirstOrDefault().FirstName, ticket.Description, ref errResponse);


            TempData["message"] = SuccessReassign + " agent " + agent.UserDetails.FirstOrDefault().LastName;
            return RedirectToAction("TicketsAssignment", "Admin");
        }


        // BEGINNING OF KNOWLEDGEBASE SIDE
        public IActionResult Articles()
        {
            var modelView = new AdminViewModel();

            var articles = _articleRepo.Table.ToList();

            modelView.Articles = articles;

            ViewData["Title"] = "Articles page";
            return View(modelView);
        }

        [HttpPost]
        public IActionResult DeleteArticle(int ArticleId)
        {
            // trappings
            var article = _articleRepo.Table.Where(m => m.Id == ArticleId).FirstOrDefault();
            if (article == null)
            {
                TempData["error"] = "Invalid article ID";
                return RedirectToAction("Articles", "Admin");
            }

            _articleRepo.Delete(ArticleId);

            TempData["message"] = $"Successfully delete Article {article.Title}.";
            return RedirectToAction("Articles", "Admin");
        }

        [HttpGet]
        public IActionResult AddArticle()
        {
            // Initialize the model for the form
            var model = new ArticleViewModel();
            model.Categories = _categoryRepo.GetAll().ToList();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateArticle(ArticleViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Validate that all required fields are filled
                if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Content))
                {
                    ModelState.AddModelError("", "Please fill in all required fields.");
                    return View(model);
                }
                string attachmentPath = "";
                if (model.Attachment != null && model.Attachment.Length > 0)
                {
                    // Create the path for the Attachments folder inside wwwroot
                    var attachmentsPicturesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ArticleAttachments");

                    if (!Directory.Exists(attachmentsPicturesFolder))
                    {
                        Directory.CreateDirectory(attachmentsPicturesFolder);
                    }
                    
                    // Create a unique file name to avoid overwriting files with the same name
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.Attachment.FileName;

                    // Full file path
                    string filePath = Path.Combine(attachmentsPicturesFolder, uniqueFileName);

                    // Copy the file to the target location
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Attachment.CopyToAsync(fileStream);
                    }

                    // Save the relative path to the file (relative to wwwroot)
                    attachmentPath = Path.Combine("ArticleAttachments", uniqueFileName).Replace("\\", "/");
                }

                // Save the article to the database
                var currentUser = HttpContext.User.Identity.Name;
                var article = new Article
                {
                    Title = model.Title,
                    Content = model.Content,
                    CategoryId = model.CategoryId,
                    Status = "Draft", // Set the initial status to "Draft"
                    Author = currentUser, // Set the user or author who created the article
                    Attachments = attachmentPath,
                    PublishDate = PHTIME, // Set the publish date to null initially
                    LastmodifiedDate = PHTIME // Set the modified date to the current date and time
                };

                _articleRepo.Create(article);
                TempData["message"] = "Article created successfully.";
                return RedirectToAction("Articles", "Admin");
            }

            return View(model);
        }
        [HttpGet]
        public IActionResult EditArticle(int ArticleId)
        {
            var article = _articleRepo.Table.Where(m => m.Id == ArticleId).FirstOrDefault();
            

            var model = new ArticleViewModel()
            {
                Id = article.Id,
                Title = article.Title,
                Content = article.Content,
                CategoryId = article.CategoryId,
                Status = article.Status,
                Author = article.Author,
                LastModifiedDate = PHTIME.Value,
                AttachmentPath = article.Attachments
            };

            model.Categories = _categoryRepo.GetAll().ToList();

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> EditArticle(ArticleViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Validate that all required fields are filled
                if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Content))
                {
                    ModelState.AddModelError("", "Please fill in all required fields.");
                    return View(model);
                }

                var article = _articleRepo.FindByCondition(a => a.Id == model.Id).FirstOrDefault();
                if (article == null)
                {
                    return NotFound();
                }

                string attachmentPath = "";
                if (model.Attachment != null && model.Attachment.Length > 0)
                {
                    // Create the path for the Attachments folder inside wwwroot
                    var attachmentsPicturesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ArticleAttachments");

                    if (!Directory.Exists(attachmentsPicturesFolder))
                    {
                        Directory.CreateDirectory(attachmentsPicturesFolder);
                    }

                    // Create a unique file name to avoid overwriting files with the same name
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.Attachment.FileName;

                    // Full file path
                    string filePath = Path.Combine(attachmentsPicturesFolder, uniqueFileName);

                    // Copy the file to the target location
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Attachment.CopyToAsync(fileStream);
                    }

                    // Save the relative path to the file (relative to wwwroot)
                    attachmentPath = Path.Combine("ArticleAttachments", uniqueFileName).Replace("\\", "/");
                }

                article.Title = model.Title;
                article.Content = model.Content;
                article.CategoryId = model.CategoryId;
                article.Status = model.Status;
                article.LastmodifiedDate = PHTIME;
                if((model.Attachment != null && model.Attachment.Length > 0) && attachmentPath != "") {
                    article.Attachments = attachmentPath;
                }

                _articleRepo.Update(model.Id, article);

                TempData["message"] = "Article updated successfully.";
                return RedirectToAction("Articles", "Admin");
            }

            model.Categories = _categoryRepo.GetAll().ToList();
            return View(model);
        }

        [HttpGet]
        public IActionResult ViewArticle(int ArticleId)
        {
            var article = _articleRepo.Table.Where(m => m.Id == ArticleId).FirstOrDefault();


            var model = new ArticleViewModel()
            {
                Id = article.Id,
                Title = article.Title,
                Content = article.Content,
                CategoryId = article.CategoryId,
                Category = article.Category,
                Status = article.Status,
                Author = article.Author,
                PublishDate = article.PublishDate,
                AttachmentPath = article.Attachments,
                LastModifiedDate = PHTIME.Value
            };

            model.Categories = _categoryRepo.GetAll().ToList();

            return View(model);
        }
        // END OF KNOWLEDGEBASE SIDE
    }
}

        
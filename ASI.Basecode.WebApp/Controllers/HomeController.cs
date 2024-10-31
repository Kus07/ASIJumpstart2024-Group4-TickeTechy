using ASI.Basecode.Resources.Messages;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.Manager;
using ASI.Basecode.Services.ServiceModels;
using ASI.Basecode.Services.Services;
using ASI.Basecode.WebApp.Mvc;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using ASI.Basecode.Data.Models;
using static ASI.Basecode.Resources.Constants.Enums;

namespace ASI.Basecode.WebApp.Controllers
{
    /// <summary>
    /// Home Controller
    /// </summary>
    public class HomeController : BaseController
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mailManager"></param>
        /// <param name="userService"></param>
       
        private readonly IUserService _userService;

        public HomeController(MailManager mailManager, IUserService userService, IHttpContextAccessor httpContextAccessor) : base(mailManager, httpContextAccessor)
        {
            _userService = userService;
        }


        [Authorize(Roles = "1")]
        public IActionResult CustomerDashboard()
        {

            var articles = _db.Articles
                .Include(a => a.UserDetail)
                .Include(a => a.Category)
                .ToList();

            if (articles == null || !articles.Any())
            {
                return RedirectToAction("Error");
            }

            var model = articles.Select(article => new ArticleViewModel
            {
                Id = article.Id,
                Title = article.Title,
                Content = article.Content,
                CategoryId = article.CategoryId,
                Category = article.Category,
                Status = article.Status,
                Author = article.Author,
                PublishDate = article.PublishDate,
                LastModifiedDate = article.LastmodifiedDate ?? DateTime.Now,
                ProfilePicturePath = article.UserDetail?.ProfilePicturePath,
                UserDetail = article.UserDetail
            }).ToList();

            var viewModel = new TicketViewModel()
            {
                UserId = GetUserId(),
                Categories = _categoryRepo.GetAll().ToList(),
                Articles = model
            };

            if (UnreadNotifications())
            {
                TempData["notifications"] = "true";
            }

            return View(viewModel);
        }


        [Authorize(Roles = "2")]
        public IActionResult AgentDashboard()
        {
            var tickets = _ticketAssignedRepo.Table.Where(m => m.AgentId == GetUserId() || m.ReassignedToId == GetUserId() && m.Status.Equals("APPROVED")).Include(m => m.Ticket).Include(m => m.Ticket.User).ToList();

            if (UnreadNotifications())
            {
                TempData["notifications"] = "true";
            }

            return View(tickets);
        }

        [Authorize(Roles = "1")]
        public IActionResult Tickets()
        {
            var userId = GetUserId();

            // Check if userId is valid
            if (userId <= 0)
            {
                // Handle invalid user, maybe redirect or return an error
                return RedirectToAction("Error", new { message = "Invalid user." });
            }

            var tickets = _ticketRepo.GetAll()
                                     .Where(t => t.UserId == userId)
                                     .ToList();

            // Check if any tickets exist
            if (tickets == null || !tickets.Any())
            {
                ViewBag.Message = "No tickets found.";
                return View(new List<TicketViewModel>());
            }

            var categories = _categoryRepo.GetAll().ToList();

            // Create a list of TicketViewModel with the category names included
            var viewModel = tickets.Select(ticket => new TicketViewModel
            {
                Id = ticket.Id,
                UserId = (int)ticket.UserId,
                CategoryId = (int)ticket.CategoryId,
                Description = ticket.Description,
                Priority = ticket.Priority,
                AttachmentPath = ticket.Attachments,
                Status = ticket.Status.StatusName,
                CreatedAt = (DateTime)ticket.CreatedAt,
                CategoryName = categories.FirstOrDefault(c => c.Id == ticket.CategoryId)?.CategoryName
            }).ToList();


            var statusCounts = tickets.GroupBy(t => t.Status.StatusName)
                              .Select(group => new {
                                  Status = group.Key,
                                  Count = group.Count()
                              }).ToDictionary(g => g.Status, g => g.Count);
            ViewBag.StatusCounts = statusCounts;
            var categoryCounts = tickets.GroupBy(t => t.Category.CategoryName)
                              .Select(group => new {
                                  Status = group.Key,
                                  Count = group.Count()
                              }).ToDictionary(g => g.Status, g => g.Count);
            ViewBag.CategoryCounts = categoryCounts;

            if (UnreadNotifications())
            {
                TempData["notifications"] = "true";
            }

            return View(viewModel);
        }


        [Authorize(Roles = "1,2")]
        public IActionResult Profile()
        {
            var currentUser = _userDetailRepo.Table
                .Where(m => m.UserId == GetUserId())
                .Include(m => m.Users)
                .Include(m => m.Users.Role)
                .FirstOrDefault();

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (currentUser.Users.RoleId == 1) 
            {
                var tickets = _ticketRepo.Table.Where(m => m.UserId == currentUser.Id).ToList();
                ViewBag.TicketCount = tickets.Count;
            }
            else 
            {
                var tickets = _ticketAssignedRepo.Table
                    .Where(m => m.AgentId == currentUser.UserId && m.Ticket.StatusId == Convert.ToInt32(TicketStatus.CLOSED))
                    .ToList();

                ViewBag.TicketCount = tickets.Count;

                // Retrieve feedbacks for the current agent
                var feedbacks = _feedbackRepo.Table
                    .Where(f => f.AgentId == currentUser.UserId)
                    .Include(f => f.User)
                    .Include(f => f.Ticket)
                    .ToList();

                var averageStarRating = feedbacks.Any() ? feedbacks.Average(f => f.Star) : 0;

                ViewBag.FeedbackCount = feedbacks.Count;
                ViewBag.Feedbacks = feedbacks; 
                ViewBag.AverageStarRating = averageStarRating;
            }

            if (UnreadNotifications())
            {
                TempData["notifications"] = "true";
            }

            return View(currentUser);
        }



        public IActionResult EditProfile()
        {
            var currentUser = _userDetailRepo.Table.Where(m => m.Id == GetUserId()).Include(m => m.Users).Include(m => m.Users.Role).FirstOrDefault();

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (UnreadNotifications())
            {
                TempData["notifications"] = "true";
            }

            return View(currentUser);
        }


        [HttpPost]
        public IActionResult EditProfile(string lastName, string firstName, string contactNo, string email, IFormFile ProfilePicture)
        {
            // Check if a profile picture file is uploaded
            string profilePicturePath = null;

            if (ProfilePicture != null && ProfilePicture.Length > 0)
            {
                // Define the folder to save the profile pictures
                var profilePicturesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ProfilePictures");

                // Create the directory if it doesn't exist
                if (!Directory.Exists(profilePicturesFolder))
                {
                    Directory.CreateDirectory(profilePicturesFolder);
                }

                // Get the file extension of the uploaded picture
                var fileExtension = Path.GetExtension(ProfilePicture.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

                // Create the full path where the picture will be saved
                var filePath = Path.Combine(profilePicturesFolder, uniqueFileName);

                // Save the file to the directory
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    ProfilePicture.CopyTo(stream);
                }

                // Set the relative path to the saved profile picture
                profilePicturePath = $"/ProfilePictures/{uniqueFileName}";
            }

            // Assuming you have a user model to update
            var user = _userRepo.Table.Where(u => u.Email == email).FirstOrDefault(); // Fetch the user by email or any other unique identifier
            if (user != null)
            {
                var userDetails = _userDetailRepo.Table.Where(m => m.UserId == user.Id).FirstOrDefault();

                // Update user details
                userDetails.LastName = lastName;
                userDetails.FirstName = firstName;
                userDetails.ContactNo = contactNo;
                user.Email = email;

                // Update profile picture path if a picture was uploaded
                if (!string.IsNullOrEmpty(profilePicturePath))
                {
                    userDetails.ProfilePicturePath = profilePicturePath;
                }

                // Save changes to the database
                _userRepo.Update(user.Id, user);
                _userDetailRepo.Update(userDetails.Id, userDetails);
            }

            // Redirect to the profile page after successful save
            TempData["message"] = "Successfully edited your profile!";
            return RedirectToAction("Profile", "Home");
        }

        [HttpPost]
        public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmNewPassword)
        {
            // trappings
            if (newPassword != confirmNewPassword)
            {
                ModelState.AddModelError(string.Empty, "New password and confirm password do not match.");
                return View();
            }

            var userId = GetUserId();
            var user = _userRepo.Table.Where(m => m.Id == userId).FirstOrDefault();

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            if (user.Password != currentPassword)
            {
                TempData["error"] = "Current password incorrect.";
                return RedirectToAction("Profile", "Home");
            }

            user.Password = newPassword;

            _userRepo.Update(user.Id, user);

            TempData["message"] = "Successfully changed password!";
            return RedirectToAction("Profile", "Home");
        }

        public IActionResult ContactUs()
        {
            if (UnreadNotifications())
            {
                TempData["notifications"] = "true";
            }
            return View();
        }


        public int GetUserId()
        {
            var currentUser = _userRepo.Table.Where(m => m.Username.Equals(User.Identity.Name)).FirstOrDefault();

            return currentUser.Id;
        }


        [HttpGet]
        public IActionResult Notification()
        {
            int currentUserId = GetUserId();


            var notifications = _notificationRepo.Table
                .Where(n => n.ToUserId == currentUserId)
                .OrderByDescending(n => n.DateCreated)
                .Select(n => new Notification 
                {
                    Id = n.Id,
                    FromUserId = n.FromUserId,
                    ToUserId = n.ToUserId,
                    Title = n.Title,
                    Content = n.Content,
                    DateCreated = n.DateCreated,
                    Status = n.Status,
                    TicketId = n.TicketId 
                })
                .ToList();


            if (notifications == null || !notifications.Any())
            {
                notifications = new List<Notification>();
            }

            if (UnreadNotifications())
            {
                TempData["notifications"] = "true";
            }

            return View(notifications);
        }



        [HttpPost]
        public IActionResult MarkAsRead()
        {
            int currentUserId = GetUserId(); 

            var notifications = _notificationRepo.Table
                .Where(n => n.ToUserId == currentUserId && n.Status == "UNREAD") 
                .ToList();

            foreach (var notification in notifications)
            {
                notification.Status = "READ";
                _notificationRepo.Update(notification.Id, notification); 
            }

            return RedirectToAction("Notification");
        }


        [HttpGet]
        public IActionResult ViewNotification(int ticketId, int notificationId)
        {
            var notification = _notificationRepo.Get(notificationId);

            if (notification != null)
            {
                notification.Status = "READ";
                _notificationRepo.Update(notification.Id, notification);
                _db.SaveChanges(); 
            }

            return RedirectToAction("View", "Ticket", new { id = ticketId });
        }


        public bool UnreadNotifications()
        {
            int currentUserId = GetUserId();
            bool unreadNotificationsAvailable = false;

            var user = _userRepo.Get(currentUserId);
            var notifications = _notificationRepo.Table.Where(m => m.ToUserId == user.Id && m.Status.Equals("UNREAD")).ToList();

            if(notifications.Count > 0)
            {
                unreadNotificationsAvailable = true;                
            }

            return unreadNotificationsAvailable;

        }
    }
}

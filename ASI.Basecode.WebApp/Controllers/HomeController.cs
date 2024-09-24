using ASI.Basecode.Resources.Messages;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.Manager;
using ASI.Basecode.Services.ServiceModels;
using ASI.Basecode.Services.Services;
using ASI.Basecode.WebApp.Mvc;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System;
using System.Data.Entity;

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
            var viewModel = new TicketViewModel()
            {
                UserId = GetUserId(),
                Categories = _categoryRepo.GetAll().ToList()
            };

            return View(viewModel);
        }

        [Authorize(Roles = "2")]
        public IActionResult AgentDashboard()
        {
            var tickets = _ticketAssignedRepo.Table.Where(m => m.AgentId == GetUserId()).Include(m => m.Ticket).Include(m => m.Ticket.User).ToList();
            return View(tickets);
        }

        [Authorize(Roles = "1")]
        public IActionResult Tickets()
        {
            var userId = GetUserId();
            var tickets = _ticketRepo.GetAll().Where(t => t.UserId == userId).ToList();
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
                Status = ticket.Status,
                CreatedAt = (DateTime)ticket.CreatedAt,
                CategoryName = categories.FirstOrDefault(c => c.Id == ticket.CategoryId)?.CategoryName
            }).ToList();


            return View(viewModel);
        }

        [Authorize(Roles = "1,2")]
        public IActionResult Profile()
        {
            var currentUser = _userDetailRepo.Table.Where(m => m.UserId == GetUserId()).Include(m => m.Users).Include(m => m.Users.Role).FirstOrDefault();

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
                var tickets = _ticketAssignedRepo.Table.Where(m => m.AgentId == currentUser.Id).ToList();

                ViewBag.TicketCount = tickets.Count;
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


        [Authorize(Roles = "2")]
        public IActionResult SupportDashboard()
        {
            return View();
        }

        public IActionResult ContactUs()
        {
            return View();
        }


        public int GetUserId()
        {
            var currentUser = _userRepo.Table.Where(m => m.Username.Equals(User.Identity.Name)).FirstOrDefault();

            return currentUser.Id;
        }

    }
}

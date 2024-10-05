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

namespace ASI.Basecode.WebApp.Controllers
{
    [Authorize(Roles = "3,4")]
    public class AdminController : BaseController
    {
        private readonly IUserService _userService;

        public AdminController(MailManager mailManager, IUserService userService, IHttpContextAccessor httpContextAccessor) : base(mailManager, httpContextAccessor)
        {
            _userService = userService;
        }


        public IActionResult AdminDashboard()
        {
            return View();
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
            if (String.IsNullOrEmpty(firstName.Trim()) || String.IsNullOrEmpty(lastName.Trim()) || String.IsNullOrEmpty(contactNo.Trim())
                || String.IsNullOrEmpty(email.Trim()) || String.IsNullOrEmpty(username.Trim())
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
                CreatedAt = DateTime.Now
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

            var customer = _userRepo.Table.Where(m => m.Id == CustomerId && m.RoleId == 2).FirstOrDefault();
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
            if (String.IsNullOrEmpty(firstName.Trim()) || String.IsNullOrEmpty(lastName.Trim()) || String.IsNullOrEmpty(contactNo.Trim())
                || String.IsNullOrEmpty(email.Trim()) || String.IsNullOrEmpty(username.Trim())
                )
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
                CreatedAt = DateTime.Now
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
            if (String.IsNullOrEmpty(firstName.Trim()) || String.IsNullOrEmpty(lastName.Trim()) || String.IsNullOrEmpty(contactNo.Trim())
                || String.IsNullOrEmpty(email.Trim()) || String.IsNullOrEmpty(username.Trim()))
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
                CreatedAt = DateTime.Now
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
            user.RoleId = newRoleId;

            if (user.RoleId != 2)
            {
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
    }
}

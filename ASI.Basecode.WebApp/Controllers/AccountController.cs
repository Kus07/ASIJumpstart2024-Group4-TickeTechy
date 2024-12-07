using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.Manager;
using ASI.Basecode.Services.ServiceModels;
using ASI.Basecode.Services.Services;
using ASI.Basecode.WebApp.Authentication;
using ASI.Basecode.WebApp.Models;
using ASI.Basecode.WebApp.Mvc;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static ASI.Basecode.Resources.Constants.Enums;

namespace ASI.Basecode.WebApp.Controllers
{
    public class AccountController : BaseController
    {
        private readonly SessionManager _sessionManager;
        private readonly SignInManager _signInManager;
        private readonly TokenValidationParametersFactory _tokenValidationParametersFactory;
        private readonly TokenProviderOptionsFactory _tokenProviderOptionsFactory;
        private readonly IConfiguration _appConfiguration;
        private readonly IUserService _userService;
        private readonly new MailManager _mailManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountController"/> class.
        /// </summary>
        /// <param name="signInManager">The sign in manager.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="userService">The user service.</param>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="tokenValidationParametersFactory">The token validation parameters factory.</param>
        /// <param name="tokenProviderOptionsFactory">The token provider options factory.</param>
        /// <param name="mailManager">The mail manager.</param>
        /// 
        public AccountController(MailManager mailManager, IUserService userService,
                                SignInManager signInManager,
                            IHttpContextAccessor httpContextAccessor,
                            ILoggerFactory loggerFactory,
                            IConfiguration configuration,
                            TokenValidationParametersFactory tokenValidationParametersFactory,
                            TokenProviderOptionsFactory tokenProviderOptionsFactory
            ) : base(mailManager, httpContextAccessor)
        {
            _mailManager = mailManager;
            _userService = userService;
            _signInManager = signInManager;
            this._tokenProviderOptionsFactory = tokenProviderOptionsFactory;
            this._tokenValidationParametersFactory = tokenValidationParametersFactory;
            this._appConfiguration = configuration; 
            this._sessionManager = new SessionManager(this._session);
        }

        /// <summary>
        /// Login Method
        /// </summary>
        /// <returns>Created response view</returns>
        /// 
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            ViewData["title"] = "TickeTechy Login";
            if (User.Identity.IsAuthenticated)
            {
                var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                // Redirect based on role
                switch (role)
                {
                    case "1":
                        return RedirectToAction("CustomerDashboard", "Home");
                    case "2":
                        return RedirectToAction("AgentDashboard", "Home");
                    case "3":
                        return RedirectToAction("AdminDashboard", "Admin");
                    case "4":
                        return RedirectToAction("AdminDashboard", "Admin");
                    default:
                        return RedirectToAction("Error", "Shared");
                }
            }
            return View();
        }

        /// <summary>
        /// Authenticate user and signs the user in when successful.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns> Created response view </returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(Services.ServiceModels.LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _userRepo.Table.Where(u => u.Email == model.Email && u.Password == model.Password).FirstOrDefault();

                if (user != null)
                {
                    var userDetails = _userDetailRepo.Table.Include(m => m.Users).Where(m => m.UserId == user.Id).FirstOrDefault();
                    //await _userService.AuthenticateUser(user);
                    await this._signInManager.SignInAsync(userDetails);

                    // Redirect based on role
                    switch (user.RoleId)
                    {
                        case 1:
                            return RedirectToAction("CustomerDashboard", "Home");
                        case 2:
                            return RedirectToAction("AgentDashboard", "Home");
                        case 3:
                            return RedirectToAction("AdminDashboard", "Admin");
                        case 4:
                            return RedirectToAction("AdminDashboard", "Admin");
                        default:
                            return RedirectToAction("Error", "Shared");
                    }
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }


        /// <summary>
        /// Register Method
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }


        /// <summary>
        /// Register Method
        /// </summary>
        [HttpPost]
        [AllowAnonymous]

        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if the email is already registered
                var existingUser = _userRepo.GetAll().FirstOrDefault(u => u.Email == model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError(string.Empty, "Email is already registered.");
                    return View(model);
                }

                // Create a new user object based on the input data from the form
                var newUser = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    Password = model.Password,
                    RoleId = 1,
                    ArticleViewSetting = 1
                };

                // Add the new user to the database
                _userRepo.Create(newUser);

                // get ID of the added user to insert it in UserDetails' UserId
                var createdUser = _userRepo.Table.Where(m => m.Email == model.Email).FirstOrDefault();

                var userDetails = new UserDetail()
                {
                    ContactNo = model.Contact,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    UserId = createdUser.Id
                };

                _userDetailRepo.Create(userDetails);


                // After registration, automatically log in the user
                await _userService.AuthenticateUser(newUser);

                TempData["success"] = "Successfully registered! You may log in now.";

                string errResponse = "";

                bool emailSent = _mailManager.SendWelcomeEmail(createdUser.Email, model.FirstName, createdUser.Username, createdUser.Password, "Customer", ref errResponse);

                if (!emailSent)
                {
                    // Handle failure
                    //TempData["error"] = "Failed to send email. " + errResponse;
                }
                else
                {
                    TempData["message"] = "Welcome to TickeTechy!";
                }

                // Redirect based on role after registration
                switch (newUser.RoleId)
                {
                    case 1:
                        return RedirectToAction("CustomerDashboard", "Home");
                    case 2:
                        return RedirectToAction("AgentDashboard", "Home");
                    case 3:
                        return RedirectToAction("AdminDashboard", "Admin");
                    case 4:
                        return RedirectToAction("AdminDashboard", "Admin");
                    default:
                        return RedirectToAction("Error", "Shared");
                }
            }

            // If we got this far, something failed; re-display the form
            return View(model);
        }

        /// <summary>
        /// Sign Out current account and return login view.
        /// </summary>
        /// <returns>Created response view</returns>
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            await this._signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }


        [AllowAnonymous]
        [HttpPost]
        public IActionResult SendOTP(string email)
        {

            if (String.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Invalid email format." });
            }

            string emailPattern = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";
            var isValid = Regex.IsMatch(email, emailPattern);

            if (!isValid)
            {
                return Json(new { success = false, message = "Invalid email format." });
            }

            var user = _userRepo.Table.Where(m => m.Email == email).FirstOrDefault();
            if (user == null)
            {
                return Json(new { success = false, message = "Email does not exist in TickeTechy!" });
            }

            var userDetails = _userDetailRepo.Table.Where(m => m.UserId == user.Id).FirstOrDefault();
            string otpCode = GenerateOTP();
            string errResponse = "";

            bool otpSent = _mailManager.SendOtpForgotPassword(email, userDetails.FirstName, otpCode, ref errResponse);

            if (otpSent)
            {
                user.ForgotPassOtp = otpCode;
                _userRepo.Update(user.Id, user);
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false });
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult VerifyOTP(string otp, string email)
        {
            // Logic to verify the OTP
            var user = _userRepo.Table.Where(m => m.Email.Equals(email)).FirstOrDefault();

            if (user == null || String.IsNullOrEmpty(user.ForgotPassOtp))
            {
                return Json(new { success = false });
            }

            if (!user.ForgotPassOtp.Equals(otp))
            {
                return Json(new { success = false });
            }
            else
            {
                _userRepo.Update(user.Id, user);
                return Json(new { success = true });
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult ResetPassword(string newPassword, string otpCode, string email)
        {
            var user = _userRepo.Table.Where(m => m.Email.Equals(email)).FirstOrDefault();
            if (user == null || String.IsNullOrEmpty(user.ForgotPassOtp))
            {
                return Json(new { success = false });
            }

            if (!user.ForgotPassOtp.Equals(otpCode))
            {
                return Json(new { success = false });
            }

            user.Password = newPassword;
            _userRepo.Update(user.Id, user);
            return Json(new { success = true });
        }


        private string GenerateOTP()
        {
            const string chars = "0123456789";
            Random random = new Random();
            return new string(Enumerable.Repeat(chars, 6)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }   
}

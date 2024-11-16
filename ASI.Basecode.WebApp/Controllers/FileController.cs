using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.Manager;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.ServiceModels;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Newtonsoft.Json;
using RestSharp;
using System.Web;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;

//using System.Web.Mvc;

namespace ASI.Basecode.WebApp.Controllers
{
    public class FileController : BaseController
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly IWebHostEnvironment _environment;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;
        private const string ApiUrl = "https://api.html2pdf.app/v1";
        private const string ApiKey = "YGZeyRneBbjzEBawNZVcU9ssUNzbbafVNXcN0aLpa92aqvhPAwJ17KAAmugLWhEn";
        private readonly HttpClient _httpClient;
        public FileController(MailManager mailManager, IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor, IRazorViewEngine viewEngine,HttpClient httpClient, ITempDataProvider tempDataProvider,IServiceProvider serviceProvider) : base(mailManager, httpContextAccessor)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
            _httpClient = httpClient;
            _environment = environment;
        }

        public IActionResult DownloadAttachment(int id)
        {
            // Retrieve the ticket from the database using the provided ID
            var ticket = _ticketRepo.Get(id);

            if (ticket == null || string.IsNullOrEmpty(ticket.Attachments))
            {
                TempData["error"] = "Attachment not found.";
                return RedirectToAction("Tickets", "Admin");
            }

            // Construct the full path to the attachment
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", ticket.Attachments);

            // Check if the file exists
            if (!System.IO.File.Exists(filePath))
            {
                TempData["error"] = "File not found.";
                return RedirectToAction("Tickets", "Admin");
            }

            // Get the file's content type based on the file extension
            var contentType = GetContentType(filePath);

            // Read the file bytes
            var fileBytes = System.IO.File.ReadAllBytes(filePath);

            // Return the file as a download
            return File(fileBytes, contentType, Path.GetFileName(filePath));
        }

        // Helper method to get the content type based on file extension
        private string GetContentType(string path)
        {
            var types = new Dictionary<string, string>
            {
                { ".pdf", "application/pdf" },
                { ".jpg", "image/jpeg" },
                { ".jpeg", "image/jpeg" },
                { ".png", "image/png" },
                { ".gif", "image/gif" },
                { ".doc", "application/msword" },
                { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
                { ".xls", "application/vnd.ms-excel" },
                { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
                { ".txt", "text/plain" },
                { ".zip", "application/zip" }
            };

            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types.ContainsKey(ext) ? types[ext] : "application/octet-stream";
        }

        [HttpGet]
        public async Task<IActionResult> DownloadTicketReport()
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
            var statusCounts = tickets.GroupBy(t => t.Status.StatusName)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToDictionary(g => g.Status, g => g.Count);

            var categoryCounts = tickets.GroupBy(t => t.Category.CategoryName)
                .Select(group => new
                {
                    Status = group.Key,
                    Count = group.Count()
                })
                .ToDictionary(g => g.Status, g => g.Count);

            var model = new TicketReportViewModel
            {
                StatusCounts = statusCounts,
                CategoryCounts = categoryCounts
            };

            // Render the view to HTML string
            string viewPath = "Reports/TicketReport";
            string imagePath = Path.Combine(_environment.WebRootPath, "images", "TickeTechy logo with text.png");

            // Convert the image to base64
            string base64Image = ConvertImageToBase64(imagePath);

            // Pass the base64 image to the view
            ViewBag.Base64Image = base64Image;
            string htmlContent = await RenderViewToStringAsync(viewPath, model);

            // Send the HTML to the HTML2PDF API
            using (var httpClient = new HttpClient())
            {
                var client = new RestClient(ApiUrl);
                var request = new RestRequest("/generate", Method.Post);
                request.AddHeader("Content-Type", "application/json");
                request.AddJsonBody(new
                {
                    html = htmlContent,
                    apiKey = ApiKey,
                    format = "Legal",
                    marginTop = 0,
                    marginRight = 0,
                    marginBottom = 0,
                    marginLeft = 0,
                    width = 200
                });

                var response = await client.ExecuteAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string fileName = "TicketReport.pdf";

                    // Return the PDF file
                    return File(response.RawBytes, "application/pdf", fileName);
                }
                else
                {
                    return BadRequest($"Error generating PDF:");
                }
            }
        }

        public string ConvertImageToBase64(string imagePath)
        {
            byte[] imageArray = System.IO.File.ReadAllBytes(imagePath);
            string base64Image = Convert.ToBase64String(imageArray);
            return base64Image;
        }

        private async Task<string> RenderViewToStringAsync(string viewName, object model)
        {
            ViewData.Model = model;

            using (var sw = new StringWriter())
            {
                var actionContext = new ActionContext(HttpContext, RouteData, ControllerContext.ActionDescriptor);
                var viewEngine = HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
                var viewResult = viewEngine.FindView(actionContext, viewName, false);   

                if (!viewResult.Success)
                {
                    throw new InvalidOperationException($"View '{viewName}' not found. Searched locations: {string.Join(", ", viewResult.SearchedLocations)}");
                }

                var view = viewResult.View;

                var viewContext = new ViewContext(
                    actionContext,
                    view,
                    ViewData,
                    TempData,
                    sw,
                    new HtmlHelperOptions()
                );

                await view.RenderAsync(viewContext);
                return sw.ToString();
            }
        }

        public int GetUserId()
        {
            var currentUser = _userRepo.Table.Where(m => m.Username.Equals(User.Identity.Name)).FirstOrDefault();

            return currentUser.Id;
        }


    }
}

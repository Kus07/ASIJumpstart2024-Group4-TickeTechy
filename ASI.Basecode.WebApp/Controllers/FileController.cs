using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.Manager;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;

namespace ASI.Basecode.WebApp.Controllers
{
    public class FileController : BaseController
    {
        public FileController(MailManager mailManager, IHttpContextAccessor httpContextAccessor) : base(mailManager, httpContextAccessor){}

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

    }
}

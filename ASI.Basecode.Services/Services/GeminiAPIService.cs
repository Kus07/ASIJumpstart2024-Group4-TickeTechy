using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.Services
{
    public class GeminiAPIService : IGeminiAPIService
    {
        /// <summary>
        /// Assign ticket to a agent using Gemini AI API
        /// </summary>
        [HttpPost]
        public async Task<int> AssignTicket(string description, string category)
        {
            try
            {
                string apiKey = "AIzaSyCeTO21VfJNQgZOtZ9XfEB1NQ0wq3Fkan8";
                string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key={apiKey}";

                string input = $"Ticket Description: {description}, Ticket Category: {category}. Based on this ticket description and category, assign it to the most appropriate department. These are the department options: 1-HR, 2-IT/Systems, 3-Facilities, 4-Finance, 5-Project Management, 6-Security, 7-General. Just return the number only (1-7) or 0 if unsure. No explanation needed.";

                var data = new
                {
                    contents = new[]
                    { 
                        new
                        {
                            parts = new[]
                            {
                                new
                                {
                                    text = input
                                }
                            }
                        }
                    }
                };

                using (var client = new HttpClient())
                {
                    var json = JsonConvert.SerializeObject(data);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        var jResponse = JObject.Parse(responseString);

                        if (jResponse["candidates"]?[0]?["content"]?["parts"]?[0]?["text"] != null)
                        {
                            var aiResponse = jResponse["candidates"][0]["content"]["parts"][0]["text"].ToString().Trim();

                            // Try to parse the response as an integer
                            if (int.TryParse(aiResponse, out int departmentId))
                            {
                                return departmentId;
                            }
                        }
                    }
                }

                return 0; // Default fallback
            }
            catch (Exception ex)
            {
                return 0;
            }
        }
        [HttpPost]
        public async Task<string> GenerateTicketSummary(string ticketDescription, string ticketCategory, IEnumerable<TicketMessage> conversationHistory, IFormFile image)
        {
            try
            {
                string apiKey = "AIzaSyCeTO21VfJNQgZOtZ9XfEB1NQ0wq3Fkan8";
                string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key={apiKey}";

                var input = $"The ticket description is: {ticketDescription}. The ticket category is: {ticketCategory}.";
                var conversation_history = StringifyMessages(conversationHistory);

                string fullPrompt = $"{input}\n\n{conversation_history}\n\nPlease generate a brief summary of this ticket in the following format:\n\nProblem Overview: [brief description]\nImage Details: [if applicable]\nKey Steps Taken: [summary of actions]\nResolution Details: [how it was resolved]";

                var data = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new
                                {
                                    text = fullPrompt
                                }
                            }
                        }
                    }
                };

                using (var client = new HttpClient())
                {
                    var json = JsonConvert.SerializeObject(data);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        var jResponse = JObject.Parse(responseString);

                        if (jResponse["candidates"]?[0]?["content"]?["parts"]?[0]?["text"] != null)
                        {
                            var aiResponse = jResponse["candidates"][0]["content"]["parts"][0]["text"].ToString();
                            return aiResponse;
                        }
                    }
                }

                return "Unable to generate summary at this time.";
            }
            catch (Exception ex)
            {
                return "Unable to generate summary at this time.";
            }
        }

        public string StringifyMessages(IEnumerable<TicketMessage> messages) {
            string chathistory = "Chat History: \n";
            foreach (var message in messages)
            {
                if (message.User.RoleId == 1)
                {
                    chathistory += "Customer: " + message.Message + "\n";
                }
                else if (message.User.RoleId == 2)
                { 
                    chathistory += "Agent: " + message.Message + "\n";
                }
            }
            return chathistory;
        }
    }
}

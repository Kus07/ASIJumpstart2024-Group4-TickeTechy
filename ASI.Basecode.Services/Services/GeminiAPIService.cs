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
                int departmentId = 0;
                dynamic jsonResponse = "";

                string input = $"Ticket Description: {description}, Ticket Category: {category}";

                using (var client = new HttpClient())
                {
                    var values = new Dictionary<string, string>
                    {
                        { "input", input }
                    };

                    var content = new FormUrlEncodedContent(values);

                    // Post the request to the external API
                    var response = await client.PostAsync("https://proctocodeapis.et.r.appspot.com/assignTicket", content);
                    response.EnsureSuccessStatusCode();

                    // Read the API response
                    var responseString = await response.Content.ReadAsStringAsync();
                    var jResponse = JObject.Parse(responseString);

                    // Handle Safety Ratings
                    //var safetyRatings = jResponse["safety_ratings"]?.ToObject<JArray>();
                    //bool contentFlagged = false;

                    //foreach (var rating in safetyRatings)
                    //{
                    //    string categoryGeminiResponse = rating["category"].ToString();
                    //    string probability = rating["probability"].ToString();

                    //    // Check if dangerous content has MEDIUM or higher probability
                    //    if (category == "HARM_CATEGORY_DANGEROUS_CONTENT" && (probability == "MEDIUM" || probability == "HIGH"))
                    //    {
                    //        return 6;
                    //    }
                    //    else if (category == "HARM_CATEGORY_HATE_SPEECH" && (probability == "MEDIUM" || probability == "HIGH"))
                    //    {
                    //        return 7;
                    //    }
                    //    else if (category == "HARM_CATEGORY_SEXUALLY_EXPLICIT" && (probability == "MEDIUM" || probability == "HIGH"))
                    //    {
                    //        return 7;
                    //    }
                    //    else if (category == "HARM_CATEGORY_HARASSMENT" && (probability == "MEDIUM" || probability == "HIGH"))
                    //    {
                    //        return 6;
                    //    }
                    //    else
                    //    {
                    //        Console.WriteLine($"Category: {category}, Probability: {probability}");
                    //    }
                    //}

                    // Extract the recommendation and safety ratings from the response
                    var recommendation = jResponse["recommendation"].ToString();
                    departmentId = int.Parse(recommendation);

                    // Take appropriate action based on safety ratings
                    //if (contentFlagged)
                    //{
                    //    // You could log, reject the request, or notify the admin
                    //    throw new Exception("Content flagged due to safety concerns.");
                    //}
                }


                // Return the recommendation as JSON
                return departmentId;
            }
            catch (Exception ex)
            {
                // Return error message as JSON
                return 0;
            }
        }
        [HttpPost]
        public async Task<string> GenerateTicketSummary(string ticketDescription, string ticketCategory, IEnumerable<TicketMessage> conversationHistory, IFormFile image)
        {
            try
            {
                var input = $"The ticket description is: {ticketDescription}. The ticket category is: {ticketCategory}.";
                var conversation_history = StringifyMessages(conversationHistory);
                dynamic jsonResponse = "";
                var content = new MultipartFormDataContent();
                if (image != null)
                {
                    content.Add(new StringContent(image.FileName), "file");
                }


                using (var client = new HttpClient())
                {
                    /*var values = new Dictionary<string, string>
                        {
                            { "input", input },
                            { "conversation_history", conversation_history }
                        };*/

                    //var json = JsonConvert.SerializeObject(values);
                    //var content = new FormUrlEncodedContent(values);
                    content.Add(new StringContent(input), "input");
                    content.Add(new StringContent(conversation_history), "conversation_history");

                    var response = await client.PostAsync("https://proctocodeapis.et.r.appspot.com/generateTicketSummary", content);


                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var jResponse = JObject.Parse(responseBody);
                        //var responseData = JsonConvert.DeserializeObject<dynamic>(responseBody);
                        var output = jResponse["response"].ToString();
                        return output;
                    }
                    else
                    {
                        throw new Exception("Failed to generate ticket summary");
                    }
                }
            }
            catch (Exception ex) {
                return null;
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

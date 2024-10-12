using ASI.Basecode.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
                    var response = await client.PostAsync("http://127.0.0.1:5000/chat", content);
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

    }
}

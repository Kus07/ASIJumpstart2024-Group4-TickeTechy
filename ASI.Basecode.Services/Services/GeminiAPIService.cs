using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TickeTechy.Services.Implementations
{
    public class GeminiAPIService
    {
        [HttpPost]
        public async Task<int> AssignTicket(string description, string category)
        {
            try
            {
                int departmentId = 0;
                dynamic jsonResponse = "";

                string input = $"Ticket Description: {description}, Ticket Category: {category}";

                // Set up the client for API request
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

                    var recommendation = jResponse["recommendation"].ToString();
                    departmentId = int.Parse(recommendation);
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

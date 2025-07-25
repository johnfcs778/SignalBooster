// NoteSender sends structured note data to an external API endpoint.
using System;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Synapse.SignalBoosterExample
{
    // Handles sending note payloads to an external API
    public class NoteSender
    {
        private readonly ILogger _logger; // Logger for info/debug
        private readonly string _endpoint = "https://alert-api.com/DrExtract"; // API endpoint

        // Constructor: injects logger
        public NoteSender(ILogger logger) => _logger = logger;

        // Sends the payload to the API (skips if SKIP_API env var is set)
        public void Send(JObject payload)
        {
            // Skip API call in dev mode
            if (Environment.GetEnvironmentVariable("SKIP_API") == "true")
            {
                _logger.LogInformation("[SKIPPED] Would send payload: {Payload}", payload);
                return;
            }
            // Send data to API
            using var httpClient = new HttpClient();
            var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending data to API: {Payload}", payload);
            try
            {
                var response = httpClient.PostAsync(_endpoint, content).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                _logger.LogInformation("API response: {StatusCode}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to post data to external API.");
                throw;
            }
        }
    }
}

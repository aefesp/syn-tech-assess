using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Synapse
{
    /// <summary>
    /// Handles API communication for sending extraction results.
    /// </summary>
    public static class ApiClient
    {
        public static async Task SendExtractionResultAsync(JsonObject jsonPayload, string apiUrl, ILogger logger)
        {
            if (jsonPayload == null)
                throw new ArgumentNullException(nameof(jsonPayload));
            
            if (string.IsNullOrWhiteSpace(apiUrl))
                throw new ArgumentException("API URL cannot be null or empty", nameof(apiUrl));

            logger.LogDebug("Creating HTTP client with 30 second timeout");
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            var serializedJson = JsonSerializer.Serialize(jsonPayload);
            logger.LogDebug("Serialized JSON payload: {JsonPayload}", serializedJson);
            
            var jsonContent = new StringContent(serializedJson, Encoding.UTF8, "application/json");
            logger.LogDebug("Sending POST request to {ApiUrl}", apiUrl);
            
            var response = await httpClient.PostAsync(apiUrl, jsonContent);
            
            logger.LogInformation("API response status: {StatusCode}", response.StatusCode);
            response.EnsureSuccessStatusCode();
        }
    }
}
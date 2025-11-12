using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Synapse
{
    /// <summary>
    /// Handles extracting structured data from physician records and posting them to an API.
    /// </summary>
    public class Program
    {
        static async Task<int> Main(string[] args)
        {
            // Set up logging
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Information);
            });
            
            var logger = loggerFactory.CreateLogger<Program>();

            try
            {
                logger.LogInformation("Starting DME extraction process");

                // Load configuration
                logger.LogDebug("Loading configuration from appsettings.json");
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                var physicianNotePath = configuration["PhysicianNotePath"] ?? "physician_note1.txt";
                var apiUrl = configuration["ApiUrl"] ?? "https://alert-api.com/DrExtract";

                // Allow command-line override of file path
                if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
                {
                    physicianNotePath = args[0];
                    logger.LogInformation("File path overridden via command line: {FilePath}", physicianNotePath);
                }

                logger.LogInformation("Reading physician note from: {FilePath}", physicianNotePath);
                var patientData = NoteParser.ParseFile(physicianNotePath);
                logger.LogDebug("Successfully parsed physician note file");

                logger.LogInformation("Extracting device information from note");
                var deviceType = ExtractDeviceType(patientData);
                var orderingProvider = ExtractOrderingProvider(patientData);
                var liters = ExtractOxygenLiters(patientData);
                var usage = ExtractOxygenUsage(patientData);
                var diagnosis = ExtractDiagnosis(patientData);
                var patientName = ExtractPatientName(patientData);
                var dateOfBirth = ExtractDateOfBirth(patientData);

                logger.LogInformation("Extracted device type: {DeviceType}", deviceType);
                logger.LogDebug("Extracted data - Provider: {Provider}, Patient: {PatientName}, Diagnosis: {Diagnosis}",
                    orderingProvider, patientName, diagnosis);

                logger.LogInformation("Building JSON payload");
                var resultJson = BuildJsonPayload(deviceType, orderingProvider, diagnosis, patientName, dateOfBirth, liters, usage);

                logger.LogInformation("Sending data to API endpoint: {ApiUrl}", apiUrl);
                await ApiClient.SendExtractionResultAsync(resultJson, apiUrl, logger);
                logger.LogInformation("Successfully sent data to API");

                return 0;
            }
            catch (FileNotFoundException ex)
            {
                logger.LogError(ex, "File not found: {FilePath}", ex.FileName);
                return 1;
            }
            catch (FormatException ex)
            {
                logger.LogError(ex, "Invalid file format");
                return 2;
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "HTTP request failed while sending data to API");
                return 3;
            }
            catch (TaskCanceledException ex)
            {
                logger.LogError(ex, "API request timed out");
                return 4;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occurred");
                return 5;
            }
        }

        public static string ExtractDeviceType(Dictionary<string, string> patientData)
        {
            string? prescription = GetValue(patientData, "Prescription");
            string? recommendation = GetValue(patientData, "Recommendation");
            string? text = prescription ?? recommendation;

            if (string.IsNullOrEmpty(text))
                return "Unknown";

            if (text.Contains("CPAP", StringComparison.OrdinalIgnoreCase))
                return "CPAP";
            else if (text.Contains("oxygen", StringComparison.OrdinalIgnoreCase))
                return "Oxygen Tank";
            else if (text.Contains("wheelchair", StringComparison.OrdinalIgnoreCase))
                return "Wheelchair";

            return "Unknown";
        }

        public static string ExtractOrderingProvider(Dictionary<string, string> patientData)
        {
            string? provider = GetValue(patientData, "Ordering Physician");
            if (!string.IsNullOrEmpty(provider))
            {
                return provider;
            }

            return "Unknown";
        }

        public static string? ExtractOxygenLiters(Dictionary<string, string> patientData)
        {
            string? prescription = GetValue(patientData, "Prescription");
            string? recommendation = GetValue(patientData, "Recommendation");
            string? text = prescription ?? recommendation;

            if (string.IsNullOrEmpty(text))
                return null;

            // Matches oxygen liter measurements like "2 L", "2.5L", "3 L", etc.
            // Pattern breakdown: (\d+(\.\d+)?) captures the number (integer or decimal),
            // ? matches an optional space, and L matches the unit (case-insensitive)
            Match literMatch = Regex.Match(text, @"(\d+(\.\d+)?) ?L", RegexOptions.IgnoreCase);
            if (literMatch.Success)
            {
                return literMatch.Groups[1].Value + " L";
            }

            return null;
        }

        public static string? ExtractOxygenUsage(Dictionary<string, string> patientData)
        {
            string? usageText = GetValue(patientData, "Usage");
            if (string.IsNullOrEmpty(usageText))
                return null;

            bool hasSleep = usageText.Contains("sleep", StringComparison.OrdinalIgnoreCase);
            bool hasExertion = usageText.Contains("exertion", StringComparison.OrdinalIgnoreCase);

            if (hasSleep && hasExertion)
                return "sleep and exertion";
            else if (hasSleep)
                return "sleep";
            else if (hasExertion)
                return "exertion";

            return null;
        }

        public static string ExtractDiagnosis(Dictionary<string, string> patientData)
        {
            return GetValue(patientData, "Diagnosis") ?? "Unknown";
        }

        public static string ExtractPatientName(Dictionary<string, string> patientData)
        {
            return GetValue(patientData, "Patient Name") ?? "Unknown";
        }

        public static string ExtractDateOfBirth(Dictionary<string, string> patientData)
        {
            return GetValue(patientData, "DOB") ?? "Unknown";
        }

        private static string? GetValue(Dictionary<string, string> patientData, string key)
        {
            if (patientData == null)
                return null;

            return patientData.TryGetValue(key, out string? value) ? value : null;
        }

        public static JsonObject BuildJsonPayload(
            string deviceType, string orderingProvider, string diagnosis, string patientName,
            string dateOfBirth, string? liters, string? usage
            )
        {
            var resultJson = new JsonObject
            {
                ["device"] = deviceType,
                ["ordering_provider"] = orderingProvider,
                ["diagnosis"] = diagnosis,
                ["patient_name"] = patientName,
                ["dob"] = dateOfBirth
            };

            if (liters != null)
            {
                resultJson["liters"] = liters;
            }

            if (usage != null)
            {
                resultJson["usage"] = usage;
            }

            return resultJson;
        }
    }
}

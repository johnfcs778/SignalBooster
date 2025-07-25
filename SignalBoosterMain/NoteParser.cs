// NoteParser extracts structured data from medical notes using regex and string matching.
using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Synapse.SignalBoosterExample
{
    // Interface for note parsers
    public interface IParser
    {
        Task<JObject> ParseAndExtract(string filePath);
        Task<JObject> ParseAndExtractFromContent(string content);
    }

    // NoteParser implements IParser using regex and heuristics (no LLM)
    public class NoteParser : IParser
    {
        private readonly ILogger _logger; // Logger for info/debug


        public NoteParser(ILogger logger)
        {
            _logger = logger;
        }

        // Reads a file and parses its content into structured JSON
        public async Task<JObject> ParseAndExtract(string filePath)
        {
            string content = File.ReadAllText(filePath);
            return await ParseAndExtractFromContent(content);
        }

        // Parses note content (string) into structured JSON
        public async Task<JObject> ParseAndExtractFromContent(string content)
        {
            // If content is already JSON, extract the 'data' field
            if (content.TrimStart().StartsWith("{"))
            {
                _logger.LogInformation("Detected JSON note format.");
                content = JObject.Parse(content)["data"]?.ToString() ?? string.Empty;
            }

            _logger.LogInformation("Extracting structured data from note.");
            var result = ExtractData(content);
            return await Task.FromResult(result);
        }

        // Extracts structured fields from the note using regex and string matching
        public JObject ExtractData(string note)
        {
            var result = new JObject();
            var addOns = new List<string>();

            // Extract basic fields using regex
            result["patient_name"] = MatchValue(note, "Patient Name: (.+)");
            result["dob"] = MatchValue(note, "DOB: (.+)");
            result["diagnosis"] = MatchValue(note, "Diagnosis: (.+)");
            result["ordering_provider"] = MatchValue(note, "Ordering Physician: (Dr\\..+)")
                                          ?? MatchValue(note, "Ordered by (Dr\\..+)");

            // Guess device type based on keywords
            string device = "Unknown";
            if (note.Contains("CPAP", StringComparison.OrdinalIgnoreCase))
                device = "CPAP";
            else if (note.Contains("oxygen", StringComparison.OrdinalIgnoreCase))
                device = "Oxygen Tank";
            else if (note.Contains("wheelchair", StringComparison.OrdinalIgnoreCase))
                device = "Wheelchair";

            result["device"] = device;

            // Extract device-specific fields
            if (device == "CPAP")
            {
                if (note.Contains("full face", StringComparison.OrdinalIgnoreCase))
                    result["mask_type"] = "full face";

                if (note.Contains("humidifier", StringComparison.OrdinalIgnoreCase))
                    addOns.Add("humidifier");

                var ahiMatch = Regex.Match(note, @"AHI[:\\s]+(\\d+)");
                if (ahiMatch.Success)
                    result["qualifier"] = $"AHI: {ahiMatch.Groups[1].Value}";
                else if (note.Contains("AHI > 20"))
                    result["qualifier"] = "AHI > 20";
            }
            else if (device == "Oxygen Tank")
            {
                var match = Regex.Match(note, @"(\\d+(\\.\\d+)?) ?L");
                if (match.Success)
                    result["liters"] = match.Groups[1].Value + " L";

                // Determine usage context
                bool sleep = note.Contains("sleep", StringComparison.OrdinalIgnoreCase);
                bool exertion = note.Contains("exertion", StringComparison.OrdinalIgnoreCase);
                if (sleep && exertion)
                    result["usage"] = "sleep and exertion";
                else if (sleep)
                    result["usage"] = "sleep";
                else if (exertion)
                    result["usage"] = "exertion";
            }

            // Add-ons (e.g., humidifier)
            if (addOns.Count > 0)
            {
                result["add_ons"] = new JArray(addOns);
            }

            return result;
        }

        // Helper: returns first regex group match or null
        private string? MatchValue(string text, string pattern)
        {
            var match = Regex.Match(text, pattern);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }
    }
}

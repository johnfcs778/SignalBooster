using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Synapse.SignalBoosterExample
{
    public interface IParser
    {
        Task<JObject> ParseAndExtract(string filePath);
    }

    public class NoteParser : IParser
    {
        private readonly ILogger _logger;

        public NoteParser(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<JObject> ParseAndExtract(string filePath)
        {
            string content = File.ReadAllText(filePath);
            if (content.TrimStart().StartsWith("{"))
            {
                _logger.LogInformation("Detected JSON note format.");
                content = JObject.Parse(content)["data"]?.ToString() ?? string.Empty;
            }

            _logger.LogInformation("Extracting structured data from note.");
            // No LLM, just use ExtractData
            var result = ExtractData(content);
            return await Task.FromResult(result);
        }

        public JObject ExtractData(string note)
        {
            var result = new JObject();
            var addOns = new List<string>();


            result["patient_name"] = MatchValue(note, "Patient Name: (.+)");
            result["dob"] = MatchValue(note, "DOB: (.+)");
            result["diagnosis"] = MatchValue(note, "Diagnosis: (.+)");
            result["ordering_provider"] = MatchValue(note, "Ordering Physician: (Dr\\..+)")
                                          ?? MatchValue(note, "Ordered by (Dr\\..+)");

            string device = "Unknown";
            if (note.Contains("CPAP", StringComparison.OrdinalIgnoreCase))
                device = "CPAP";
            else if (note.Contains("oxygen", StringComparison.OrdinalIgnoreCase))
                device = "Oxygen Tank";
            else if (note.Contains("wheelchair", StringComparison.OrdinalIgnoreCase))
                device = "Wheelchair";

            result["device"] = device;
            
            if (device == "CPAP")
            {
                if (note.Contains("full face", StringComparison.OrdinalIgnoreCase))
                    result["mask_type"] = "full face";

                if (note.Contains("humidifier", StringComparison.OrdinalIgnoreCase))
                    addOns.Add("humidifier");

                var ahiMatch = Regex.Match(note, @"AHI[:\s]+(\d+)");
                if (ahiMatch.Success)
                    result["qualifier"] = $"AHI: {ahiMatch.Groups[1].Value}";
                else if (note.Contains("AHI > 20"))
                    result["qualifier"] = "AHI > 20";
            }

            else if (device == "Oxygen Tank")
            {
                var match = Regex.Match(note, @"(\d+(\.\d+)?) ?L");
                if (match.Success)
                    result["liters"] = match.Groups[1].Value + " L";

                bool sleep = note.Contains("sleep", StringComparison.OrdinalIgnoreCase);
                bool exertion = note.Contains("exertion", StringComparison.OrdinalIgnoreCase);
                if (sleep && exertion)
                    result["usage"] = "sleep and exertion";
                else if (sleep)
                    result["usage"] = "sleep";
                else if (exertion)
                    result["usage"] = "exertion";
            }
            if (addOns.Count > 0)
            {
                result["add_ons"] = new JArray(addOns);
            }


            return result;
        }

        private string? MatchValue(string text, string pattern)
        {
            var match = Regex.Match(text, pattern);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }
    }
}

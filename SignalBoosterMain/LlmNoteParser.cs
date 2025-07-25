// LlmNoteParser uses OpenAI's API to extract structured data from medical notes using an LLM.
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.ResponseModels;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.IO;
using Newtonsoft.Json;
using System;

namespace Synapse.SignalBoosterExample
{
    // Implements IParser to extract structured info from medical notes using an LLM
    public class LlmNoteParser : IParser
    {
        private readonly OpenAIService _openAi; // OpenAI API service instance

        // Constructor: initializes OpenAI service with provided API key
        public LlmNoteParser(string apiKey)
        {
            _openAi = new OpenAIService(new OpenAiOptions
            {
                ApiKey = apiKey
            });
        }

        // Reads a file, extracts content, and parses it into structured JSON
        public async Task<JObject> ParseAndExtract(string filePath)
        {
            string content = File.ReadAllText(filePath);
            // If file is already JSON, extract the 'data' field
            if (content.TrimStart().StartsWith("{"))
            {
                content = JObject.Parse(content)["data"]?.ToString() ?? string.Empty;
            }
            return await ParseAndExtractFromContent(content);
        }

        // Parses raw note content into structured JSON using the LLM
        public async Task<JObject> ParseAndExtractFromContent(string content)
        {
            var jsonString = await ParseAsync(content);
            try
            {
                return JObject.Parse(jsonString);
            }
            catch (JsonReaderException ex)
            {
                // If LLM output is not valid JSON, throw a clear error
                throw new InvalidOperationException("Failed to parse LLM response as JSON:\n" + jsonString, ex);
            }
        }

        // Calls OpenAI with retries and exponential backoff on rate limit errors
        private async Task<ChatCompletionCreateResponse> CallWithRetry(ChatCompletionCreateRequest request, int retries = 3)
        {
            for (int i = 0; i < retries; i++)
            {
                var response = await _openAi.ChatCompletion.CreateCompletion(request);
                if (response.Successful)
                    return response;

                Console.WriteLine($"Retry {i + 1}/{retries} - Error: {response.Error?.Message}");
                // Only retry on rate limit errors
                if (response.HttpStatusCode != System.Net.HttpStatusCode.TooManyRequests)
                    break;

                await Task.Delay((int)Math.Pow(2, i) * 1000); // backoff: 1s, 2s, 4s
            }
            throw new InvalidOperationException("OpenAI request failed after retries.");
        }

        // Sends the note to the LLM and returns the extracted JSON string
        public async Task<string> ParseAsync(string note)
        {
            // Prompt instructs the LLM to extract a specific JSON schema from the note
            var prompt = 
            "Extract the following structured JSON from the medical note below.\n\n" +
            "Return only a JSON object with this schema:\n" +
            "{\n" +
            "  \"patient_name\": string,\n" +
            "  \"dob\": string,\n" +
            "  \"diagnosis\": string,\n" +
            "  \"device\": string,\n" +
            "  \"mask_type\": string,\n" +
            "  \"add_ons\": [string],\n" +
            "  \"qualifier\": string,\n" +
            "  \"ordering_provider\": string,\n" +
            "  \"liters\": string,\n" +
            "  \"usage\": string\n" +
            "}\n\n" +
            "Medical note:\n" +
            "\"\"\"\n" +
            note + "\n" +
            "\"\"\"";

            var completion = await CallWithRetry(new ChatCompletionCreateRequest
            {
                Model = "gpt-3.5-turbo",
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem("You are a helpful assistant that returns only JSON responses."),
                    ChatMessage.FromUser(prompt)
                },
                Temperature = 0.2f
            });

            Console.WriteLine("Raw LLM Response:");
            Console.WriteLine(completion.ToString());
            if (completion == null || completion.Choices == null || !completion.Choices.Any())
            {
                throw new InvalidOperationException("OpenAI returned no completions.");
            }

            return completion.Choices.First().Message.Content;
        }
    }
}

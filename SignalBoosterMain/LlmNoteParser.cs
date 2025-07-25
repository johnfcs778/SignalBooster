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
    public class LlmNoteParser : IParser
    {
        private readonly OpenAIService _openAi;

        public LlmNoteParser(string apiKey)
        {
            _openAi = new OpenAIService(new OpenAiOptions
            {
                ApiKey = apiKey
            });
        }

        public async Task<JObject> ParseAndExtract(string filePath)
        {
            string content = File.ReadAllText(filePath);
            if (content.TrimStart().StartsWith("{"))
            {
                content = JObject.Parse(content)["data"]?.ToString() ?? string.Empty;
            }
            return await ParseAndExtractFromContent(content);
        }

        public async Task<JObject> ParseAndExtractFromContent(string content)
        {
            var jsonString = await ParseAsync(content);
            try
            {
                return JObject.Parse(jsonString);
            }
            catch (JsonReaderException ex)
            {
                throw new InvalidOperationException("Failed to parse LLM response as JSON:\n" + jsonString, ex);
            }
        }

        private async Task<ChatCompletionCreateResponse> CallWithRetry(ChatCompletionCreateRequest request, int retries = 3)
        {
            for (int i = 0; i < retries; i++)
            {
                var response = await _openAi.ChatCompletion.CreateCompletion(request);
                if (response.Successful)
                    return response;

                Console.WriteLine($"Retry {i + 1}/{retries} - Error: {response.Error?.Message}");
                if (response.HttpStatusCode != System.Net.HttpStatusCode.TooManyRequests)
                    break;

                await Task.Delay((int)Math.Pow(2, i) * 1000); // backoff: 1s, 2s, 4s
            }
            throw new InvalidOperationException("OpenAI request failed after retries.");
        }

        public async Task<string> ParseAsync(string note)
        {
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

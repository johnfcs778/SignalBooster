// Program entry point for SignalBooster: parses and sends medical notes using LLM or regex parser.
using System;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Synapse.SignalBoosterExample;
using System.Linq;

namespace Synapse.SignalBoosterExample
{
    class Program
    {
        // Main entry: parses args, loads note, extracts data, sends to API
        static async Task<int> Main(string[] args)
        {
            bool useLlm = args.Contains("--llm"); // Use LLM parser if flag present
            string source = args.FirstOrDefault(arg => arg.StartsWith("--source="))?.Split('=')[1] ?? "file";
            string path = args.FirstOrDefault(arg => arg.StartsWith("--path="))?.Split('=')[1] ?? "physician_note1.txt";
            string url = args.FirstOrDefault(arg => arg.StartsWith("--url="))?.Split('=')[1];

            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            ILogger logger = loggerFactory.CreateLogger<Program>();

            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (useLlm && string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");
            }

            try
            {
                string content;
                if (source == "api")
                {
                    // Load note content from URL if --source=api
                    if (string.IsNullOrWhiteSpace(url))
                        throw new ArgumentException("--url must be provided when --source=api");

                    using var httpClient = new System.Net.Http.HttpClient();
                    content = await httpClient.GetStringAsync(url);
                }
                else
                {
                    // Load note content from file (default)
                    content = System.IO.File.ReadAllText(path);
                }

                // Choose parser: LLM or regex-based
                IParser parser = useLlm ? new LlmNoteParser(apiKey) : new NoteParser(logger);
                var note = await parser.ParseAndExtractFromContent(content);

                // Send extracted data to API
                var sender = new NoteSender(logger);
                sender.Send(note);

                return 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception in application.");
                return 1;
            }
        }
    }
}

using System;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Synapse.SignalBoosterExample;
using System.Linq;


namespace Synapse.SignalBoosterExample
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            bool useLlm = Array.IndexOf(args, "--llm") >= 0;
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            ILogger logger = loggerFactory.CreateLogger<Program>();

            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");
            }

            try
            {
                string path = args.FirstOrDefault(arg => !arg.StartsWith("--")) ?? "physician_note1.txt";
                IParser parser = useLlm ? new LlmNoteParser(apiKey) : new NoteParser(logger);
                var note = await parser.ParseAndExtract(path);

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

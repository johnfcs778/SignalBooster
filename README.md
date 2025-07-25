# SignalBooster

SignalBooster is a console-based C# application that parses physician notes and extracts structured Durable Medical Equipment (DME) data. It supports both rule-based parsing and optional LLM-based parsing via the OpenAI API.

---

## 🔧 Setup

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An OpenAI API key (if using the `--llm` option)

### Clone the Project
```bash
git clone https://github.com/your-username/SignalBooster.git
cd SignalBooster/SignalBoosterMain
```

---

## ▶️ Running the App

### Basic File Parsing
```bash
dotnet run -- --path=physician_note1.txt
```

### With LLM-Based Parsing (OpenAI)
```bash
dotnet run -- --path=physician_note1.txt --llm
```

### Parse from API Endpoint (GET)
```bash
dotnet run -- --source=api --url=https://example.com/note
```

---

## ⚙️ Configuration Flags

| Flag        | Description                                           | Default     |
|-------------|-------------------------------------------------------|-------------|
| `--source`  | `file` (local file) or `api` (download via HTTP)      | `file`      |
| `--path`    | Local file path for note input                        | N/A         |
| `--url`     | URL to fetch note from (when `--source=api`)         | N/A         |
| `--llm`     | Enable LLM-based parsing via OpenAI                   | `false`     |

To use the `--llm` flag, set your OpenAI API key in an environment variable:
```bash
export OPENAI_API_KEY=your_api_key_here  # Unix/macOS
set OPENAI_API_KEY=your_api_key_here     # Windows
```

---

## ✅ Running Tests

Unit tests are written with xUnit.

```bash
cd ../SignalBoosterTests
dotnet test
```

---

## 🤖 LLM Parsing Details

When the `--llm` flag is provided, the app uses the OpenAI API (gpt-3.5-turbo or compatible) to parse the note and return structured JSON.

- LLM-based parsing is defined in `LlmNoteParser.cs`
- You can switch the model or endpoint via code

---

## 🚀 Improvements I Would Make

If continuing development, I would focus on:

### 🧪 1. Better Test Coverage
- Add tests for LLM fallback behavior and edge cases
- Mock HTTP calls and OpenAI API for deterministic testability

### 🧠 2. Enhanced NLP Capabilities
- Use named entity recognition (NER) or a fine-tuned model for better entity extraction
- Add more robust regex fallback for ambiguous cases

### 🛠 3. Config File Support
- Allow loading settings (API keys, source preferences, etc.) from `appsettings.json` or environment-specific config

### ☁️ 4. Deployment-Ready Enhancements
- Add logging to file (e.g. `serilog`) for audit trail
- Graceful retry logic for transient LLM or network failures

### 📦 5. API Wrapper
- Expose the parser logic as a web API endpoint (ASP.NET Core) for integration with other systems

### 🧩 6. Multi-format Support
- Add support for Word/PDF inputs (with OCR if needed)
- Option to output in HL7/FHIR format for EHR integration

---

## 📄 License

MIT License – free for personal or commercial use.

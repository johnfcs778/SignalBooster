using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using Synapse.SignalBoosterExample;
using Newtonsoft.Json.Linq;

public class NoteParserTests
{
    [Fact]
    public void ExtractData_Parses_OxygenNoteCorrectly()
    {
        var logger = NullLogger.Instance;
        var parser = new NoteParser(logger);

        string note = @"Patient Name: Harold Finch
DOB: 04/12/1952
Diagnosis: COPD
Prescription: Requires a portable oxygen tank delivering 2 L per minute.
Usage: During sleep and exertion.
Ordering Physician: Dr. Cuddy";

        JObject result = parser.ExtractData(note);

        Assert.Equal("Harold Finch", result["patient_name"]);
        Assert.Equal("04/12/1952", result["dob"]);
        Assert.Equal("COPD", result["diagnosis"]);
        Assert.Equal("Dr. Cuddy", result["ordering_provider"]);
        Assert.Equal("Oxygen Tank", result["device"]);
        Assert.Equal("2 L", result["liters"]);
        Assert.Equal("sleep and exertion", result["usage"]);
    }

    [Fact]
    public void ExtractData_Parses_CPAPNoteCorrectly()
    {
        var logger = NullLogger.Instance;
        var parser = new NoteParser(logger);

        string note = @"Patient Name: Lisa Turner
    DOB: 09/23/1984
    Diagnosis: Severe sleep apnea
    Recommendation: CPAP therapy with full face mask and heated humidifier.
    AHI: 28
    Ordering Physician: Dr. Foreman";

        JObject result = parser.ExtractData(note);

        Assert.Equal("Lisa Turner", result["patient_name"]);
        Assert.Equal("09/23/1984", result["dob"]);
        Assert.Equal("Severe sleep apnea", result["diagnosis"]);
        Assert.Equal("Dr. Foreman", result["ordering_provider"]);
        Assert.Equal("CPAP", result["device"]);
        Assert.Equal("full face", result["mask_type"]);
        Assert.Matches(@"AHI[:\s]+28", result["qualifier"]?.ToString() ?? "");
    }

}

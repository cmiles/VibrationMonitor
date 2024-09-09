using System.ComponentModel.DataAnnotations;

namespace VibrationMonitorErrorDb;

public class ErrorLog
{
    public string? Exception { get; set; }

    // ReSharper disable once InconsistentNaming - Matches column from the Serilog sqlite plugin
    [Key] public int id { get; set; }
    [StringLength(10)] public string Level { get; set; } = string.Empty;
    public string? Properties { get; set; }
    public string? RenderedMessage { get; set; }
    public DateTime TimeStamp { get; set; }
}
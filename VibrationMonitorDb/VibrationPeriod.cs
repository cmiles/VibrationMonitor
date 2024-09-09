namespace VibrationMonitorDb;

public class VibrationPeriod
{
    public string Description { get; set; } = string.Empty;
    public int? DurationInSeconds { get; set; }
    public DateTime? EndedOn { get; set; }
    public int Id { get; set; }
    public DateTime StartedOn { get; set; }
}
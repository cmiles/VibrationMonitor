using CommandLine;

namespace VibrationMonitor;

internal class Options
{
    [Option('d', "description", Required = false,
        HelpText =
            "A short description to associate with the Vibration Periods in the database.", Default = "Vibration Detected")]
    public string Description { get; set; } = "Vibration Detected";

    [Option('p', "gpiopin", Required = false,
        HelpText = "The gpio pin the SW420 Sensor is connected to.", Default = 17)]
    public int GpioPin { get; set; }

    [Option('m', "minimumperiod", Required = false,
        HelpText = "The minimum duration in milliseconds to record as a Vibration Period.", Default = 2000)]
    public int MinimumDuration { get; set; }

    [Option('f', "pollingfrequency", Required = false,
        HelpText = "The number of milliseconds between polling the sensor state", Default = 500)]
    public int PollingFrequency { get; set; }
}
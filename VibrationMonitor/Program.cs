using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using VibrationMonitor;
using VibrationMonitorUtilities;

var parseResult = Parser.Default.ParseArguments<Options>(args);

if (parseResult.Errors.Any())
{
    foreach (var resultError in parseResult.Errors)
    {
        if (resultError.Tag is ErrorType.HelpRequestedError or ErrorType.HelpVerbRequestedError
            or ErrorType.VersionRequestedError) continue;

        Console.WriteLine($"Error: {resultError}");
    }

    return;
}

LogTools.StandardStaticLoggerForProgramDirectory("VibrationMonitor");

Console.WriteLine($"Startup Options -> Gpio Pin: {parseResult.Value.GpioPin}");
Console.WriteLine($"Startup Options -> Polling Frequency: {parseResult.Value.PollingFrequency}");
Console.WriteLine($"Startup Options -> Minimum Vibration Duration: {parseResult.Value.MinimumDuration}");
Console.WriteLine($"Startup Options -> Description: {parseResult.Value.Description}");

Log.ForContext(nameof(parseResult), parseResult.SafeObjectDump()).Debug(
    "Command Line Options: Gpio Pin {0}, Polling Frequency: {1}, Minimum Vibration Duration {2}, Description: {3}",
    parseResult.Value.GpioPin, parseResult.Value.PollingFrequency, parseResult.Value.MinimumDuration,
    parseResult.Value.Description);

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSystemd();
builder.Services.AddHostedService<VibrationSensorWorker>(x => new VibrationSensorWorker
{
    GpioPin = parseResult.Value.GpioPin, MinimumPeriodInMilliseconds = parseResult.Value.MinimumDuration,
    VibrationDescription = parseResult.Value.Description,
    PollingFrequencyInMilliseconds = parseResult.Value.PollingFrequency
});

var host = builder.Build();

try
{
    host.Run();
}
catch (Exception e)
{
    Log.Error(e, "Exception with host.Run");
}
finally
{
    Log.CloseAndFlush();
}
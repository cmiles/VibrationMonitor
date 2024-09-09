using System.Device.Gpio;
using Microsoft.Extensions.Hosting;
using Serilog;
using VibrationMonitorUtilities;

namespace VibrationMonitor;

public class VibrationSensorWorker : BackgroundService
{
    public int GpioPin { get; set; } = 17;
    public int MinimumPeriodInMilliseconds { get; set; } = 2000;
    public int PollingFrequencyInMilliseconds { get; set; } = 500;
    public string VibrationDescription { get; set; } = "Vibration Detected";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("Starting Vibration Monitor - SW-420 sensor on Pin {sensorPin}", GpioPin);

        var gpioController = new GpioController();
        gpioController.OpenPin(GpioPin, PinMode.Input);

        var vibrationProcessor = await VibrationProcessor.CreateInstance(LocationTools.DataDbFilename());
        vibrationProcessor.MinimumPeriodInMilliseconds = MinimumPeriodInMilliseconds;
        vibrationProcessor.VibrationDescription = VibrationDescription;

        await VibrationMonitorDb.VibrationMonitorDbContext.CreateInstanceWithEnsureCreated(
            LocationTools.DataDbFilename());

        while (true)
        {
            var sensorValue = gpioController.Read(GpioPin);

            await vibrationProcessor.ProcessVibrationChange(DateTime.Now, sensorValue == PinValue.High);

            await Task.Delay(500, stoppingToken);
        }
    }
}
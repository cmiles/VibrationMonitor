using Serilog;
using VibrationMonitorDb;
using VibrationMonitorUtilities;

namespace VibrationMonitor;

/// <summary>
/// This class handles Vibration Processing dealing with Minimum Period Lengths and Database Writes.
/// The expectation is that this class will be used from a loop that reads the sensor data - if used
/// in another context you will have to be more careful about feeding the class data. Call CreateInstance
/// to get a new instance.
/// </summary>
public class VibrationProcessor
{
    public VibrationPeriod? CurrentVibrationPeriod { get; set; }
    public required string DbFileName { get; set; }
    public DateTime? LastVibrationTime { get; set; }
    public int MinimumPeriodInMilliseconds { get; set; } = 2000;
    public string VibrationDescription { get; set; } = "Vibration Detected";

    public static async Task<VibrationProcessor> CreateInstance(string dbFileName)
    {
        await VibrationMonitorDbContext.CreateInstanceWithEnsureCreated(dbFileName);

        return new VibrationProcessor { DbFileName = dbFileName };
    }

    public async Task ProcessVibrationChange(DateTime changeOn, bool isVibrating)
    {
        //New Vibration
        if (isVibrating && CurrentVibrationPeriod is null)
        {
            try
            {
                LastVibrationTime = changeOn;
                Log.Verbose("Starting new Vibration Period");
                CurrentVibrationPeriod = new VibrationPeriod
                    { StartedOn = LastVibrationTime.Value, Description = VibrationDescription };
            }
            catch (Exception e)
            {
                Log.Error(e, "Error writing new vibration period");
            }

            return;
        }

        //Continuing Vibration - Reset the last vibration time
        if (isVibrating && CurrentVibrationPeriod is not null)
        {
            LastVibrationTime = changeOn;
            if (CurrentVibrationPeriod.Id < 1 &&
                LastVibrationTime.Value.Subtract(CurrentVibrationPeriod.StartedOn).TotalMilliseconds >=
                MinimumPeriodInMilliseconds)
                CurrentVibrationPeriod =
                    await VibrationMonitorDbQuery.NewGreyWaterPumpVibrationPeriod(CurrentVibrationPeriod, DbFileName);

            return;
        }

        //Vibration Ended, but we have an invalid state - reset state and continue
        if (!isVibrating && (CurrentVibrationPeriod is null || LastVibrationTime == null))
        {
            Log.ForContext(nameof(LastVibrationTime), LastVibrationTime.SafeObjectDump())
                .ForContext(nameof(CurrentVibrationPeriod), CurrentVibrationPeriod.SafeObjectDump())
                .ForContext("comment",
                    "This state is not logged as an error since it can be easily recovered from, but it is logged because it shouldn't occur...")
                .Warning(
                    "Invalid State: Vibration ended but LastVibrationTime is Null {0} or CurrentVibrationPeriod is Null {1}",
                    LastVibrationTime is null, CurrentVibrationPeriod is null);
            LastVibrationTime = null;
            CurrentVibrationPeriod = null;

            return;
        }

        //This is to work with the SW-420 sensor since we don't want a 'single vibration' but rather a 'vibration period',
        //if we have a recent vibration then ignore the PinValue.Low and continue
        if (!isVibrating &&
            changeOn.Subtract(LastVibrationTime ?? DateTime.MinValue).TotalMilliseconds < MinimumPeriodInMilliseconds)
            return;

        //Vibration Period Ended
        if (!isVibrating)
            if (CurrentVibrationPeriod != null)
                if (LastVibrationTime!.Value.Subtract(CurrentVibrationPeriod.StartedOn).TotalMilliseconds <=
                    MinimumPeriodInMilliseconds)
                {
                    Log.ForContext(nameof(CurrentVibrationPeriod), CurrentVibrationPeriod.SafeObjectDump())
                        .ForContext(nameof(LastVibrationTime), LastVibrationTime.SafeObjectDump())
                        .Verbose("Vibration Period too short - Ignoring");
                    LastVibrationTime = null;
                    CurrentVibrationPeriod = null;
                }
                else
                {
                    try
                    {
                        await VibrationMonitorDbQuery.EndGreyWaterPumpVibrationPeriod(CurrentVibrationPeriod,
                            LastVibrationTime!.Value, DbFileName);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Error writing new vibration period");
                    }
                    finally
                    {
                        LastVibrationTime = null;
                        CurrentVibrationPeriod = null;
                    }
                }
    }
}
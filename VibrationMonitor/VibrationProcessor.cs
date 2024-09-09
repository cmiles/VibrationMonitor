using Serilog;
using VibrationMonitorDb;

namespace VibrationMonitor;

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
                CurrentVibrationPeriod = new VibrationPeriod { StartedOn = LastVibrationTime.Value, Description = VibrationDescription};
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
                LastVibrationTime.Value.Subtract(CurrentVibrationPeriod.StartedOn).TotalMilliseconds >= MinimumPeriodInMilliseconds)
                CurrentVibrationPeriod = await VibrationMonitorDbQuery.NewGreyWaterPumpVibrationPeriod(CurrentVibrationPeriod, DbFileName);

            return;
        }

        //Vibration Ended, but we have an invalid state - reset state and continue
        if (!isVibrating && (CurrentVibrationPeriod is null ||  LastVibrationTime == null))
        {
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
                if (LastVibrationTime!.Value.Subtract(CurrentVibrationPeriod.StartedOn).TotalMilliseconds <= MinimumPeriodInMilliseconds)
                {
                    LastVibrationTime = null;
                    CurrentVibrationPeriod = null;
                    return;
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
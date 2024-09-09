using Microsoft.EntityFrameworkCore;
using Serilog;

namespace VibrationMonitorDb;

public static class VibrationMonitorDbQuery
{
    public static async Task<VibrationPeriod?> LastGreyWaterPumpVibration(string databaseName)
    {
        var db = await VibrationMonitorDbContext.CreateInstance(databaseName);
        return await db.GreyWaterPumpVibrations.OrderByDescending(v => v.StartedOn).FirstOrDefaultAsync();
    }

    public static async Task<List<VibrationPeriod>> LastNGreyWaterPumpVibrations(string databaseName,
        int nNumberOfVibrations)
    {
        var db = await VibrationMonitorDbContext.CreateInstance(databaseName);
        return await db.GreyWaterPumpVibrations.OrderByDescending(v => v.StartedOn).Take(nNumberOfVibrations)
            .ToListAsync();
    }

    public static async Task<List<VibrationPeriod>> GreyWaterVibrationsByStartTime(string databaseName,
        DateTime startTimesBeginning, DateTime startTimesEnding)
    {
        var db = await VibrationMonitorDbContext.CreateInstance(databaseName);
        return await db.GreyWaterPumpVibrations
            .Where(v => v.StartedOn >= startTimesBeginning && v.StartedOn <= startTimesEnding)
            .OrderBy(v => v.StartedOn)
            .ToListAsync();
    }

    public static async Task<VibrationPeriod> NewGreyWaterPumpVibrationPeriod(VibrationPeriod vibrationPeriod, string databaseName)
    {
        var frozenNow = DateTime.Now;
        Log.Information("Writing a new VibrationPeriod Entry {startTime}", frozenNow);
        var db = await VibrationMonitorDbContext.CreateInstance(databaseName);
        db.GreyWaterPumpVibrations.Add(vibrationPeriod);
        await db.SaveChangesAsync();
        return vibrationPeriod;
    }

    public static async Task EndGreyWaterPumpVibrationPeriod(VibrationPeriod vibrationPeriod, DateTime endDateTime,
        string databaseName)
    {
        var db = await VibrationMonitorDbContext.CreateInstance(databaseName);
        var vibrationEntry = await db.GreyWaterPumpVibrations.SingleOrDefaultAsync(x => x.Id == vibrationPeriod.Id);

        if (vibrationEntry is null)
        {
            Log.Error("Vibration Entry {vibrationId} not found", vibrationPeriod.Id);
            return;
        }

        vibrationEntry.EndedOn = endDateTime;
        vibrationEntry.DurationInSeconds = (int?)(vibrationEntry.EndedOn - vibrationEntry.StartedOn)?.TotalSeconds;

        Log.Information(
            "Ending VibrationPeriod Entry {startTime} to {endTime} - Duration in Seconds {durationInSeconds}",
            vibrationEntry.StartedOn, vibrationEntry.EndedOn, vibrationEntry.DurationInSeconds);

        await db.SaveChangesAsync();
    }
}
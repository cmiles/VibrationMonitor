using Microsoft.EntityFrameworkCore;
using Serilog;
using VibrationMonitor;
using VibrationMonitorDb;
using VibrationMonitorErrorDb;
using VibrationMonitorUtilities;

namespace VibrationMonitorTests;

public class BasicVibrationTests
{
    public VibrationProcessor Processor { get; set; }
    public DateTime ReferenceDateTime { get; set; }

    [SetUp]
    public async Task Setup()
    {
        LogTools.StandardStaticLoggerForProgramDirectory("PiSlicedDayPhotos");

        ReferenceDateTime = DateTime.Now;

        Processor = await VibrationProcessor.CreateInstance(LocationTools.DataDbFilename());
        Processor.MinimumPeriodInMilliseconds = 3000;
    }

    [Test]
    public async Task A_SimplePeriod()
    {
        Processor.MinimumPeriodInMilliseconds = 3000;

        var lastVibrationTime = ReferenceDateTime;
        for (var j = 0; j < 31; j++)
        {
            lastVibrationTime = ReferenceDateTime.AddMilliseconds(500 * j);
            await Processor.ProcessVibrationChange(lastVibrationTime, true);
        }

        //Correct start and end
        Assert.That(Processor.CurrentVibrationPeriod?.StartedOn, Is.EqualTo(ReferenceDateTime));
        Assert.That(Processor.LastVibrationTime, Is.EqualTo(lastVibrationTime));

        //Add a stop after the MinimumPeriodInMilliseconds
        var noVibrationTime = Processor.LastVibrationTime!.Value.AddSeconds(Processor.MinimumPeriodInMilliseconds + 1000);

        await Processor.ProcessVibrationChange(noVibrationTime, false);

        //If a period ends - as it should above - the CurrentVibrationPeriod should be null
        Assert.That(Processor.CurrentVibrationPeriod, Is.Null);

        var lastEntry = await VibrationMonitorDbQuery.LastGreyWaterPumpVibration(Processor.DbFileName);

        Assert.That(lastEntry?.StartedOn, Is.EqualTo(ReferenceDateTime));
        Assert.That(lastEntry?.EndedOn, Is.EqualTo(lastVibrationTime));

        ReferenceDateTime = noVibrationTime.AddMinutes(1);
    }

    [Test]
    public async Task B_MixedPeriod()
    {
        Processor.MinimumPeriodInMilliseconds = 4000;

        var currentIsVibrating = true;
        var lastVibrationTime = ReferenceDateTime;

        for (var j = 0; j < 31; j++)
        {
            var loopTime = ReferenceDateTime.AddMilliseconds(500 * j);
            if (currentIsVibrating) lastVibrationTime = loopTime;
            await Processor.ProcessVibrationChange(lastVibrationTime, currentIsVibrating);
            currentIsVibrating = !currentIsVibrating;
        }

        //Correct start and end
        Assert.That(Processor.CurrentVibrationPeriod?.StartedOn, Is.EqualTo(ReferenceDateTime));
        Assert.That(Processor.LastVibrationTime, Is.EqualTo(lastVibrationTime));

        //Check that no processing occurs within the MinimumPeriodInMilliseconds
        var noVibrationTime = Processor.LastVibrationTime!.Value.AddSeconds(2);

        await Processor.ProcessVibrationChange(noVibrationTime, false);

        Assert.That(Processor.CurrentVibrationPeriod?.StartedOn, Is.EqualTo(ReferenceDateTime));
        Assert.That(Processor.CurrentVibrationPeriod, Is.Not.Null);


        noVibrationTime = noVibrationTime.AddSeconds(1);

        await Processor.ProcessVibrationChange(noVibrationTime, false);

        Assert.That(Processor.CurrentVibrationPeriod?.StartedOn, Is.EqualTo(ReferenceDateTime));
        Assert.That(Processor.CurrentVibrationPeriod, !Is.Null);

        //Add a stop after the MinimumPeriodInMilliseconds
        noVibrationTime = noVibrationTime.AddSeconds(1);

        await Processor.ProcessVibrationChange(noVibrationTime, false);

        //If a period ends - as it should above - the CurrentVibrationPeriod should be null
        Assert.That(Processor.CurrentVibrationPeriod, Is.Null);

        var lastEntry = await VibrationMonitorDbQuery.LastGreyWaterPumpVibration(Processor.DbFileName);

        Assert.That(lastEntry?.StartedOn, Is.EqualTo(ReferenceDateTime));
        Assert.That(lastEntry?.EndedOn, Is.EqualTo(lastVibrationTime));

        ReferenceDateTime = noVibrationTime.AddMinutes(1);
    }

    [Test]
    public async Task C_BelowMinimumPeriodIsIgnored()
    {
        Processor.MinimumPeriodInMilliseconds = 2000;

        var lastVibrationTime = ReferenceDateTime;

        for (var j = 0; j < 7; j++)
        {
            var loopTime = ReferenceDateTime.AddMilliseconds(250 * j);
            lastVibrationTime = loopTime;
            await Processor.ProcessVibrationChange(lastVibrationTime, true);
        }

        //Correct start and end
        Assert.That(Processor.CurrentVibrationPeriod?.StartedOn, Is.EqualTo(ReferenceDateTime));
        Assert.That(Processor.LastVibrationTime, Is.EqualTo(lastVibrationTime));

        //Add a stop - but still below the MinimumPeriodInMilliseconds
        var noVibrationTime = Processor.LastVibrationTime!.Value.AddMilliseconds(250);
        await Processor.ProcessVibrationChange(noVibrationTime, false);

        Assert.That(Processor.CurrentVibrationPeriod?.StartedOn, Is.EqualTo(ReferenceDateTime));
        Assert.That(Processor.LastVibrationTime, Is.EqualTo(lastVibrationTime));

        //Add a stop beyond the MinimumPeriodInMilliseconds - vibration start and end are below the MinimumPeriodInMilliseconds
        noVibrationTime = noVibrationTime.AddSeconds(2);
        await Processor.ProcessVibrationChange(noVibrationTime, false);

        Assert.That(Processor.CurrentVibrationPeriod, Is.Null);
        Assert.That(Processor.LastVibrationTime, Is.Null);

        ReferenceDateTime = noVibrationTime.AddMinutes(1);
    }

    [Test]
    public async Task Z_SqliteErrorLog()
    {
        var randomString = LogTools.RandomString(10);
        Log.Error("Test Error {0}", randomString);
        await Task.Delay(1000);
        await Log.CloseAndFlushAsync();

        var errorDb = await ErrorDbContext.CreateInstance(LocationTools.ErrorDbFilename());

        var lastError = await errorDb.ErrorLogs.OrderByDescending(x => x.TimeStamp).FirstOrDefaultAsync();
        Assert.That(lastError?.RenderedMessage is not null && lastError.RenderedMessage.Contains(randomString));
    }
}
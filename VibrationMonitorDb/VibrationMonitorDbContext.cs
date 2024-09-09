using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;

namespace VibrationMonitorDb;

public class VibrationMonitorDbContext(DbContextOptions<VibrationMonitorDbContext> options) : DbContext(options)
{
    public DbSet<VibrationPeriod> GreyWaterPumpVibrations { get; set; }

    public static Task<VibrationMonitorDbContext> CreateInstance(string fileName)
    {
        // https://github.com/aspnet/EntityFrameworkCore/issues/9994#issuecomment-508588678
        Batteries_V2.Init();
        raw.sqlite3_config(2 /*SQLITE_CONFIG_MULTITHREAD*/);
        var optionsBuilder = new DbContextOptionsBuilder<VibrationMonitorDbContext>();

        optionsBuilder.LogTo(message => Debug.WriteLine(message));

        return Task.FromResult(new VibrationMonitorDbContext(optionsBuilder
            .UseSqlite($"Data Source={fileName}").Options));
    }

    public static async Task<VibrationMonitorDbContext> CreateInstanceWithEnsureCreated(string fileName)
    {
        var context = await CreateInstance(fileName);
        await context.Database.EnsureCreatedAsync();

        return context;
    }
}
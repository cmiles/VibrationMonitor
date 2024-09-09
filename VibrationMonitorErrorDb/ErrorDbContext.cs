using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;

namespace VibrationMonitorErrorDb;

public class ErrorDbContext(DbContextOptions<ErrorDbContext> options) : DbContext(options)
{
    public DbSet<ErrorLog> ErrorLogs { get; set; }

    public static Task<ErrorDbContext> CreateInstance(string fileName)
    {
        // https://github.com/aspnet/EntityFrameworkCore/issues/9994#issuecomment-508588678
        Batteries_V2.Init();
        raw.sqlite3_config(2 /*SQLITE_CONFIG_MULTITHREAD*/);
        var optionsBuilder = new DbContextOptionsBuilder<ErrorDbContext>();

        optionsBuilder.LogTo(message => Debug.WriteLine(message));

        return Task.FromResult(new ErrorDbContext(optionsBuilder
            .UseSqlite($"Data Source={fileName};Mode=ReadOnly;").Options));
    }

    public static async Task<ErrorDbContext> CreateInstanceWithEnsureCreated(string fileName)
    {
        var context = await CreateInstance(fileName);
        await context.Database.EnsureCreatedAsync();

        return context;
    }
}
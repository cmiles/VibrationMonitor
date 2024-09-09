using Microsoft.EntityFrameworkCore;

namespace VibrationMonitorErrorDb;

public static class ErrorDbQuery
{
    public static async Task<List<ErrorLog>> LastNErrorLogs(string databaseName, int count)
    {
        var db = await ErrorDbContext.CreateInstance(databaseName);
        return await db.ErrorLogs.OrderByDescending(v => v.TimeStamp).Take(count).ToListAsync();
    }

    public static async Task<ErrorLog?> LastErrorLog(string databaseName)
    {
        var db = await ErrorDbContext.CreateInstance(databaseName);
        return await db.ErrorLogs.OrderByDescending(v => v.TimeStamp).FirstOrDefaultAsync();
    }
}
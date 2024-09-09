using Serilog;
using VibrationMonitorDb;
using VibrationMonitorErrorDb;
using VibrationMonitorUtilities;

LogTools.StandardStaticLoggerForProgramDirectory("VibrationMonitorApi");

try
{
    var port = 7171;

    if (args.Any() && int.TryParse(args[0], out var newPort))
    {
        port = newPort;
        Log.Information("Vibration Monitor API: Using User Specified Port: {0}", port);
    }
    else
    {
        Log.Information("Vibration Monitor API: Using the Default Port: {0}", port);
    }

    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddSerilog();

    var url = $"http://*:{port}";

    builder.WebHost.ConfigureKestrel((context, options) => { options.AllowSynchronousIO = true; }).UseUrls(url);
    //builder.Services.Configure<HostFilteringOptions>(x => x.AllowedHosts.Add(new Uri(url).Host));

    var app = builder.Build();

    app.UseSwagger(x => { x.RouteTemplate = "/{documentname}/swagger.json"; });

    app.UseSwaggerUI(x =>
    {
        x.SwaggerEndpoint("/v1/swagger.json", "Vibration Monitor API");
        x.RoutePrefix = string.Empty;
    });

    //app.UseHttpsRedirection();

    Log.Information("Vibration Monitor Database {databaseFile}", LocationTools.DataDbFilename());

    app.MapGet("/lastvibrationperiod",
            async () => await VibrationMonitorDbQuery.LastGreyWaterPumpVibration(LocationTools.DataDbFilename()))
        .WithName("Last Vibration Period")
        .WithOpenApi();

    app.MapGet("/lastvibrationperiods", async (int count) =>
        {
            var result =
                await VibrationMonitorDbQuery.LastNGreyWaterPumpVibrations(LocationTools.DataDbFilename(), count);
            return Results.Ok(result);
        }).WithName("Last N Vibration Periods")
        .WithOpenApi();

    app.MapGet("/vibrationperiodsbystarttime", async (DateTime startTime, DateTime endTime) =>
        {
            var result =
                await VibrationMonitorDbQuery.GreyWaterVibrationsByStartTime(LocationTools.DataDbFilename(), startTime,
                    endTime);
            return Results.Ok(result);
        }).WithName("Vibration Periods by Start Time")
        .WithOpenApi();

    app.MapGet("/lasterror",
            async () => await ErrorDbQuery.LastErrorLog(LocationTools.ErrorDbFilename()))
        .WithName("Last Error")
        .WithOpenApi();

    app.MapGet("/lasterrors", async (int count) =>
        {
            var result = await ErrorDbQuery.LastNErrorLogs(LocationTools.ErrorDbFilename(), count);
            return Results.Ok(result);
        }).WithName("Last Errors")
        .WithOpenApi();

    app.Run();
    return 0;
}
catch (Exception e)
{
    Log.Fatal(e, "Unhandled exception");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
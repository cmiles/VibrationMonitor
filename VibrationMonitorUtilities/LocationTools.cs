namespace VibrationMonitorUtilities;

public static class LocationTools
{
    public static DirectoryInfo DataDirectory()
    {
        var baseDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        var dataDirectory = new DirectoryInfo(Path.Combine(baseDirectory.Parent!.FullName, "VibrationData"));

        if (!dataDirectory.Exists) dataDirectory.Create();

        return dataDirectory;
    }

    public static string DataDbFilename()
    {
        var dataDirectory = DataDirectory();

        return Path.Combine(dataDirectory.FullName, "vibration-monitor.db");
    }

    public static string ErrorDbFilename()
    {
        var dataDirectory = DataDirectory();

        return Path.Combine(dataDirectory.FullName, "vibration-monitor-error.db");
    }
}
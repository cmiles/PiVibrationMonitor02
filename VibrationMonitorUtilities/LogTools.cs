﻿using System.Reflection;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace VibrationMonitorUtilities;

public static class LogTools
{
    private static readonly Random Random = new();

    public static DateTime? GetEntryAssemblyBuildDate()
    {
        try
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly == null) return null;
            var attribute = entryAssembly.GetCustomAttribute<BuildDateAttribute>();
            return attribute?.DateTime;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null;
    }

    public static LoggerConfiguration LogToConsole(this LoggerConfiguration toConfigure)
    {
        return toConfigure.MinimumLevel.Debug().WriteTo.Console();
    }

    public static LoggerConfiguration LogToFileInProgramDirectory(this LoggerConfiguration toConfigure,
        string fileNameFragment)
    {
        var programDirectory = AppContext.BaseDirectory;

        return toConfigure.MinimumLevel.Verbose().WriteTo.File(new CompactJsonFormatter(),
            Path.Combine(programDirectory, $"1_Log-{fileNameFragment}.json"), rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30).WriteTo.SQLite(LocationTools.ErrorDbFilename(),
            restrictedToMinimumLevel: LogEventLevel.Error, tableName: "ErrorLogs",
            retentionPeriod: TimeSpan.FromDays(60), retentionCheckInterval: TimeSpan.FromDays(1));
    }

    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[Random.Next(s.Length)]).ToArray());
    }

    /// <summary>
    ///     Returns a simple string representation of an object with a Max Depth of 2 to avoid unexpected
    ///     problems and provide generally appropriate output for logging.
    /// </summary>
    /// <param name="toDump"></param>
    /// <returns></returns>
    public static string SafeObjectDump(this object? toDump)
    {
        return toDump is null
            ? "null"
            : ObjectDumper.Dump(toDump, new DumpOptions { MaxLevel = 2, DumpStyle = DumpStyle.Console });
    }

    public static void StandardStaticLoggerForProgramDirectory(string fileNameFragment)
    {
        Log.Logger = new LoggerConfiguration().LogToConsole()
            .LogToFileInProgramDirectory(fileNameFragment).CreateLogger();

        try
        {
            Log.Information($"{GetEntryAssemblyBuildDate()}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}
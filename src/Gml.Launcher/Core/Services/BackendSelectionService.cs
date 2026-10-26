using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Gml.Launcher.Core.Services;

public sealed record BackendOption(string Name, string BaseUrl)
{
    public override string ToString() => Name;
}

public static class BackendSelectionService
{
    private static readonly IReadOnlyList<BackendOption> BackendList =
        Array.AsReadOnly(new[]
        {
            new BackendOption("RU1", "https://gmlfr.vestalis.xyz/"),
            new BackendOption("RU2", "https://gmlfrr.vestalis.xyz/"),
            new BackendOption("PO", "https://201-58-180-2.sslip.io/"),
            new BackendOption("MSK1", "https://msk1.gmlfr.vestalis.xyz/")
        });

    public static IReadOnlyList<BackendOption> Options => BackendList;

    private static string SelectionFilePath
    {
        get
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(root))
                root = AppContext.BaseDirectory;

            return Path.Combine(root, "KatranStalis", "backend-selection.txt");
        }
    }

    public static BackendOption GetSelectedBackend()
    {
        try
        {
            if (File.Exists(SelectionFilePath))
            {
                var savedUrl = File.ReadAllText(SelectionFilePath).Trim();
                var saved = BackendList.FirstOrDefault(
                    option => string.Equals(option.BaseUrl, savedUrl, StringComparison.OrdinalIgnoreCase));

                if (saved is not null)
                    return saved;
            }
        }
        catch (IOException)
        {
            // Fall back to RU1 if the selection file cannot be read.
        }
        catch (UnauthorizedAccessException)
        {
            // Fall back to RU1 if the selection file cannot be read.
        }

        return BackendList[0];
    }

    /// <returns>true when the selected backend changed and was saved.</returns>
    public static bool SaveSelection(BackendOption selected)
    {
        ArgumentNullException.ThrowIfNull(selected);

        var known = BackendList.FirstOrDefault(
            option => string.Equals(option.BaseUrl, selected.BaseUrl, StringComparison.OrdinalIgnoreCase));
        if (known is null)
            throw new ArgumentException("Unknown backend selection.", nameof(selected));

        var current = GetSelectedBackend();
        if (string.Equals(current.BaseUrl, known.BaseUrl, StringComparison.OrdinalIgnoreCase))
            return false;

        var path = SelectionFilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, known.BaseUrl);
        return true;
    }

    public static void RestartApplication()
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
            throw new InvalidOperationException("Could not determine the launcher executable path.");

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = false
        };

        var arguments = Environment.GetCommandLineArgs();
        for (var i = 1; i < arguments.Length; i++)
            startInfo.ArgumentList.Add(arguments[i]);

        if (Process.Start(startInfo) is null)
            throw new InvalidOperationException("Could not restart the launcher.");

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
        else
            Environment.Exit(0);
    }
}

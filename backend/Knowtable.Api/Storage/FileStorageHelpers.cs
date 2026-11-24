
using System.Text;
using System.Text.Json;

using Knowtable.Api.Models;
using Microsoft.Extensions.Configuration;

namespace Knowtable.Api.Storage;

internal static class FileStorageHelpers
{
    internal static JsonSerializerOptions JsonOptions { get; } =
        new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

    internal static string GetRootPath(IConfiguration configuration)
    {
        var configuredPath = configuration["Storage:RootPath"];

        if (!string.IsNullOrWhiteSpace(configuredPath))
            return Path.GetFullPath(configuredPath);

        var documentsPath =
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        if (string.IsNullOrWhiteSpace(documentsPath))
            documentsPath = AppContext.BaseDirectory;

        return Path.Combine(documentsPath, "KnowtableData");
    }

    // Find a project directory by the GUID stored in project.json.
    internal static async Task<string?> FindProjectDirectoryAsync(
        string projectsRoot,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(projectsRoot))
            return null;

        string? matchingDirectory = null;

        var manifestPaths = Directory.EnumerateFiles(
            projectsRoot,
            "project.json",
            SearchOption.AllDirectories);

        foreach (var path in manifestPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var project = await ReadJsonAsync<ProjectDocument>(
                path, cancellationToken);

            if (project?.Id != projectId)
                continue;

            var directory = Path.GetDirectoryName(path)!;

            if (matchingDirectory is not null)
            {
                throw new InvalidDataException(
                    $"Multiple project folders contain ID '{projectId}'.");
            }

            matchingDirectory = directory;
        }

        return matchingDirectory;
    }

    // Validate a project title before using it as a folder name.
    internal static string ValidateProjectFolderName(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "A project title is required.", nameof(title));
        }

        var name = title.Trim();

        const string forbiddenCharacters = "<>:\"/\\|?*";

        if (name is "." or ".." ||
            name.Any(c => char.IsControl(c) ||
                          forbiddenCharacters.Contains(c)) ||
            name.EndsWith('.') ||
            name.EndsWith(' '))
        {
            throw new ArgumentException(
                "The project title contains characters that cannot be used in a folder name.",
                nameof(title));
        }

        var deviceName = name.Split('.')[0];

        var reservedNames = new[]
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5",
            "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5",
            "LPT6", "LPT7", "LPT8", "LPT9"
        };

        if (reservedNames.Contains(
            deviceName, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The project title is a reserved folder name.",
                nameof(title));
        }

        return name;
    }

    // Compare directory paths using the operating system's conventions.
    internal static bool SameDirectory(string first, string second)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return string.Equals(
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(first)),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(second)),
            comparison);
    }

    internal static async Task<T?> ReadJsonAsync<T>(
        string path,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (!File.Exists(path))
            return null;

        var json = await File.ReadAllTextAsync(path, cancellationToken);

        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    internal static async Task WriteJsonAtomicallyAsync<T>(
        string path,
        T value,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);

        await WriteTextAtomicallyAsync(path, json, cancellationToken);
    }

    internal static async Task WriteTextAtomicallyAsync(
        string path,
        string contents,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("Invalid file path.");

        Directory.CreateDirectory(directory);

        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";

        try
        {
            await File.WriteAllTextAsync(
                tempPath,
                contents,
                new UTF8Encoding(false),
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
}


using Knowtable.Api.Models;
using Microsoft.Extensions.Configuration;

namespace Knowtable.Api.Storage;

public sealed class FileProjectStorage : IProjectStorage
{
    private readonly string _projectsRoot;

    public FileProjectStorage(IConfiguration configuration)
    {
        _projectsRoot = Path.Combine(
            FileStorageHelpers.GetRootPath(configuration),
            "Projects");

        Directory.CreateDirectory(_projectsRoot);
    }

    public async Task<IReadOnlyList<ProjectDocument>> ListProjectsAsync(
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_projectsRoot))
            return Array.Empty<ProjectDocument>();

        var projects = new List<ProjectDocument>();

        var manifestPaths = Directory.EnumerateFiles(
            _projectsRoot,
            "project.json",
            SearchOption.AllDirectories);

        foreach (var path in manifestPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var project = await FileStorageHelpers.ReadJsonAsync<ProjectDocument>(
                path, cancellationToken);

            if (project is not null)
                projects.Add(project);
        }

        return projects
            .OrderByDescending(project => project.SavedAtUtc)
            .ToList();
    }

    public async Task<ProjectDocument?> LoadProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException(
                "Project ID cannot be empty.",
                nameof(projectId));
        }

        var directory = await FileStorageHelpers.FindProjectDirectoryAsync(
            _projectsRoot, projectId, cancellationToken);

        if (directory is null)
            return null;

        var path = Path.Combine(directory, "project.json");

        var project = await FileStorageHelpers.ReadJsonAsync<ProjectDocument>(
            path, cancellationToken);

        if (project is not null && project.Id != projectId)
        {
            throw new InvalidDataException(
                $"Project ID in '{path}' does not match the requested ID.");
        }

        return project;
    }

    public async Task StoreProjectAsync(
        ProjectDocument project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        if (project.Id == Guid.Empty)
        {
            throw new ArgumentException(
                "Project ID cannot be empty.",
                nameof(project));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var currentDirectory =
            await FileStorageHelpers.FindProjectDirectoryAsync(
                _projectsRoot, project.Id, cancellationToken);

        string destinationDirectory;

        if (string.IsNullOrWhiteSpace(project.Title))
        {
            // Preserve an existing legacy folder until it is given a title.
            if (currentDirectory is null)
            {
                throw new ArgumentException(
                    "A project title is required for new projects.",
                    nameof(project));
            }

            destinationDirectory = currentDirectory;
        }
        else
        {
            project.Title =
                FileStorageHelpers.ValidateProjectFolderName(project.Title);

            // Preserve the existing parent directory when renaming.
            var parentDirectory = currentDirectory is null
                ? _projectsRoot
                : Path.GetDirectoryName(currentDirectory)!;

            destinationDirectory = Path.Combine(
                parentDirectory, project.Title);

            // Never overwrite another folder or file.
            if ((Directory.Exists(destinationDirectory) ||
                 File.Exists(destinationDirectory)) &&
                (currentDirectory is null ||
                 !FileStorageHelpers.SameDirectory(
                     currentDirectory, destinationDirectory)))
            {
                throw new IOException(
                    $"A folder or file named '{project.Title}' already exists.");
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Rename an existing project folder when necessary.
        if (currentDirectory is not null &&
            !FileStorageHelpers.SameDirectory(
                currentDirectory, destinationDirectory))
        {
            Directory.Move(currentDirectory, destinationDirectory);
        }

        Directory.CreateDirectory(destinationDirectory);

        project.SavedAtUtc = DateTimeOffset.UtcNow;

        var manifestPath = Path.Combine(
            destinationDirectory, "project.json");

        await FileStorageHelpers.WriteJsonAtomicallyAsync(
            manifestPath, project, cancellationToken);
    }
}

namespace Knowtable.Api.Models;

public sealed class ProjectDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTimeOffset SavedAtUtc { get; set; }

    public List<CardDto> Cards { get; set; } = new();

    public List<string> Tags { get; set; } = new();
}
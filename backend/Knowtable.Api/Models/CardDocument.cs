using System.Text.Json;

namespace Knowtable.Api.Models;

public sealed class CardDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public JsonElement? FrontContent { get; set; }

    public CardMetadataDocument Metadata { get; set; } = new();

    public DateTimeOffset SavedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CardMetadataDocument
{
    public List<string> TagIds { get; set; } = new();
}
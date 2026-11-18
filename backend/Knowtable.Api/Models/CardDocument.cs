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
    public Guid CardId { get; set; }

    public List<string> TagIds { get; set; } = new();

    public SemanticNodeMetadata Node { get; set; } = new();
}

public sealed class SemanticNodeMetadata
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Type { get; set; } = "content";

    public EmbeddingMetadata? Embedding { get; set; }
}

public sealed class EmbeddingMetadata
{
    public string Model { get; set; } = string.Empty;

    public string? ModelDigest { get; set; }

    public int Dimensions { get; set; }

    public string InputHash { get; set; } = string.Empty;

    public float[] Vector { get; set; } = Array.Empty<float>();
}
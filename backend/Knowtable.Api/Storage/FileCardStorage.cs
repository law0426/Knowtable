using System.Text.Json;
using System.Text.Json.Serialization;
using Knowtable.Api.Models;
using Microsoft.Extensions.Configuration;

namespace Knowtable.Api.Storage;

public sealed class FileCardStorage : ICardStorage
{
    private readonly string _rootPath;

    public FileCardStorage(IConfiguration configuration)
    {
        _rootPath = FileStorageHelpers.GetRootPath(configuration);
    }

    public Task<CardDocument?> LoadSavedCardAsync(
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(
            _rootPath, "Cards", cardId.ToString("D"));

        return LoadCardAsync(path, cardId, cancellationToken);
    }

    public Task StoreSavedCardAsync(
        CardDocument card,
        CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(
            _rootPath, "Cards", card.Id.ToString("D"));

        return StoreCardAsync(path, card, cancellationToken);
    }

    public Task<CardDocument?> LoadUnsavedCardAsync(
        Guid projectId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(
            _rootPath,
            "Projects",
            projectId.ToString("D"),
            "Cards",
            cardId.ToString("D"));

        return LoadCardAsync(path, cardId, cancellationToken);
    }

    public Task StoreUnsavedCardAsync(
        Guid projectId,
        CardDocument card,
        CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(
            _rootPath,
            "Projects",
            projectId.ToString("D"),
            "Cards",
            card.Id.ToString("D"));

        return StoreCardAsync(path, card, cancellationToken);
    }

    private static async Task<CardDocument?> LoadCardAsync(
        string directory,
        Guid cardId,
        CancellationToken cancellationToken)
    {
        var frontPath = Path.Combine(directory, "front.json");
        var metadataPath = Path.Combine(directory, "metadata.json");

        if (!File.Exists(frontPath) && !File.Exists(metadataPath))
            return null;

        var frontFile =
            await FileStorageHelpers.ReadJsonAsync<CardFrontFile>(
                frontPath,
                cancellationToken);

        var metadataFile =
            await FileStorageHelpers.ReadJsonAsync<CardMetadataFile>(
                metadataPath,
                cancellationToken);

        if (metadataFile?.CardId is Guid storedId &&
            storedId != Guid.Empty &&
            storedId != cardId)
        {
            throw new InvalidDataException(
                $"Metadata belongs to a different card than '{cardId}'.");
        }

        var timestamp =
            frontFile?.Metadata?.SavedAtUtc ??
            metadataFile?.Metadata?.SavedAtUtc ??
            GetFallbackTimestamp(frontPath, metadataPath);

        return new CardDocument
        {
            Id = cardId,
            FrontContent = frontFile?.FrontContent is { } content
                ? content.Clone()
                : null,
            Metadata = new CardMetadataDocument
            {
                CardId = cardId,
                TagIds = metadataFile?.TagIds ?? new(),
                Node = metadataFile?.Node ?? new SemanticNodeMetadata()
            },
            SavedAtUtc = timestamp
        };
    }

    private static async Task StoreCardAsync(
        string directory,
        CardDocument card,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(card);

        if (card.Id == Guid.Empty)
            throw new ArgumentException(
                "Card ID cannot be empty.", nameof(card));

        var timestamp = card.SavedAtUtc;

        var frontPath = Path.Combine(directory, "front.json");
        var metadataPath = Path.Combine(directory, "metadata.json");

        // Store card content.
        var frontFile = new CardFrontFile
        {
            Metadata = new CardFileMetadata
            {
                SavedAtUtc = timestamp
            },
            FrontContent = card.FrontContent is { } content
                ? content.Clone()
                : null
        };

        await FileStorageHelpers.WriteJsonAtomicallyAsync(
            frontPath,
            frontFile,
            cancellationToken);

        // Prepare card metadata.
        var metadata = card.Metadata;

        metadata.CardId = card.Id;

        if (metadata.Node is null)
            metadata.Node = new SemanticNodeMetadata();

        if (metadata.Node.Id == Guid.Empty)
            metadata.Node.Id = Guid.NewGuid();

        // Store metadata, including semantic node and embedding.
        var metadataFile = new CardMetadataFile
        {
            Metadata = new CardFileMetadata
            {
                SavedAtUtc = timestamp
            },
            CardId = metadata.CardId,
            TagIds = metadata.TagIds,
            Node = metadata.Node
        };

        await FileStorageHelpers.WriteJsonAtomicallyAsync(
            metadataPath,
            metadataFile,
            cancellationToken);
    }

    private static DateTimeOffset GetFallbackTimestamp(
        string frontPath,
        string metadataPath)
    {
        var existingPaths = new[] { frontPath, metadataPath }
            .Where(File.Exists)
            .ToArray();

        return existingPaths
            .Select(path =>
                new DateTimeOffset(File.GetLastWriteTimeUtc(path)))
            .Max();
    }

    private sealed class CardFrontFile
    {
        [JsonPropertyName("_meta")]
        public CardFileMetadata? Metadata { get; set; }

        public JsonElement? FrontContent { get; set; }
    }

    private sealed class CardMetadataFile
{
    [JsonPropertyName("_meta")]
    public CardFileMetadata? Metadata { get; set; }

    public Guid CardId { get; set; }

    public List<string> TagIds { get; set; } = new();

    public SemanticNodeMetadata Node { get; set; } = new();
}

    private sealed class CardFileMetadata
    {
        public DateTimeOffset? SavedAtUtc { get; set; }
    }
}
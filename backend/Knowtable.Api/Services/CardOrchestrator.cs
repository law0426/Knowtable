
using System.Text.Json;
using System.Text.Json.Nodes;

using Knowtable.Api.Models;
using Knowtable.Api.Storage;

namespace Knowtable.Api.Services;

public sealed class CardOrchestrator
{
    private readonly IProjectStorage _projectStorage;
    private readonly ICardStorage _cardStorage;

    private static readonly Guid WelcomeCardId =
        Guid.Parse("d5fba12a-8a5e-4e0e-9e3a-7a38cf67c442");

    public CardOrchestrator(
        IProjectStorage projectStorage,
        ICardStorage cardStorage)
    {
        _projectStorage = projectStorage;
        _cardStorage = cardStorage;
    }

    public async Task<CardDto?> CreateCardAsync(
        Guid projectId,
        double x,
        double y,
        CancellationToken cancellationToken = default)
    {
        var project = await _projectStorage.LoadProjectAsync(
            projectId, cancellationToken);

        if (project is null)
            return null;

        var timestamp = DateTimeOffset.UtcNow;
        var cardId = Guid.NewGuid();

        var card = new CardDocument
        {
            Id = cardId,
            FrontContent = CreateTextDocument("New card"),
            Metadata = new CardMetadataDocument
            {
                CardId = cardId
            },
            SavedAtUtc = timestamp
        };

        await _cardStorage.StoreSavedCardAsync(card, cancellationToken);

        await _cardStorage.StoreUnsavedCardAsync(
            projectId, card, cancellationToken);

        var geometry = new CardGeometry
        {
            X = x,
            Y = y
        };

        project.Cards.Add(new CardInstance
        {
            CardId = card.Id,
            Geometry = geometry
        });

        await _projectStorage.StoreProjectAsync(
            project, cancellationToken);

        return ToDto(card, geometry);
    }

    public async Task<CardDocument> CreateWorkingCopyAsync(
        Guid projectId,
        CardDocument source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        var workingCopy = CreateWorkingCopy(source);

        await _cardStorage.StoreUnsavedCardAsync(
            projectId, workingCopy, cancellationToken);

        return workingCopy;
    }

    public async Task<CardDocument> LoadProjectCardAsync(
        Guid projectId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        var card = await _cardStorage.LoadUnsavedCardAsync(
            projectId, cardId, cancellationToken);

        if (card is not null)
            return card;

        card = await _cardStorage.LoadSavedCardAsync(
            cardId, cancellationToken);

        if (card is null)
        {
            throw new InvalidDataException(
                $"Card '{cardId}' has no saved file.");
        }

        card.SavedAtUtc = DateTimeOffset.UtcNow;

        await _cardStorage.StoreUnsavedCardAsync(
            projectId, card, cancellationToken);

        return card;
    }

    public async Task<CardDto> AutosaveCardAsync(
        Guid projectId,
        CardDto card,
        CancellationToken cancellationToken = default)
    {
        ValidateCard(projectId, card);

        var existingCard = await _cardStorage.LoadUnsavedCardAsync(
            projectId, card.Id, cancellationToken);

        existingCard ??= await _cardStorage.LoadSavedCardAsync(
            card.Id, cancellationToken);

        var document = ToDocument(
            card, DateTimeOffset.UtcNow, existingCard);

        await _cardStorage.StoreUnsavedCardAsync(
            projectId, document, cancellationToken);

        card.SavedAtUtc = document.SavedAtUtc;

        return card;
    }

    public async Task<CardDto> SaveCardAsync(
        Guid projectId,
        CardDto card,
        CancellationToken cancellationToken = default)
    {
        ValidateCard(projectId, card);

        var existingCard = await _cardStorage.LoadUnsavedCardAsync(
            projectId, card.Id, cancellationToken);

        existingCard ??= await _cardStorage.LoadSavedCardAsync(
            card.Id, cancellationToken);

        var document = ToDocument(
            card, DateTimeOffset.UtcNow, existingCard);

        await _cardStorage.StoreUnsavedCardAsync(
            projectId, document, cancellationToken);

        await _cardStorage.StoreSavedCardAsync(
            document, cancellationToken);

        card.SavedAtUtc = document.SavedAtUtc;

        return card;
    }

    public async Task<CardDto> RevertCardAsync(
        Guid projectId,
        CardDto currentCard,
        CancellationToken cancellationToken = default)
    {
        ValidateCard(projectId, currentCard);

        var savedCard = await _cardStorage.LoadSavedCardAsync(
            currentCard.Id, cancellationToken);

        if (savedCard is null)
        {
            throw new FileNotFoundException(
                $"Saved card '{currentCard.Id}' was not found.");
        }

        var revertedCard = CreateWorkingCopy(savedCard);
        revertedCard.SavedAtUtc = DateTimeOffset.UtcNow;

        await _cardStorage.StoreUnsavedCardAsync(
            projectId, revertedCard, cancellationToken);

        return ToDto(
            revertedCard,
            new CardGeometry
            {
                X = currentCard.X,
                Y = currentCard.Y,
                Width = currentCard.Width,
                Height = currentCard.Height
            });
    }

    public CardDto ToDto(CardDocument card, CardGeometry geometry)
    {
        return new CardDto
        {
            Id = card.Id,
            FrontContent = CloneContent(card.FrontContent),
            TagIds = card.Metadata.TagIds.ToList(),
            X = geometry.X,
            Y = geometry.Y,
            Width = geometry.Width,
            Height = geometry.Height,
            SavedAtUtc = card.SavedAtUtc
        };
    }

    private static CardDocument CreateWorkingCopy(CardDocument source)
    {
        var metadata = CloneMetadata(source.Metadata);
        metadata.CardId = source.Id;

        return new CardDocument
        {
            Id = source.Id,
            FrontContent = CloneContent(source.FrontContent),
            Metadata = metadata,
            SavedAtUtc = source.SavedAtUtc
        };
    }

    private static CardDocument ToDocument(
        CardDto card,
        DateTimeOffset savedAtUtc,
        CardDocument? existingCard)
    {
        var metadata = CloneMetadata(existingCard?.Metadata);

        metadata.CardId = card.Id;
        metadata.TagIds = card.TagIds.ToList();

        return new CardDocument
        {
            Id = card.Id,
            FrontContent = CloneContent(card.FrontContent),
            Metadata = metadata,
            SavedAtUtc = savedAtUtc
        };
    }

    private static CardMetadataDocument CloneMetadata(
        CardMetadataDocument? metadata)
    {
        var sourceNode = metadata?.Node;
        var sourceEmbedding = sourceNode?.Embedding;

        return new CardMetadataDocument
        {
            CardId = metadata?.CardId ?? Guid.Empty,
            TagIds = metadata?.TagIds?.ToList() ?? new(),
            Node = new SemanticNodeMetadata
            {
                Id = sourceNode?.Id is Guid nodeId &&
                     nodeId != Guid.Empty
                    ? nodeId
                    : Guid.NewGuid(),

                Type = string.IsNullOrWhiteSpace(sourceNode?.Type)
                    ? "content"
                    : sourceNode.Type,

                Embedding = sourceEmbedding is null
                    ? null
                    : new EmbeddingMetadata
                    {
                        Model = sourceEmbedding.Model,
                        ModelDigest = sourceEmbedding.ModelDigest,
                        Dimensions = sourceEmbedding.Dimensions,
                        InputHash = sourceEmbedding.InputHash,
                        Vector = sourceEmbedding.Vector?.ToArray()
                            ?? Array.Empty<float>()
                    }
            }
        };
    }

    private static void ValidateCard(Guid projectId, CardDto card)
    {
        ArgumentNullException.ThrowIfNull(card);

        if (projectId == Guid.Empty)
            throw new ArgumentException("Project ID cannot be empty.");

        if (card.Id == Guid.Empty)
            throw new ArgumentException("Card ID cannot be empty.");
    }

    private static JsonElement? CloneContent(JsonElement? content)
    {
        if (content is null ||
            content.Value.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        return content.Value.Clone();
    }

    private static JsonElement CreateTextDocument(string text)
    {
        var paragraphs = new JsonArray();

        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            var paragraph = new JsonObject
            {
                ["type"] = "paragraph"
            };

            if (line.Length > 0)
            {
                paragraph["content"] = new JsonArray(
                    new JsonObject
                    {
                        ["type"] = "text",
                        ["text"] = line
                    });
            }

            paragraphs.Add(paragraph);
        }

        return JsonSerializer.SerializeToElement(new JsonObject
        {
            ["type"] = "doc",
            ["content"] = paragraphs
        });
    }

    public async Task<CardDocument> GetOrCreateWelcomeCardAsync(
        CancellationToken cancellationToken = default)
    {
        var card = await _cardStorage.LoadSavedCardAsync(
            WelcomeCardId, cancellationToken);

        if (card?.FrontContent is JsonElement content &&
            content.ValueKind == JsonValueKind.Object)
        {
            card.Metadata ??= new CardMetadataDocument();
            card.Metadata.CardId = card.Id;

            return card;
        }

        card ??= new CardDocument
        {
            Id = WelcomeCardId,
            Metadata = new CardMetadataDocument
            {
                CardId = WelcomeCardId
            }
        };

        card.Metadata ??= new CardMetadataDocument();
        card.Metadata.CardId = card.Id;

        card.FrontContent = await LoadWelcomeCardContentAsync(
            cancellationToken);

        card.SavedAtUtc = DateTimeOffset.UtcNow;

        await _cardStorage.StoreSavedCardAsync(
            card, cancellationToken);

        return card;
    }

    private static async Task<JsonElement> LoadWelcomeCardContentAsync(
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Templates",
            "welcome-card.json");

        await using var stream = File.OpenRead(path);

        using var document = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken);

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException(
                "The welcome-card template must contain a JSON object.");
        }

        return document.RootElement.Clone();
    }
}

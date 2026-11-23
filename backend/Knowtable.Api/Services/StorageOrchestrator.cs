
using Knowtable.Api.Models;
using Knowtable.Api.Storage;

namespace Knowtable.Api.Services;

public sealed class StorageOrchestrator
{
    private readonly IProjectStorage _projectStorage;
    private readonly CardOrchestrator _cardOrchestrator;

    public StorageOrchestrator(
        IProjectStorage projectStorage,
        CardOrchestrator cardOrchestrator)
    {
        _projectStorage = projectStorage;
        _cardOrchestrator = cardOrchestrator;
    }

    public async Task<ProjectDto> LoadDraftProjectAsync(
        CancellationToken cancellationToken = default)
    {
        var welcomeCard = await _cardOrchestrator.GetOrCreateWelcomeCardAsync(
            cancellationToken);

        return new ProjectDto
        {
            Id = Guid.Empty,
            SavedAtUtc = DateTimeOffset.UtcNow,
            Cards =
            {
                _cardOrchestrator.ToDto(
                    welcomeCard, GetWelcomeGeometry())
            }
        };
    }

    public async Task<ProjectDto> CreateProjectAsync(
        CancellationToken cancellationToken = default)
    {
        var welcomeCard = await _cardOrchestrator.GetOrCreateWelcomeCardAsync(
            cancellationToken);

        var geometry = GetWelcomeGeometry();

        var project = new ProjectDocument
        {
            Id = Guid.NewGuid(),
            Cards =
            {
                new CardInstance
                {
                    CardId = welcomeCard.Id,
                    Geometry = geometry
                }
            }
        };

        var workingCopy = await _cardOrchestrator.CreateWorkingCopyAsync(
            project.Id, welcomeCard, cancellationToken);

        await _projectStorage.StoreProjectAsync(
            project, cancellationToken);

        return new ProjectDto
        {
            Id = project.Id,
            SavedAtUtc = project.SavedAtUtc,
            Cards =
            {
                _cardOrchestrator.ToDto(workingCopy, geometry)
            }
        };
    }

    public Task<IReadOnlyList<ProjectDocument>> ListProjectsAsync(
        CancellationToken cancellationToken = default)
    {
        return _projectStorage.ListProjectsAsync(cancellationToken);
    }

    public async Task<ProjectDto?> LoadProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var project = await _projectStorage.LoadProjectAsync(
            projectId, cancellationToken);

        if (project is null)
            return null;

        var cards = new List<CardDto>(project.Cards.Count);

        foreach (var instance in project.Cards)
        {
            var card = await _cardOrchestrator.LoadProjectCardAsync(
                projectId, instance.CardId, cancellationToken);

            cards.Add(
                _cardOrchestrator.ToDto(card, instance.Geometry));
        }

        return new ProjectDto
        {
            Id = project.Id,
            SavedAtUtc = project.SavedAtUtc,
            Cards = cards
        };
    }

    public async Task<ProjectDto> SaveDraftProjectAsync(
        ProjectDto draft,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);

        if (draft.Id != Guid.Empty)
        {
            throw new ArgumentException(
                "Only drafts without a project ID can use this operation.",
                nameof(draft));
        }

        draft.Id = Guid.NewGuid();

        return await SaveProjectAsync(draft, cancellationToken);
    }

    public async Task<ProjectDto> SaveProjectAsync(
        ProjectDto project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        if (project.Id == Guid.Empty)
            throw new ArgumentException("Project ID cannot be empty.");

        EnsureUniqueCardIds(project.Cards);

        // Save every card's working and saved copies.
        foreach (var card in project.Cards)
        {
            await _cardOrchestrator.SaveCardAsync(
                project.Id, card, cancellationToken);
        }

        // Save the project manifest and its card positions.
        return await AutosaveProjectLayoutAsync(
            project, cancellationToken);
    }

    public async Task<ProjectDto> AutosaveProjectLayoutAsync(
        ProjectDto project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        if (project.Id == Guid.Empty)
            throw new ArgumentException("Project ID cannot be empty.");

        EnsureUniqueCardIds(project.Cards);

        var document = new ProjectDocument
        {
            Id = project.Id,
            Cards = project.Cards.Select(card => new CardInstance
            {
                CardId = card.Id,
                Geometry = new CardGeometry
                {
                    X = card.X,
                    Y = card.Y,
                    Width = card.Width,
                    Height = card.Height
                }
            }).ToList()
        };

        await _projectStorage.StoreProjectAsync(
            document, cancellationToken);

        project.SavedAtUtc = document.SavedAtUtc;

        return project;
    }

    private static void EnsureUniqueCardIds(List<CardDto> cards)
    {
        if (cards.Select(card => card.Id).Distinct().Count() != cards.Count)
        {
            throw new InvalidOperationException(
                "A project cannot contain duplicate card IDs.");
        }
    }

    private static CardGeometry GetWelcomeGeometry() => new()
    {
        X = 100,
        Y = 100,
        Width = 560,
        Height = 280
    };
}

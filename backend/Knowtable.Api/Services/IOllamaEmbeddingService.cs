using Knowtable.Api.Models;

namespace Knowtable.Api.Services;

public interface IOllamaEmbeddingService
{
    Task<EmbeddingMetadata?> GetOrCreateEmbeddingAsync(
        string text,
        EmbeddingMetadata? existingEmbedding,
        CancellationToken cancellationToken = default);
}
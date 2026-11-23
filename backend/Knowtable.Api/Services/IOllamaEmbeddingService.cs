using Knowtable.Api.Models;

namespace Knowtable.Api.Services;

public interface IOllamaEmbeddingService
{
    Task<EmbeddingMetadata> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);
}
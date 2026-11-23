
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

using Knowtable.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace Knowtable.Api.Services;

public sealed class OllamaEmbeddingService : IOllamaEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _modelName;
    private readonly IMemoryCache _memoryCache;

    public OllamaEmbeddingService(
    HttpClient httpClient,
    IConfiguration configuration,
    IMemoryCache memoryCache)
    {
        _httpClient = httpClient;
        _memoryCache = memoryCache;

        _modelName = configuration["Ollama:Model"]
            ?? throw new InvalidOperationException(
                "Ollama:Model is missing from configuration.");

        if (_httpClient.BaseAddress is null)
        {
            var baseUrl = configuration["Ollama:BaseUrl"]
                ?? "http://localhost:11434/";

            _httpClient.BaseAddress = new Uri(
                baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
        }
    }

    public async Task<EmbeddingMetadata?> GetOrCreateEmbeddingAsync(
        string text,
        EmbeddingMetadata? existingEmbedding,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        // Identify the exact text being embedded.
        var inputHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(text)))
            .ToLowerInvariant();

        // Identify the currently installed model version.
        var modelDigest = await GetModelDigestAsync(
            _modelName, cancellationToken);

        // Reuse the existing vector if both fingerprints match.
        if (IsReusable(existingEmbedding, inputHash, modelDigest))
            return existingEmbedding;

        // Generate a new embedding.
        var request = new OllamaEmbedRequest(_modelName, text);

        using var response = await _httpClient.PostAsJsonAsync(
            "api/embed", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(
                cancellationToken);

            throw new HttpRequestException(
                $"Ollama embedding request failed: {error}",
                null,
                response.StatusCode);
        }

        var result = await response.Content
            .ReadFromJsonAsync<OllamaEmbedResponse>(
                cancellationToken: cancellationToken);

        if (result?.Embeddings is not { Count: 1 })
        {
            throw new InvalidDataException(
                "Ollama did not return exactly one embedding vector.");
        }

        var vector = result.Embeddings[0];

        if (vector.Length == 0 ||
            vector.Any(value => !float.IsFinite(value)))
        {
            throw new InvalidDataException(
                "Ollama returned an invalid embedding vector.");
        }

        return new EmbeddingMetadata
        {
            Model = string.IsNullOrWhiteSpace(result.Model)
                ? _modelName
                : result.Model,

            ModelDigest = modelDigest,
            Dimensions = vector.Length,
            InputHash = inputHash,
            Vector = vector
        };
    }

    private static bool IsReusable(
        EmbeddingMetadata? existing,
        string inputHash,
        string modelDigest)
    {
        return existing is not null
            && existing.Vector is { Length: > 0 }
            && existing.Dimensions == existing.Vector.Length
            && existing.Vector.All(float.IsFinite)
            && existing.InputHash == inputHash
            && existing.ModelDigest == modelDigest;
    }

    private async Task<string> GetModelDigestAsync(
        string modelName,
        CancellationToken cancellationToken)
    {
        var cacheKey =
            $"Ollama:ModelDigest:{_httpClient.BaseAddress}:{modelName}";

        return await _memoryCache.GetOrCreateAsync(
            cacheKey,
            async entry =>
            {

                var models = await _httpClient
                    .GetFromJsonAsync<OllamaTagsResponse>(
                        "api/tags", cancellationToken);

                var model = models?.Models?.FirstOrDefault(item =>
                    string.Equals(item.Name, modelName,
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.Model, modelName,
                        StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrWhiteSpace(model?.Digest))
                {
                    throw new InvalidOperationException(
                        $"Could not retrieve the digest for Ollama model '{modelName}'.");
                }

                return model.Digest;
            }) ?? throw new InvalidOperationException(
                "Could not retrieve the Ollama model digest.");
    }

    private sealed record OllamaEmbedRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] string Input);

    private sealed class OllamaEmbedResponse
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("embeddings")]
        public List<float[]> Embeddings { get; set; } = new();
    }

    private sealed class OllamaTagsResponse
    {
        [JsonPropertyName("models")]
        public List<OllamaModelInfo>? Models { get; set; }
    }

    private sealed class OllamaModelInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("digest")]
        public string Digest { get; set; } = string.Empty;
    }
}

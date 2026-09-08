using System.Net;
using System.Text.Json;
using AutoSale.Application.Abstractions.Integrations;

namespace AutoSale.Infrastructure.Integrations;

public abstract class HttpIntegrationClient
{
    private readonly HttpClient _httpClient;
    private readonly string _serviceKey;

    protected HttpIntegrationClient(HttpClient httpClient, string serviceKey)
    {
        _httpClient = httpClient;
        _serviceKey = serviceKey;
    }

    protected async Task<IntegrationResult<JsonElement>> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.TryAddWithoutValidation("X-Service-Key", _serviceKey);
        try
        {
            using var response = await _httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return IntegrationResult<JsonElement>.Failure(
                    Classify(response.StatusCode),
                    await ReadErrorCodeAsync(response, cancellationToken),
                    $"Integration returned HTTP {(int)response.StatusCode}.",
                    GetRetryAfter(response));
            }

            var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (content.Length == 0)
            {
                return IntegrationResult<JsonElement>.Success(default);
            }

            using var document = JsonDocument.Parse(content);
            return IntegrationResult<JsonElement>.Success(document.RootElement.Clone());
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return IntegrationResult<JsonElement>.Failure(
                IntegrationFailureKind.Transient, "integration_timeout", "Integration request timed out.");
        }
        catch (HttpRequestException)
        {
            return IntegrationResult<JsonElement>.Failure(
                IntegrationFailureKind.Transient, "integration_unavailable", "Integration is unavailable.");
        }
        catch (JsonException)
        {
            return IntegrationResult<JsonElement>.Failure(
                IntegrationFailureKind.InvalidResponse, "integration_invalid_json", "Integration returned invalid JSON.");
        }
    }

    private static IntegrationFailureKind Classify(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.NotFound => IntegrationFailureKind.NotFound,
        HttpStatusCode.Conflict => IntegrationFailureKind.Conflict,
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => IntegrationFailureKind.Unauthorized,
        HttpStatusCode.TooManyRequests => IntegrationFailureKind.Transient,
        >= HttpStatusCode.InternalServerError => IntegrationFailureKind.Transient,
        _ => IntegrationFailureKind.InvalidResponse
    };

    private static async Task<string> ReadErrorCodeAsync(HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (document.RootElement.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String)
            {
                return code.GetString() ?? $"http_{(int)response.StatusCode}";
            }

            if (document.RootElement.TryGetProperty("extensions", out var extensions) &&
                extensions.TryGetProperty("code", out code) && code.ValueKind == JsonValueKind.String)
            {
                return code.GetString() ?? $"http_{(int)response.StatusCode}";
            }
        }
        catch (JsonException)
        {
            // Error bodies are deliberately not propagated.
        }

        return $"http_{(int)response.StatusCode}";
    }

    private static TimeSpan? GetRetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is not null)
        {
            return retryAfter.Delta;
        }

        return retryAfter?.Date is null ? null : retryAfter.Date.Value - DateTimeOffset.UtcNow;
    }
}

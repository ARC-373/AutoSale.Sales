using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoSale.Payments.Mock.Contracts;
using AutoSale.Payments.Mock.Payments;
using Microsoft.Extensions.Options;

namespace AutoSale.Payments.Mock.Integrations;

public sealed class SalesWebhookClient(HttpClient httpClient, IOptions<SalesWebhookOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly SalesWebhookOptions _options = options.Value;

    public async Task DeliverAsync(Payment payment, CancellationToken cancellationToken)
    {
        if (payment.EventId is null || payment.OccurredAtUtc is null || payment.Status == PaymentStatus.Pending)
        {
            throw new InvalidOperationException("Only decided payments can be delivered.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/payments/webhook")
        {
            Content = JsonContent.Create(new PaymentWebhookRequest(
                payment.PaymentCode, payment.EventId.Value, payment.Status, payment.OccurredAtUtc.Value),
                options: JsonOptions)
        };
        request.Headers.TryAddWithoutValidation("X-Payment-Webhook-Key", _options.WebhookKey);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Sales webhook returned HTTP {(int)response.StatusCode}.", null,
                response.StatusCode);
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace AutoSale.Payments.Mock.Integrations;

public sealed class SalesWebhookOptions
{
    public const string SectionName = "Integrations:Sales";

    [Required, Url]
    public string BaseUrl { get; init; } = string.Empty;

    [Required]
    public string WebhookKey { get; init; } = string.Empty;

    [Range(1, 60)]
    public int TimeoutSeconds { get; init; } = 5;
}

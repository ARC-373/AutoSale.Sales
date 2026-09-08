namespace AutoSale.Infrastructure.Integrations.Payments;

public sealed class PaymentsOptions
{
    public const string SectionName = "Integrations:Payments";
    public string BaseUrl { get; init; } = string.Empty;
    public string ServiceKey { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 5;
}

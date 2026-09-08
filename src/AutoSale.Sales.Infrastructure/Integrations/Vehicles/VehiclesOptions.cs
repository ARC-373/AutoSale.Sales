namespace AutoSale.Infrastructure.Integrations.Vehicles;

public sealed class VehiclesOptions
{
    public const string SectionName = "Integrations:Vehicles";
    public string BaseUrl { get; init; } = string.Empty;
    public string ServiceKey { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 5;
}

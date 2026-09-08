namespace AutoSale.Infrastructure.BackgroundServices;

public sealed class SaleProcessingOptions
{
    public const string SectionName = "SaleProcessing";
    public int PollIntervalSeconds { get; init; } = 2;
    public int LeaseSeconds { get; init; } = 30;
    public int BatchSize { get; init; } = 10;
}

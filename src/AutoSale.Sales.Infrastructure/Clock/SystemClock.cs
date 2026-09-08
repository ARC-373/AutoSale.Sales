using AutoSale.Application.Abstractions.Clock;

namespace AutoSale.Infrastructure.Clock;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

namespace AutoSale.Application.Sales.ProcessPending;

public static class RetrySchedule
{
    private static readonly TimeSpan[] Delays =
    [
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(60)
    ];

    public static TimeSpan GetDelay(int previousAttempts, TimeSpan? retryAfter)
    {
        if (retryAfter > TimeSpan.Zero)
        {
            return retryAfter.Value;
        }

        var index = Math.Clamp(previousAttempts, 0, Delays.Length - 1);
        return Delays[index];
    }
}

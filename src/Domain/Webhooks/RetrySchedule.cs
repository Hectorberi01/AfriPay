namespace AfriPay.Domain.Webhooks;

/// <summary>
/// Durées de retry exponentielles avec jitter minimal.
/// Mobile Money APIs étant moins fiables, on tolère plus de retries.
/// </summary>
public static class RetrySchedule
{
    private static readonly TimeSpan[] Delays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(30),
    ];
 
    public static int MaxAttempts => Delays.Length + 1;
 
    public static TimeSpan? NextDelay(int attemptCount)
    {
        var index = attemptCount - 1;
        return index >= 0 && index < Delays.Length ? Delays[index] : null;
    }
}
namespace KemiKoll.Application.Tests.TestDoubles;

/// <summary>
/// A clock that always returns the same date, so date rules can be tested exactly.
/// </summary>
/// <param name="today">The date the clock returns (at noon UTC).</param>
internal sealed class FixedTimeProvider(DateOnly today) : TimeProvider
{
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() =>
        new(today.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);
}

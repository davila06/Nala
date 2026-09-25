namespace PawTrack.Infrastructure.Clinics;

public static class ClinicBusinessDate
{
    public static (DateTimeOffset Start, DateTimeOffset End) UtcRange(DateOnly businessDate)
    {
        var localStart = new DateTimeOffset(businessDate.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(-6));
        var start = localStart.ToUniversalTime();
        return (start, start.AddDays(1));
    }
}

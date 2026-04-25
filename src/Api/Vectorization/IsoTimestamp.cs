namespace Rinha.Fraud.Vectorization;

internal static class IsoTimestamp
{
    public static long ParseToUtcTicks(ReadOnlySpan<char> s)
    {
        if (s.Length < 20) return 0;
        if (!TryParseInt(s[..4], out var year)) return 0;
        if (!TryParseInt(s.Slice(5, 2), out var month)) return 0;
        if (!TryParseInt(s.Slice(8, 2), out var day)) return 0;
        if (!TryParseInt(s.Slice(11, 2), out var hour)) return 0;
        if (!TryParseInt(s.Slice(14, 2), out var minute)) return 0;
        if (!TryParseInt(s.Slice(17, 2), out var second)) return 0;

        try
        {
            return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc).Ticks;
        }
        catch (ArgumentOutOfRangeException)
        {
            return 0;
        }
    }

    public static int HourOfDay(ReadOnlySpan<char> s)
    {
        if (s.Length < 13) return 0;
        return TryParseInt(s.Slice(11, 2), out var hour) ? hour : 0;
    }

    public static int DayOfWeekMondayZero(long utcTicks)
    {
        if (utcTicks == 0) return 0;
        // DateTime.DayOfWeek: Sunday=0..Saturday=6. Spec wants: Monday=0..Sunday=6.
        var sunday0 = (int)new DateTime(utcTicks, DateTimeKind.Utc).DayOfWeek;
        return (sunday0 + 6) % 7;
    }

    public static long MinutesBetween(long fromUtcTicks, long toUtcTicks) =>
        (toUtcTicks - fromUtcTicks) / TimeSpan.TicksPerMinute;

    private static bool TryParseInt(ReadOnlySpan<char> s, out int value)
    {
        value = 0;
        foreach (var c in s)
        {
            if ((uint)(c - '0') > 9) return false;
            value = (value * 10) + (c - '0');
        }
        return true;
    }
}

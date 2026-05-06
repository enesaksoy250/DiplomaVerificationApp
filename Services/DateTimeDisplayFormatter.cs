using System.Globalization;

namespace DiplomaVerificationApp.Services;

public static class DateTimeDisplayFormatter
{
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    public static string FromUnixTimestamp(long unixTimestamp)
    {
        var utcDate = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
        return utcDate.ToString("dd MMMM yyyy HH:mm 'UTC'", TurkishCulture);
    }
}

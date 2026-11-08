using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace DntuLab.Api.Services;

/// <summary>Dong ho kiem thu chi hoat dong trong Development.
/// Header: X-Debug-Date (yyyy-MM-dd), X-Debug-Time (HH:mm).
/// </summary>
public static class DemoClock
{
    public static DateTime GetNow(HttpRequest request, IWebHostEnvironment environment)
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GetVietnamTimeZone());
        if (!environment.IsDevelopment()) return now;

        var date = now.Date;
        var time = TimeOnly.FromDateTime(now);
        if (request.Headers.TryGetValue("X-Debug-Date", out var dateHeader) &&
            DateOnly.TryParse(dateHeader.ToString(), out var parsedDate))
            date = parsedDate.ToDateTime(TimeOnly.MinValue);
        if (request.Headers.TryGetValue("X-Debug-Time", out var timeHeader) &&
            TimeOnly.TryParse(timeHeader.ToString(), out var parsedTime))
            time = parsedTime;

        return date.Add(time.ToTimeSpan());
    }

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
    }
}

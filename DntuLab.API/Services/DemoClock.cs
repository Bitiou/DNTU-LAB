using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace DntuLab.Api.Services;

/// <summary>
/// Đồng hồ Demo dùng thống nhất trong Development và Production.
/// Frontend gửi X-Debug-Date (yyyy-MM-dd) và X-Debug-Time (HH:mm).
/// </summary>
public static class DemoClock
{
    public static DateTime GetNow(HttpRequest request, IWebHostEnvironment environment)
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            GetVietnamTimeZone());

        var date = DateOnly.FromDateTime(now);
        var time = TimeOnly.FromDateTime(now);

        // Dùng định dạng ISO chính xác để không phụ thuộc CultureInfo của Render.
        if (request.Headers.TryGetValue("X-Debug-Date", out var dateHeader))
        {
            var rawDate = dateHeader.ToString().Trim();
            if (DateOnly.TryParseExact(
                rawDate,
                new[] { "yyyy-MM-dd", "yyyy-M-d" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate))
            {
                date = parsedDate;
            }
        }

        if (request.Headers.TryGetValue("X-Debug-Time", out var timeHeader))
        {
            var rawTime = timeHeader.ToString().Trim();
            if (TimeOnly.TryParseExact(
                rawTime,
                new[] { "HH:mm", "H:mm", "HH:mm:ss", "H:mm:ss" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedTime))
            {
                time = parsedTime;
            }
        }

        return date.ToDateTime(time);
    }

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
    }
}

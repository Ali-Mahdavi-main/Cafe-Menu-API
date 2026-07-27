using System.Globalization;

namespace CafeMenu.Api.Helpers;

public static class PersianDateHelper
{
    private static readonly PersianCalendar PersianCalendar = new PersianCalendar();

    public static string ToPersianDateString(DateTime date)
    {
        var year = PersianCalendar.GetYear(date);
        var month = PersianCalendar.GetMonth(date);
        var day = PersianCalendar.GetDayOfMonth(date);
        return $"{year:0000}/{month:00}/{day:00}";
    }
}

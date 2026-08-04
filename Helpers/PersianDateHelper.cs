using System;
using System.Globalization;

namespace CafeMenu.Api.Helpers;

public static class PersianDateHelper
{
    private static readonly PersianCalendar PersianCalendar = new();
    private static readonly DateTime MinSupported = PersianCalendar.MinSupportedDateTime;

    public static string ToPersianDateString(DateTime dateTime)
    {
        // If the date is earlier than the Persian calendar can handle, return a placeholder
        if (dateTime < MinSupported)
            return "قبل از هجری شمسی";

        try
        {
            return $"{PersianCalendar.GetYear(dateTime)}/{PersianCalendar.GetMonth(dateTime):D2}/{PersianCalendar.GetDayOfMonth(dateTime):D2}";
        }
        catch (ArgumentOutOfRangeException)
        {
            return "تاریخ نامعتبر";
        }
    }
}
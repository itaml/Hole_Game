using System;

namespace Menu.UI
{
    public static class UiTimeFormat
    {
        public static string FormatMinutesSeconds(TimeSpan t)
        {
            if (t.TotalSeconds <= 0) return "0s";

            int totalSeconds = (int)Math.Ceiling(t.TotalSeconds);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            if (minutes <= 0) return $"{seconds}s";
            return $"{minutes}m {seconds:00}s";
        }

        public static string FormatHMS(TimeSpan t)
        {
            if (t.TotalSeconds <= 0) return "00:00";

            int totalSeconds = (int)Math.Ceiling(t.TotalSeconds);
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;

            if (hours > 0) return $"{hours:00}:{minutes:00}:{seconds:00}";
            return $"{minutes:00}:{seconds:00}";
        }
    }
}
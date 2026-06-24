using ChronosPointer.Api;

namespace ChronosPointer.Core
{
    internal static class ChronosSettingsSnapshotFactory
    {
        public static ChronosSettingsSnapshot Create(ChronosPointerSettings settings)
        {
            settings = settings ?? new ChronosPointerSettings();

            return new ChronosSettingsSnapshot
            {
                DrawArrow = settings.DrawArrow,
                DrawCurrentHourHighlight = settings.DrawCurrentHourHighlight,
                DrawHourBar = settings.DrawHourBar,
                DrawHoursBarCursor = settings.DrawHoursBarCursor,
                DoDynamicHoursBarLine = settings.DoDynamicHoursBarLine,
                DrawMainCursor = settings.DrawMainCursor,
                DoFilledHourHighlight = settings.DoFilledHourHighlight,
                DrawIncidentOverlay = settings.DrawIncidentOverlay,
                DoLoadWarnings = settings.DoLoadWarnings,

                CursorThickness = ChronosPointerSettings.ValidateCursorThickness(settings.CursorThickness),
                HoursBarCursorThickness = ChronosPointerSettings.ValidateCursorThickness(settings.HoursBarCursorThickness),
                HighlightBorderThickness = settings.HighlightBorderThickness,
                AuroraMinOpacity = settings.AuroraMinOpacity,
                AuroraMaxOpacity = settings.AuroraMaxOpacity,
                SunlightThresholdNight = settings.SunlightThreshold_Night,
                SunlightThresholdAny = settings._SunlightThreshold_Any,
                SunlightThresholdDawnDusk = settings.SunlightThreshold_DawnDusk,
                SunlightThresholdSunriseSunset = settings.SunlightThreshold_SunriseSunset,

                ColorArrow = settings.Color_Arrow,
                ColorHourHighlight = settings.Color_HourHighlight,
                ColorMainCursor = settings.Color_MainCursor,
                ColorHoursBarCursorDay = settings.Color_HoursBarCursor_Day,
                ColorHoursBarCursorNight = settings.Color_HoursBarCursor_Night,
                ColorNight = settings.Color_Night,
                ColorDawnDusk = settings.Color_DawnDusk,
                ColorSunriseSunset = settings.Color_SunriseSunset,
                ColorDay = settings.Color_Day,
                ColorVolcanicWinter = settings.Color_VolcanicWinter,
                ColorToxicFallout = settings.Color_ToxicFallout,
                ColorAurora1 = settings.Color_Aurora1,
                ColorAurora2 = settings.Color_Aurora2,
                DefaultTransparentColor = settings._DefaultTransparentColor,
                HighlightInteriorColor = settings._HighlightInteriorColor
            };
        }
    }
}

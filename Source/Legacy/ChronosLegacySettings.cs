using System;
using UnityEngine;

namespace ChronosPointer
{
    public static class Defaults
    {
        public static bool DrawArrow = true;
        public static bool DrawCurrentHourHighlight = true;
        public static bool DrawHourBar = true;
        public static bool DrawHoursBarCursor = true;
        public static bool DoDynamicHoursBarLine = true;
        public static bool DrawMainCursor = true;
        public static bool DoFilledHourHighlight = false;
        public static bool DrawIncidentOverlay = true;
        public static bool DoLoadWarnings = true;

        public static float CursorThickness = 2f;
        public static float HoursBarCursorThickness = 2f;
        public static float HighlightBorderThickness = 2f;
        public static float AuroraMinOpacity = 0.1f;
        public static float AuroraMaxOpacity = 0.75f;
        public static float SunlightThreshold_Night = 0.0f;
        public static float _SunlightThreshold_Any = 0.05f;
        public static float SunlightThreshold_DawnDusk = 0.35f;
        public static float SunlightThreshold_SunriseSunset = 0.7f;

        public static Color Color_Arrow = Color.red;
        public static Color Color_MainCursor = Color.white;
        public static Color Color_HourHighlight = new Color(0.50f, 0.50f, 0.12f, 0.62f);
        public static Color Color_HoursBarCursor_Day = Color.black;
        public static Color Color_HoursBarCursor_Night = Color.white;
        public static Color Color_Night = new Color(0f, 0f, 0.5f);
        public static Color Color_DawnDusk = new Color(0.5f, 0.5f, 1f);
        public static Color Color_SunriseSunset = new Color(1f, 0.5f, 0f);
        public static Color Color_Day = Color.yellow;
        public static Color Color_VolcanicWinter = new Color(0f, 0f, 0f, 0.5f);
        public static Color Color_ToxicFallout = new Color(0f, 1f, 0f, 0.5f);
        public static Color Color_Aurora1 = new Color(1.0f, 0.5f, 1.0f, 1.0f);
        public static Color Color_Aurora2 = new Color(0.5f, 1.0f, 0.5f, 1.0f);

        public static Color _DefaultTransparentColor = new Color(1, 1, 1, 0);
        public static Color _HighlightInteriorColor = new Color(0f, 0f, 0f, 0f);
    }

    public class ChronosPointerSettings
    {
        public static Action OnSunlightThresholdChanged;

        public bool DrawArrow = Defaults.DrawArrow;
        public bool DrawCurrentHourHighlight = Defaults.DrawCurrentHourHighlight;
        public bool DrawHourBar = Defaults.DrawHourBar;
        public bool DrawHoursBarCursor = Defaults.DrawHoursBarCursor;
        public bool DoDynamicHoursBarLine = Defaults.DoDynamicHoursBarLine;
        public bool DrawMainCursor = Defaults.DrawMainCursor;
        public bool DoFilledHourHighlight = true;
        public bool DrawIncidentOverlay = false;
        public bool DoLoadWarnings = Defaults.DoLoadWarnings;

        public float CursorThickness = Defaults.CursorThickness;
        public float HoursBarCursorThickness = Defaults.HoursBarCursorThickness;
        public float HighlightBorderThickness = Defaults.HighlightBorderThickness;
        public float AuroraMinOpacity = Defaults.AuroraMinOpacity;
        public float AuroraMaxOpacity = Defaults.AuroraMaxOpacity;
        public float SunlightThreshold_Night = Defaults.SunlightThreshold_Night;
        public float _SunlightThreshold_Any = Defaults._SunlightThreshold_Any;
        public float SunlightThreshold_DawnDusk = Defaults.SunlightThreshold_DawnDusk;
        public float SunlightThreshold_SunriseSunset = Defaults.SunlightThreshold_SunriseSunset;

        public Color Color_Arrow = Defaults.Color_Arrow;
        public Color Color_HourHighlight = Defaults.Color_HourHighlight;
        public Color Color_MainCursor = Defaults.Color_MainCursor;
        public Color Color_HoursBarCursor_Day = Defaults.Color_HoursBarCursor_Day;
        public Color Color_HoursBarCursor_Night = Defaults.Color_HoursBarCursor_Night;
        public Color Color_Night = Defaults.Color_Night;
        public Color Color_DawnDusk = Defaults.Color_DawnDusk;
        public Color Color_SunriseSunset = Defaults.Color_SunriseSunset;
        public Color Color_Day = Defaults.Color_Day;
        public Color Color_VolcanicWinter = Defaults.Color_VolcanicWinter;
        public Color Color_ToxicFallout = Defaults.Color_ToxicFallout;
        public Color Color_Aurora1 = Defaults.Color_Aurora1;
        public Color Color_Aurora2 = Defaults.Color_Aurora2;
        public Color _DefaultTransparentColor = Defaults._DefaultTransparentColor;
        public Color _HighlightInteriorColor = Defaults._HighlightInteriorColor;

        public static float ValidateCursorThickness(float cursor)
        {
            if (cursor % 2 != 0)
            {
                cursor += 1;
            }

            return cursor;
        }

        public void Write()
        {
            ChronosSharedSettingsStore.Save(this);
        }
    }
}

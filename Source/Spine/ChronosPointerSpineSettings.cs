using System.Collections.Generic;
using Spine.UI.SettingsFramework;

namespace ChronosPointer
{
    /// <summary>
    /// Spine's schema for Chronos Pointer's existing settings object.
    /// ChronosPointerSettings remains the owner of persistence, migrations, and
    /// load-time normalization; this schema only describes the player-facing
    /// settings page and its stable contextual targets.
    /// </summary>
    public static class ChronosPointerSpineSettings
    {
        public const string VisibilityGroupId = "chronos.visibility";
        public const string ShowArrowId = "chronos.visibility.arrow";
        public const string ShowHourBarId = "chronos.visibility.hourBar";
        public const string ShowHoursBarCursorId = "chronos.visibility.hourBar.cursor";
        public const string DynamicHoursBarCursorId = "chronos.visibility.hourBar.dynamicCursor";
        public const string IncidentOverlayId = "chronos.visibility.hourBar.incidentOverlay";
        public const string ShowMainCursorId = "chronos.visibility.mainCursor";
        public const string ShowCurrentHourHighlightId = "chronos.visibility.currentHour";
        public const string FillCurrentHourHighlightId = "chronos.visibility.currentHour.fill";
        public const string LoadWarningsId = "chronos.visibility.loadWarnings";

        public const string CursorThicknessGroupId = "chronos.cursorThickness";
        public const string CursorThicknessId = "chronos.cursorThickness.main";
        public const string HoursBarCursorThicknessId = "chronos.cursorThickness.hourBar";
        public const string HighlightBorderThicknessId = "chronos.cursorThickness.highlightBorder";

        public const string SunlightThresholdsGroupId = "chronos.sunlightThresholds";
        public const string SunlightThresholdNightId = "chronos.sunlightThresholds.night";
        public const string SunlightThresholdDawnDuskId = "chronos.sunlightThresholds.dawnDusk";
        public const string SunlightThresholdSunriseSunsetId = "chronos.sunlightThresholds.sunriseSunset";

        public const string AuroraOpacityGroupId = "chronos.auroraOpacity";
        public const string AuroraMinOpacityId = "chronos.auroraOpacity.minimum";
        public const string AuroraMaxOpacityId = "chronos.auroraOpacity.maximum";

        public const string MainColorsGroupId = "chronos.colors.main";
        public const string ArrowColorId = "chronos.colors.main.arrow";
        public const string MainCursorColorId = "chronos.colors.main.cursor";
        public const string CurrentHourHighlightColorId = "chronos.colors.main.currentHour";

        public const string HoursBarCursorColorsGroupId = "chronos.colors.hourBarCursor";
        public const string HoursBarCursorDayColorId = "chronos.colors.hourBarCursor.day";
        public const string HoursBarCursorNightColorId = "chronos.colors.hourBarCursor.night";

        public const string HoursBarColorsGroupId = "chronos.colors.hourBar";
        public const string NightColorId = "chronos.colors.hourBar.night";
        public const string DawnDuskColorId = "chronos.colors.hourBar.dawnDusk";
        public const string SunriseSunsetColorId = "chronos.colors.hourBar.sunriseSunset";
        public const string DayColorId = "chronos.colors.hourBar.day";

        public const string IncidentColorsGroupId = "chronos.colors.incidents";
        public const string ToxicFalloutColorId = "chronos.colors.incidents.toxicFallout";
        public const string VolcanicWinterColorId = "chronos.colors.incidents.volcanicWinter";
        public const string AuroraColor1Id = "chronos.colors.incidents.aurora1";
        public const string AuroraColor2Id = "chronos.colors.incidents.aurora2";

        private const string SunlightThresholdNightField = "_sunlightThreshold_Night";
        private const string SunlightThresholdDawnDuskField = "_sunlightThreshold_DawnDusk";
        private const string SunlightThresholdSunriseSunsetField = "_sunlightThreshold_SunriseSunset";

        public static SettingsSchema<ChronosPointerSettings> Schema { get; }

        public static IReadOnlyList<SettingDefinition> Definitions =>
            Schema.Definitions;

        static ChronosPointerSpineSettings()
        {
            var schema = new SettingsSchema<ChronosPointerSettings>();

            var visibility = schema.Section(VisibilityGroupId, "Visibility");
            visibility.Toggle(
                ShowArrowId,
                settings => settings.DrawArrow,
                "Show Arrow",
                tooltip: "Whether to draw the arrow above the schedule area.")
                .DefaultTo(Defaults.DrawArrow)
                .ControlsChildren();
            visibility.Toggle(
                ShowHourBarId,
                settings => settings.DrawHourBar,
                "Show Hours Bar",
                tooltip: "Whether to draw the Hours Bar.",
                onChanged: settings => settings.DrawHourBarGetSet = settings.DrawHourBar)
                .DefaultTo(Defaults.DrawHourBar)
                .ControlsChildren();
            schema.Under(ShowHourBarId).Toggle(
                ShowHoursBarCursorId,
                settings => settings.DrawHoursBarCursor,
                "Show Hours Bar Cursor",
                tooltip: "Whether to draw the Hours Bar cursor.")
                .DefaultTo(Defaults.DrawHoursBarCursor)
                .ControlsChildren();
            schema.Under(ShowHoursBarCursorId).Toggle(
                DynamicHoursBarCursorId,
                settings => settings.DoDynamicHoursBarLine,
                "Dynamic Hours Bar Cursor Color",
                tooltip: "Whether to dynamically change the Hours Bar cursor color based on daylight level.")
                .DefaultTo(Defaults.DoDynamicHoursBarLine)
                .ControlsChildren()
                .ScribeAs("DrawDynamicTimeTraceLine");
            schema.Under(ShowHourBarId).Toggle(
                IncidentOverlayId,
                settings => settings.DrawIncidentOverlay,
                "Do Incident Special Effects",
                tooltip: "Whether to display incident effects on the Hours Bar.")
                .DefaultTo(Defaults.DrawIncidentOverlay)
                .ControlsChildren();
            visibility.Toggle(
                ShowMainCursorId,
                settings => settings.DrawMainCursor,
                "Show Main Cursor",
                tooltip: "Whether to show the Pawn Main Cursor.")
                .DefaultTo(Defaults.DrawMainCursor)
                .ControlsChildren();
            visibility.Toggle(
                ShowCurrentHourHighlightId,
                settings => settings.DrawCurrentHourHighlight,
                "Show Current Hour Highlight",
                tooltip: "Whether to show the Current Hour highlight.",
                onChanged: settings => settings.DrawCurrentHourHighlightGetSet = settings.DrawCurrentHourHighlight)
                .DefaultTo(Defaults.DrawCurrentHourHighlight)
                .ControlsChildren()
                .ScribeAs("DrawHighlight");
            schema.Under(ShowCurrentHourHighlightId).Toggle(
                FillCurrentHourHighlightId,
                settings => settings.DoFilledHourHighlight,
                "Fill Current Hour Highlight",
                tooltip: "Whether to fill the Current Hour highlight.")
                .DefaultTo(Defaults.DoFilledHourHighlight);
            visibility.Toggle(
                LoadWarningsId,
                settings => settings.DoLoadWarnings,
                "Show Warnings on Load",
                tooltip: "Whether to show mod conflict warnings on startup.")
                .DefaultTo(Defaults.DoLoadWarnings);

            var cursorThickness = schema.Section(CursorThicknessGroupId, "Cursor Thicknesses");
            cursorThickness.Slider(
                CursorThicknessId,
                settings => settings.CursorThickness,
                "Main Cursor",
                tooltip: "Thickness of the Main Cursor.",
                onChanged: settings => settings.CursorThickness =
                    ChronosPointerSettings.ValidateCursorThickness(settings.CursorThickness))
                .DefaultTo(Defaults.CursorThickness)
                .Range(2f, 10f)
                .Step(2f);
            cursorThickness.Slider(
                HoursBarCursorThicknessId,
                settings => settings.HoursBarCursorThickness,
                "Hours Bar Cursor",
                tooltip: "Thickness of the Hours Bar cursor.",
                onChanged: settings => settings.HoursBarCursorThickness =
                    ChronosPointerSettings.ValidateCursorThickness(settings.HoursBarCursorThickness))
                .DefaultTo(Defaults.HoursBarCursorThickness)
                .Range(2f, 10f)
                .Step(2f)
                .ScribeAs("HoursBarThickness");
#if V1_3
            cursorThickness.Slider(
                HighlightBorderThicknessId,
                settings => settings.HighlightBorderThickness,
                "Highlight Border",
                tooltip: "Thickness of the Current Hour highlight border.")
                .DefaultTo(Defaults.HighlightBorderThickness)
                .Range(2f, 10f)
                .Step(2f);
#endif

            var sunlight = schema.Section(SunlightThresholdsGroupId, "Sunlight Thresholds");
            sunlight.Slider(
                SunlightThresholdNightId,
                SunlightThresholdNightField,
                "Night",
                tooltip: "How dark the map has to be to show the night color.",
                onChanged: settings => settings.SunlightThreshold_Night =
                    settings.SunlightThreshold_Night)
                .DefaultTo(Defaults.SunlightThreshold_Night)
                .Range(0f, 1f)
                .ScribeAs("SunlightThreshold_Night");
            sunlight.Slider(
                SunlightThresholdDawnDuskId,
                SunlightThresholdDawnDuskField,
                "Dawn/Dusk",
                tooltip: "How dark the map has to be to show the dawn/dusk color.",
                onChanged: settings => settings.SunlightThreshold_DawnDusk =
                    settings.SunlightThreshold_DawnDusk)
                .DefaultTo(Defaults.SunlightThreshold_DawnDusk)
                .Range(0f, 1f)
                .ScribeAs("SunlightThreshold_DawnDusk");
            sunlight.Slider(
                SunlightThresholdSunriseSunsetId,
                SunlightThresholdSunriseSunsetField,
                "Sunrise/Sunset",
                tooltip: "How dark the map has to be to show the sunrise/sunset color.",
                onChanged: settings => settings.SunlightThreshold_SunriseSunset =
                    settings.SunlightThreshold_SunriseSunset)
                .DefaultTo(Defaults.SunlightThreshold_SunriseSunset)
                .Range(0f, 1f)
                .ScribeAs("SunlightThreshold_SunriseSunset");

            var auroraOpacity = schema.Section(AuroraOpacityGroupId, "Aurora Opacity");
            auroraOpacity.Slider(
                AuroraMinOpacityId,
                settings => settings.AuroraMinOpacity,
                "Aurora Min Opacity",
                tooltip: "The minimum transparency the aurora effect gets.")
                .DefaultTo(Defaults.AuroraMinOpacity)
                .Range(0f, 1f);
            auroraOpacity.Slider(
                AuroraMaxOpacityId,
                settings => settings.AuroraMaxOpacity,
                "Aurora Max Opacity",
                tooltip: "The maximum transparency the aurora effect gets.")
                .DefaultTo(Defaults.AuroraMaxOpacity)
                .Range(0f, 1f);

            var mainColors = schema.Section(MainColorsGroupId, "Main Colors");
            mainColors.Colour(
                ArrowColorId,
                settings => settings.Color_Arrow,
                "Arrow Color",
                tooltip: "Color used for the arrow above the schedule.")
                .DefaultTo(Defaults.Color_Arrow);
            mainColors.Colour(
                MainCursorColorId,
                settings => settings.Color_MainCursor,
                "Cursor Color",
                tooltip: "Color used for the Pawn Main Cursor.")
                .DefaultTo(Defaults.Color_MainCursor);
            mainColors.Colour(
                CurrentHourHighlightColorId,
                settings => settings.Color_HourHighlight,
                "Current-hour Color",
                tooltip: "Color used to highlight the current hour.")
                .DefaultTo(Defaults.Color_HourHighlight)
                .ScribeAs("Color_Highlight");

            var hoursBarCursorColors = schema.Section(
                HoursBarCursorColorsGroupId,
                "Hours Bar Cursor Colors");
            hoursBarCursorColors.Colour(
                HoursBarCursorDayColorId,
                settings => settings.Color_HoursBarCursor_Day,
                "Time Trace Color Day",
                tooltip: "Color used for the Hours Bar cursor during daylight.")
                .DefaultTo(Defaults.Color_HoursBarCursor_Day);
            hoursBarCursorColors.Colour(
                HoursBarCursorNightColorId,
                settings => settings.Color_HoursBarCursor_Night,
                "Time Trace Color Night",
                tooltip: "Color used for the Hours Bar cursor at night.")
                .DefaultTo(Defaults.Color_HoursBarCursor_Night);

            var hoursBarColors = schema.Section(HoursBarColorsGroupId, "Hours Bar Colors");
            hoursBarColors.Colour(
                NightColorId,
                settings => settings.Color_Night,
                "Night Color",
                tooltip: "Base Hours Bar color for night.")
                .DefaultTo(Defaults.Color_Night);
            hoursBarColors.Colour(
                DawnDuskColorId,
                settings => settings.Color_DawnDusk,
                "Dawn/Dusk Color",
                tooltip: "Base Hours Bar color for dawn and dusk.")
                .DefaultTo(Defaults.Color_DawnDusk);
            hoursBarColors.Colour(
                SunriseSunsetColorId,
                settings => settings.Color_SunriseSunset,
                "Sunrise/Sunset Color",
                tooltip: "Base Hours Bar color for sunrise and sunset.")
                .DefaultTo(Defaults.Color_SunriseSunset);
            hoursBarColors.Colour(
                DayColorId,
                settings => settings.Color_Day,
                "Day Color",
                tooltip: "Base Hours Bar color for day.")
                .DefaultTo(Defaults.Color_Day);

            var incidentColors = schema.Section(IncidentColorsGroupId, "Incident Overlay Colors");
            incidentColors.Colour(
                ToxicFalloutColorId,
                settings => settings.Color_ToxicFallout,
                "Toxic Fallout Color",
                tooltip: "Overlay color used during Toxic Fallout.")
                .DefaultTo(Defaults.Color_ToxicFallout)
                .ScribeAs("ToxicFalloutColor");
            incidentColors.Colour(
                VolcanicWinterColorId,
                settings => settings.Color_VolcanicWinter,
                "Volcanic Winter Color",
                tooltip: "Overlay color used during Volcanic Winter.")
                .DefaultTo(Defaults.Color_VolcanicWinter);
            incidentColors.Colour(
                AuroraColor1Id,
                settings => settings.Color_Aurora1,
                "Aurora Color 1",
                tooltip: "First color used by the aurora overlay.")
                .DefaultTo(Defaults.Color_Aurora1)
                .ScribeAs("AuroraColor1");
            incidentColors.Colour(
                AuroraColor2Id,
                settings => settings.Color_Aurora2,
                "Aurora Color 2",
                tooltip: "Second color used by the aurora overlay.")
                .DefaultTo(Defaults.Color_Aurora2)
                .ScribeAs("AuroraColor2");

            Schema = schema;
        }
    }
}

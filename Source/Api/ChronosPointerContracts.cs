using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ChronosPointer.Api
{
    public static class ChronosScheduleGeometryDefaults
    {
        public const float BaseOffsetX = 1f;
        public const float BaseOffsetY = 40f;
        public const float HourBoxGap = 2f;
        public const float HourBoxHeight = 10f;
        public const float PawnAreaTopOffset = 16f;
        public const float HourBoxWidth = 19f;
    }

    public enum ChronosLightBand
    {
        Night,
        DawnDusk,
        SunriseSunset,
        Day
    }

    public enum ChronosOverlayLayer
    {
        BehindChronos = 0,
        AboveHourBar = 100,
        AboveChronos = 200
    }

    public sealed class ChronosIncidentState
    {
        public ChronosIncidentState(bool solarFlare, bool eclipse, bool toxicFallout, bool volcanicWinter, bool aurora)
        {
            SolarFlare = solarFlare;
            Eclipse = eclipse;
            ToxicFallout = toxicFallout;
            VolcanicWinter = volcanicWinter;
            Aurora = aurora;
        }

        public bool SolarFlare { get; private set; }
        public bool Eclipse { get; private set; }
        public bool ToxicFallout { get; private set; }
        public bool VolcanicWinter { get; private set; }
        public bool Aurora { get; private set; }
        public bool Any => SolarFlare || Eclipse || ToxicFallout || VolcanicWinter || Aurora;
    }

    public sealed class ChronosHourSegment
    {
        public ChronosHourSegment(int hour, float sunlight, ChronosLightBand lightBand, Color baseColor, Color incidentOverlayColor)
        {
            Hour = hour;
            Sunlight = sunlight;
            LightBand = lightBand;
            BaseColor = baseColor;
            IncidentOverlayColor = incidentOverlayColor;
        }

        public int Hour { get; private set; }
        public float Sunlight { get; private set; }
        public ChronosLightBand LightBand { get; private set; }
        public Color BaseColor { get; private set; }
        public Color IncidentOverlayColor { get; private set; }
        public bool HasIncidentOverlay => IncidentOverlayColor.a > 0f;
    }

    public sealed class ChronosSettingsSnapshot
    {
        public bool DrawArrow { get; internal set; }
        public bool DrawCurrentHourHighlight { get; internal set; }
        public bool DrawHourBar { get; internal set; }
        public bool DrawHoursBarCursor { get; internal set; }
        public bool DoDynamicHoursBarLine { get; internal set; }
        public bool DrawMainCursor { get; internal set; }
        public bool DoFilledHourHighlight { get; internal set; }
        public bool DrawIncidentOverlay { get; internal set; }
        public bool DoLoadWarnings { get; internal set; }

        public float CursorThickness { get; internal set; }
        public float HoursBarCursorThickness { get; internal set; }
        public float HighlightBorderThickness { get; internal set; }
        public float AuroraMinOpacity { get; internal set; }
        public float AuroraMaxOpacity { get; internal set; }
        public float SunlightThresholdNight { get; internal set; }
        public float SunlightThresholdAny { get; internal set; }
        public float SunlightThresholdDawnDusk { get; internal set; }
        public float SunlightThresholdSunriseSunset { get; internal set; }

        public Color ColorArrow { get; internal set; }
        public Color ColorHourHighlight { get; internal set; }
        public Color ColorMainCursor { get; internal set; }
        public Color ColorHoursBarCursorDay { get; internal set; }
        public Color ColorHoursBarCursorNight { get; internal set; }
        public Color ColorNight { get; internal set; }
        public Color ColorDawnDusk { get; internal set; }
        public Color ColorSunriseSunset { get; internal set; }
        public Color ColorDay { get; internal set; }
        public Color ColorVolcanicWinter { get; internal set; }
        public Color ColorToxicFallout { get; internal set; }
        public Color ColorAurora1 { get; internal set; }
        public Color ColorAurora2 { get; internal set; }
        public Color DefaultTransparentColor { get; internal set; }
        public Color HighlightInteriorColor { get; internal set; }
    }

    public sealed class ChronosTimelineSnapshot
    {
        public ChronosTimelineSnapshot(
            Map map,
            int mapTile,
            long absoluteTick,
            float localDayPercent,
            float localHour,
            int currentHour,
            Season season,
            ChronosIncidentState incidents,
            IReadOnlyList<ChronosHourSegment> hours,
            ChronosSettingsSnapshot settings)
        {
            Map = map;
            MapTile = mapTile;
            AbsoluteTick = absoluteTick;
            LocalDayPercent = localDayPercent;
            LocalHour = localHour;
            CurrentHour = currentHour;
            Season = season;
            Incidents = incidents;
            Hours = hours;
            Settings = settings;
        }

        public Map Map { get; private set; }
        public int MapTile { get; private set; }
        public long AbsoluteTick { get; private set; }
        public float LocalDayPercent { get; private set; }
        public float LocalHour { get; private set; }
        public int CurrentHour { get; private set; }
        public Season Season { get; private set; }
        public ChronosIncidentState Incidents { get; private set; }
        public IReadOnlyList<ChronosHourSegment> Hours { get; private set; }
        public ChronosSettingsSnapshot Settings { get; private set; }
    }

    public sealed class ChronosDrawOptions
    {
        public ChronosDrawOptions()
        {
            DrawHourBar = true;
            DrawRegularBar = true;
            DrawIncidentOverlay = true;
            DrawHourBarCursor = true;
            DrawArrow = true;
            DrawCurrentHourHighlight = true;
            DrawMainCursor = true;
            DrawOverlays = true;
        }

        public bool DrawHourBar { get; set; }
        public bool DrawRegularBar { get; set; }
        public bool DrawIncidentOverlay { get; set; }
        public bool DrawHourBarCursor { get; set; }
        public bool DrawArrow { get; set; }
        public bool DrawCurrentHourHighlight { get; set; }
        public bool DrawMainCursor { get; set; }
        public bool DrawOverlays { get; set; }

        public static ChronosDrawOptions Default()
        {
            return new ChronosDrawOptions();
        }

        public static ChronosDrawOptions EmbeddedTimeline()
        {
            return new ChronosDrawOptions
            {
                DrawCurrentHourHighlight = false,
                DrawMainCursor = false
            };
        }
    }

    public sealed class ChronosScheduleGeometrySnapshot
    {
        public ChronosScheduleGeometrySnapshot(
            Rect fillRect,
            float baseOffsetX,
            float baseOffsetY,
            float hourBoxWidth,
            float hourBoxGap,
            float hourBoxHeight,
            float pawnAreaTopOffset,
            float windowHeight,
            float headerHeight,
            long frameTick,
            bool isValid)
        {
            FillRect = fillRect;
            BaseOffsetX = baseOffsetX;
            BaseOffsetY = baseOffsetY;
            HourBoxWidth = hourBoxWidth;
            HourBoxGap = hourBoxGap;
            HourBoxHeight = hourBoxHeight;
            PawnAreaTopOffset = pawnAreaTopOffset;
            WindowHeight = windowHeight;
            HeaderHeight = headerHeight;
            FrameTick = frameTick;
            IsValid = isValid;
        }

        public Rect FillRect { get; private set; }
        public float BaseOffsetX { get; private set; }
        public float BaseOffsetY { get; private set; }
        public float HourBoxWidth { get; private set; }
        public float HourBoxGap { get; private set; }
        public float HourBoxHeight { get; private set; }
        public float PawnAreaTopOffset { get; private set; }
        public float WindowHeight { get; private set; }
        public float HeaderHeight { get; private set; }
        public long FrameTick { get; private set; }
        public bool IsValid { get; private set; }

        public Rect HourBarRect => new Rect(FillRect.x + BaseOffsetX, FillRect.y + BaseOffsetY, 24f * HourBoxWidth + 23f * HourBoxGap, HourBoxHeight);
        public Rect PawnAreaRect => new Rect(FillRect.x + BaseOffsetX, FillRect.y + BaseOffsetY + HourBoxHeight + PawnAreaTopOffset, 24f * HourBoxWidth + 23f * HourBoxGap, WindowHeight);

        public Rect GetHourBarRect(int hour)
        {
            int clampedHour = Mathf.Clamp(hour, 0, 23);
            return new Rect(FillRect.x + BaseOffsetX + clampedHour * (HourBoxWidth + HourBoxGap), FillRect.y + BaseOffsetY, HourBoxWidth, HourBoxHeight);
        }

        public Rect GetPawnHourRect(int hour)
        {
            Rect hourRect = GetHourBarRect(hour);
            return new Rect(hourRect.x, FillRect.y + BaseOffsetY + HourBoxHeight + PawnAreaTopOffset, HourBoxWidth, WindowHeight);
        }

        public float XAtLocalHour(float localHour)
        {
            float normalizedHour = Mathf.Repeat(localHour, 24f);
            int hour = Mathf.Clamp((int)normalizedHour, 0, 23);
            float progress = normalizedHour - hour;
            return FillRect.x + BaseOffsetX + hour * (HourBoxWidth + HourBoxGap) + progress * HourBoxWidth;
        }
    }

    public sealed class ChronosOverlayContext
    {
        public ChronosOverlayContext(ChronosTimelineSnapshot timeline, ChronosScheduleGeometrySnapshot geometry, Event currentEvent)
        {
            Timeline = timeline;
            Geometry = geometry;
            CurrentEvent = currentEvent;
        }

        public ChronosTimelineSnapshot Timeline { get; private set; }
        public ChronosScheduleGeometrySnapshot Geometry { get; private set; }
        public Event CurrentEvent { get; private set; }
        public bool IsRepaint => CurrentEvent != null && CurrentEvent.type == EventType.Repaint;
        public bool IsLayout => CurrentEvent != null && CurrentEvent.type == EventType.Layout;
    }

    public sealed class ChronosOverlayRegistration
    {
        public string OwnerPackageId { get; set; }
        public ChronosOverlayLayer Layer { get; set; }
        public int SortOrder { get; set; }
        public Action<ChronosOverlayContext> Draw { get; set; }
    }
}

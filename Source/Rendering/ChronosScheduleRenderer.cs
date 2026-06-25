using ChronosPointer.Api;
using ChronosPointer.Core;
using ChronosPointer.RimWorld;
using UnityEngine;
using Verse;

namespace ChronosPointer.Rendering
{
    internal static class ChronosScheduleRenderer
    {
        private const float ArrowWidth = 8f;
        private const float ArrowHeight = 8f;

        public static void DrawSchedule(ChronosTimelineSnapshot timeline, ChronosScheduleGeometrySnapshot geometry, bool drawRegularBar, bool drawIncidentOverlay)
        {
            ChronosDrawOptions options = ChronosDrawOptions.Default();
            options.DrawRegularBar = drawRegularBar;
            options.DrawIncidentOverlay = drawIncidentOverlay;
            DrawSchedule(timeline, geometry, options);
        }

        public static void DrawSchedule(ChronosTimelineSnapshot timeline, ChronosScheduleGeometrySnapshot geometry, ChronosDrawOptions options)
        {
            if (timeline == null || geometry == null || !geometry.IsValid)
            {
                return;
            }

            if (options == null)
            {
                options = ChronosDrawOptions.Default();
            }

            ChronosOverlayContext overlayContext = null;
            if (options.DrawOverlays)
            {
                overlayContext = ChronosPointerApi.CreateOverlayContext(timeline, geometry);
                ChronosOverlayRegistry.DrawLayer(ChronosOverlayLayer.BehindChronos, overlayContext);
            }

            ChronosSettingsSnapshot settings = timeline.Settings;
            if (settings.DrawHourBar && options.DrawHourBar)
            {
                if (options.DrawRegularBar)
                {
                    DrawDayNightBar(timeline, geometry, useIncidentOverlay: false);
                }

                if (settings.DrawIncidentOverlay && options.DrawIncidentOverlay && timeline.Incidents.Any)
                {
                    DrawDayNightBar(timeline, geometry, useIncidentOverlay: true);
                }

                if (settings.DrawHoursBarCursor && options.DrawHourBarCursor)
                {
                    DrawDayNightTimeIndicator(timeline, geometry);
                }
            }

            if (options.DrawOverlays)
            {
                ChronosOverlayRegistry.DrawLayer(ChronosOverlayLayer.AboveHourBar, overlayContext);
            }

            if (settings.DrawArrow && options.DrawArrow)
            {
                DrawArrowTexture(timeline, geometry);
            }

            if (settings.DrawCurrentHourHighlight && options.DrawCurrentHourHighlight)
            {
                DrawHighlight(timeline, geometry);
            }

            if (settings.DrawMainCursor && options.DrawMainCursor)
            {
                DrawFullHeightCursor(timeline, geometry);
            }

            if (options.DrawOverlays)
            {
                ChronosOverlayRegistry.DrawLayer(ChronosOverlayLayer.AboveChronos, overlayContext);
            }
        }

        private static void DrawDayNightBar(ChronosTimelineSnapshot timeline, ChronosScheduleGeometrySnapshot geometry, bool useIncidentOverlay)
        {
            for (int hour = 0; hour < timeline.Hours.Count && hour < 24; hour++)
            {
                ChronosHourSegment segment = timeline.Hours[hour];
                Color color = useIncidentOverlay ? segment.IncidentOverlayColor : segment.BaseColor;
                Widgets.DrawBoxSolid(geometry.GetHourBarRect(hour), color);
            }
        }

        private static void DrawHighlight(ChronosTimelineSnapshot timeline, ChronosScheduleGeometrySnapshot geometry)
        {
            ChronosSettingsSnapshot settings = timeline.Settings;
            Rect highlightRect = geometry.GetPawnHourRect(timeline.CurrentHour);
            if (settings.DoFilledHourHighlight)
            {
                Widgets.DrawBoxSolid(highlightRect, settings.ColorHourHighlight);
            }
            else
            {
                ChronosRimWorldCompat.DrawBoxSolidWithOutline(highlightRect, settings.HighlightInteriorColor, settings.ColorHourHighlight, 2);
            }
        }

        private static void DrawDayNightTimeIndicator(ChronosTimelineSnapshot timeline, ChronosScheduleGeometrySnapshot geometry)
        {
            Rect traceRect = GetTimeCursorRect(timeline, geometry);
            Color lineColor = ChronosTimelineService.GetCurrentHoursBarCursorColor(timeline.Map, timeline);
            Widgets.DrawBoxSolid(traceRect, lineColor);
        }

        private static void DrawArrowTexture(ChronosTimelineSnapshot timeline, ChronosScheduleGeometrySnapshot geometry)
        {
            if (ChronosPointerTextures.DownArrowTexture == null)
            {
                return;
            }

            ChronosSettingsSnapshot settings = timeline.Settings;
            Rect cursorRect = GetTimeCursorRect(timeline, geometry);
            float arrowCenterX = cursorRect.x + cursorRect.width / 2f;
            float barTopY = geometry.FillRect.y + geometry.BaseOffsetY + 4f;
            Rect arrowRect = new Rect(
                arrowCenterX - (ArrowWidth / 2f),
                barTopY - ArrowHeight - (!settings.DrawHourBar ? -2f : 4f),
                ArrowWidth,
                ArrowHeight);
            Rect drawRect = arrowRect.ScaledBy(!settings.DrawHourBar ? 2 : 1);

            Color oldColor = GUI.color;
            GUI.color = settings.ColorArrow;
            try
            {
                GUI.DrawTexture(drawRect, ChronosPointerTextures.DownArrowTexture);
            }
            finally
            {
                GUI.color = oldColor;
            }
        }

        private static void DrawFullHeightCursor(ChronosTimelineSnapshot timeline, ChronosScheduleGeometrySnapshot geometry)
        {
            ChronosSettingsSnapshot settings = timeline.Settings;
            float cursorThickness = ChronosPointerSettings.ValidateCursorThickness(settings.CursorThickness);
            float cursorX = geometry.XAtLocalHour(timeline.LocalHour) + 1f - (cursorThickness / 2f);
            float cursorY = geometry.FillRect.y + geometry.BaseOffsetY + geometry.HourBoxHeight + geometry.PawnAreaTopOffset;

            Rect cursorRect = new Rect(cursorX, cursorY, cursorThickness, geometry.WindowHeight);
            Widgets.DrawBoxSolid(cursorRect, settings.ColorMainCursor);
        }

        private static Rect GetTimeCursorRect(ChronosTimelineSnapshot timeline, ChronosScheduleGeometrySnapshot geometry)
        {
            ChronosSettingsSnapshot settings = timeline.Settings;
            float cursorThickness = settings.HoursBarCursorThickness;
            float cursorX = geometry.XAtLocalHour(timeline.LocalHour);
            float cursorY = geometry.FillRect.y + geometry.BaseOffsetY;
            return new Rect(cursorX, cursorY, cursorThickness, geometry.HourBoxHeight);
        }
    }
}

using ChronosPointer.Api;
using ChronosPointer.Core;
using UnityEngine;
using Verse;
#if CHRONOS_POINTER_USE_SPINE
using Spine.UI.ContextualSettings;
#endif

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

            ChronosSettingsSnapshot settings = timeline.Settings;
#if CHRONOS_POINTER_USE_SPINE
            if (TryBindContextualSettings(timeline, geometry, options, settings))
            {
                return;
            }
#endif

            ChronosOverlayContext overlayContext = null;
            if (options.DrawOverlays)
            {
                overlayContext = ChronosPointerApi.CreateOverlayContext(timeline, geometry);
                ChronosOverlayRegistry.DrawLayer(ChronosOverlayLayer.BehindChronos, overlayContext);
            }

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
                Widgets.DrawBoxSolidWithOutline(highlightRect, settings.HighlightInteriorColor, settings.ColorHourHighlight, 2);
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
            Rect drawRect = GetArrowDrawRect(timeline, geometry);

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
            Widgets.DrawBoxSolid(GetFullHeightCursorRect(timeline, geometry), settings.ColorMainCursor);
        }

#if CHRONOS_POINTER_USE_SPINE
        private static bool TryBindContextualSettings(
            ChronosTimelineSnapshot timeline,
            ChronosScheduleGeometrySnapshot geometry,
            ChronosDrawOptions options,
            ChronosSettingsSnapshot settings)
        {
            bool handled = false;
            bool hourBarVisible = settings.DrawHourBar &&
                options.DrawHourBar &&
                (options.DrawRegularBar ||
                 (settings.DrawIncidentOverlay &&
                  options.DrawIncidentOverlay &&
                  timeline.Incidents.Any) ||
                 (settings.DrawHoursBarCursor &&
                  options.DrawHourBarCursor));
            if (hourBarVisible)
            {
                handled |= ChronosPointerMod.ContextualSettings?.BindSetting(
                    geometry.HourBarRect,
                    ChronosPointerSpineSettings.ShowHourBarId,
                    ChronosPointerSpineSettings.VisibilityGroupId,
                    priority: 0) == true;
                if (settings.DrawHoursBarCursor && options.DrawHourBarCursor)
                {
                    handled |= ChronosPointerMod.ContextualSettings?.BindSetting(
                        GetTimeCursorRect(timeline, geometry),
                        ChronosPointerSpineSettings.ShowHoursBarCursorId,
                        ChronosPointerSpineSettings.VisibilityGroupId,
                        priority: 10) == true;
                }
            }

            if (settings.DrawArrow && options.DrawArrow &&
                ChronosPointerTextures.DownArrowTexture != null)
            {
                handled |= ChronosPointerMod.ContextualSettings?.BindSetting(
                    GetArrowDrawRect(timeline, geometry),
                    ChronosPointerSpineSettings.ShowArrowId,
                    ChronosPointerSpineSettings.VisibilityGroupId,
                    priority: 0) == true;
            }

            if (settings.DrawCurrentHourHighlight && options.DrawCurrentHourHighlight)
            {
                handled |= ChronosPointerMod.ContextualSettings?.BindSetting(
                    geometry.GetPawnHourRect(timeline.CurrentHour),
                    ChronosPointerSpineSettings.ShowCurrentHourHighlightId,
                    ChronosPointerSpineSettings.VisibilityGroupId,
                    priority: 10) == true;
            }

            if (settings.DrawMainCursor && options.DrawMainCursor)
            {
                handled |= ChronosPointerMod.ContextualSettings?.BindSetting(
                    GetFullHeightCursorRect(timeline, geometry),
                    ChronosPointerSpineSettings.ShowMainCursorId,
                    ChronosPointerSpineSettings.VisibilityGroupId,
                    priority: 0) == true;
            }

            return handled;
        }
#endif

        private static Rect GetArrowDrawRect(
            ChronosTimelineSnapshot timeline,
            ChronosScheduleGeometrySnapshot geometry)
        {
            ChronosSettingsSnapshot settings = timeline.Settings;
            Rect cursorRect = GetTimeCursorRect(timeline, geometry);
            float arrowCenterX = cursorRect.x + cursorRect.width / 2f;
            float barTopY = geometry.FillRect.y + geometry.BaseOffsetY + 4f;
            Rect arrowRect = new Rect(
                arrowCenterX - (ArrowWidth / 2f),
                barTopY - ArrowHeight - (!settings.DrawHourBar ? -2f : 4f),
                ArrowWidth,
                ArrowHeight);
            return arrowRect.ScaledBy(!settings.DrawHourBar ? 2 : 1);
        }

        private static Rect GetFullHeightCursorRect(
            ChronosTimelineSnapshot timeline,
            ChronosScheduleGeometrySnapshot geometry)
        {
            ChronosSettingsSnapshot settings = timeline.Settings;
            float cursorThickness = ChronosPointerSettings.ValidateCursorThickness(settings.CursorThickness);
            float cursorX = geometry.XAtLocalHour(timeline.LocalHour) + 1f - (cursorThickness / 2f);
            float cursorY = geometry.FillRect.y + geometry.BaseOffsetY + geometry.HourBoxHeight + geometry.PawnAreaTopOffset;
            return new Rect(cursorX, cursorY, cursorThickness, geometry.WindowHeight);
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

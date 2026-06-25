#if VALPHA4
using ChronosPointer.Api;
using ChronosPointer.Core;
using ChronosPointer.Rendering;
using ChronosPointer.RimWorld;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ChronosPointer
{
    [HarmonyPatch(typeof(Tab_Overview_Work), "PanelOnGUI")]
    public static class Patch_ScheduleWindow
    {
        private const float InnerMargin = 10f;
        private const float NameColumnWidth = 175f;
        private const float RightScrollbarReserve = 16f;
        private const float HourGap = 1f;
        private const float HourBarY = 32f;
        private const float HourBarHeight = 8f;

        private static Map lastKnownMap;
        private static bool loggedRender;

        public static bool dayNightColorsCalculated = false;
        public static Season _cachedSeason = Season.Undefined;
        public static Map _cachedMap = null;

        internal static bool isSolarFlare = false;
        internal static bool isEclipse = false;
        internal static bool isToxicFallout = false;
        internal static bool isVolcanicWinter = false;
        internal static bool isAurora = false;

        public static bool AuroraActive { get { return isAurora || overrideIsAurora; } }
        public static bool SolarFlareActive { get { return isSolarFlare || overrideIsSolarFlare; } }
        public static bool EclipseActive { get { return isEclipse || overrideIsEclipse; } }
        public static bool ToxicFalloutActive { get { return isToxicFallout || overrideIsToxicFallout; } }
        public static bool VolcanicWinterActive { get { return isVolcanicWinter || overrideIsVolcanicWinter; } }

        public static bool IsInTestMode = false;
        public static bool overrideIsSolarFlare = false;
        public static bool overrideIsEclipse = false;
        public static bool overrideIsToxicFallout = false;
        public static bool overrideIsVolcanicWinter = false;
        public static bool overrideIsAurora = false;
        public static bool overrideDrawRegularBar = true;

        [HarmonyPostfix]
        public static void Postfix(Rect fillRect)
        {
            Map map = ChronosRimWorldCompat.CurrentMap();
            if (map == null)
            {
                return;
            }

            if (!loggedRender)
            {
                loggedRender = true;
                Log.Message("[ChronosPointer] Alpha4 Work overview timeline renderer active.");
            }

            if (map != lastKnownMap)
            {
                dayNightColorsCalculated = false;
                lastKnownMap = map;
            }

            Rect innerRect = new Rect(
                fillRect.x + InnerMargin,
                fillRect.y + InnerMargin,
                Mathf.Max(0f, fillRect.width - InnerMargin * 2f),
                Mathf.Max(0f, fillRect.height - InnerMargin * 2f));

            float availableWidth = Mathf.Max(24f, innerRect.width - NameColumnWidth - RightScrollbarReserve);
            float hourWidth = Mathf.Max(1f, (availableWidth - HourGap * 23f) / 24f);
            ChronosScheduleGeometrySnapshot geometry = ChronosPointerApi.CreateGeometry(
                innerRect,
                0f,
                hourWidth,
                NameColumnWidth,
                HourBarY,
                HourGap,
                HourBarHeight,
                0f,
                0f);

            ChronosTimelineSnapshot timeline;
            if (!ChronosTimelineService.TryCreateTimeline(map, ChronosPointerMod.Settings, out timeline, true))
            {
                return;
            }

            ChronosPointerApi.SetLatestTimeline(timeline);
            ChronosPointerApi.SetLatestGeometry(geometry);

            ChronosDrawOptions options = ChronosDrawOptions.Default();
            options.DrawRegularBar = overrideDrawRegularBar;
            options.DrawIncidentOverlay = false;
            options.DrawCurrentHourHighlight = false;
            options.DrawMainCursor = false;
            ChronosScheduleRenderer.DrawSchedule(timeline, geometry, options);
        }
    }
}
#endif

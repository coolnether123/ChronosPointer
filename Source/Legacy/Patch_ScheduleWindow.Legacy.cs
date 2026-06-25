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
    [HarmonyPatch(typeof(MainTabWindow_Restrict), nameof(MainTabWindow_Restrict.DoWindowContents))]
    public static class Patch_ScheduleWindow
    {
        private const float HeaderHeight = 65f;
        private const float ScheduleX = 201f;
        private const float HourWidth = 20.833334f;
        private const float HourBarY = 52f;
        private const float HourBarHeight = 10f;

        private static Map lastKnownMap;

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

            if (map != lastKnownMap)
            {
                dayNightColorsCalculated = false;
                lastKnownMap = map;
            }

            ChronosScheduleGeometrySnapshot geometry = ChronosPointerApi.CreateGeometry(
                fillRect,
                Mathf.Max(0f, fillRect.height - HeaderHeight),
                HourWidth,
                ScheduleX,
                HourBarY,
                0f,
                HourBarHeight,
                HeaderHeight - HourBarY - HourBarHeight,
                HeaderHeight);

            ChronosTimelineSnapshot timeline;
            if (!ChronosTimelineService.TryCreateTimeline(map, ChronosPointerMod.Settings, out timeline, true))
            {
                return;
            }

            ChronosPointerApi.SetLatestTimeline(timeline);
            ChronosPointerApi.SetLatestGeometry(geometry);
            ChronosScheduleRenderer.DrawSchedule(timeline, geometry, overrideDrawRegularBar, false);
        }
    }
}

using ChronosPointer.Api;
using ChronosPointer.Core;
using ChronosPointer.Rendering;
using ChronosPointer.RimWorld;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ChronosPointer
{
    [HarmonyPatch(typeof(MainTabWindow_Schedule), nameof(MainTabWindow_Schedule.DoWindowContents))]
    public static class Patch_ScheduleWindow
    {
        private static Map lastKnownMap;
        private static bool debugDrawOverlayBar = true;

        public static bool dayNightColorsCalculated = false;
        public static Season _cachedSeason = Season.Undefined;
        public static Map _cachedMap = null;

        internal static bool isSolarFlare = false;
        internal static bool isEclipse = false;
        internal static bool isToxicFallout = false;
        internal static bool isVolcanicWinter = false;
        internal static bool isAurora = false;

        public static bool AuroraActive => isAurora || overrideIsAurora;
        public static bool SolarFlareActive => isSolarFlare || overrideIsSolarFlare;
        public static bool EclipseActive => isEclipse || overrideIsEclipse;
        public static bool ToxicFalloutActive => isToxicFallout || overrideIsToxicFallout;
        public static bool VolcanicWinterActive => isVolcanicWinter || overrideIsVolcanicWinter;

        public static bool IsInTestMode = false;
        public static bool overrideIsSolarFlare = false;
        public static bool overrideIsEclipse = false;
        public static bool overrideIsToxicFallout = false;
        public static bool overrideIsVolcanicWinter = false;
        public static bool overrideIsAurora = false;
        public static bool overrideDrawRegularBar = true;

        [HarmonyPostfix]
        public static void Postfix(MainTabWindow_Schedule __instance, Rect fillRect)
        {
            if (!IsInTestMode && Find.MainTabsRoot.OpenTab != __instance.def)
            {
                return;
            }

            Map map = Find.CurrentMap;
            if (map == null)
            {
                return;
            }

            if (!ChronosScheduleGeometryFactory.TryCreate(__instance, fillRect, out ChronosScheduleGeometrySnapshot geometry))
            {
                return;
            }

            if (map != lastKnownMap)
            {
                dayNightColorsCalculated = false;
                lastKnownMap = map;
            }

            if (!ChronosTimelineService.TryCreateTimeline(map, ChronosPointerMod.Settings, out ChronosTimelineSnapshot timeline, syncLegacyPatchState: true))
            {
                return;
            }

            ChronosPointerApi.SetLatestTimeline(timeline);
            ChronosPointerApi.SetLatestGeometry(geometry);
            ChronosScheduleRenderer.DrawSchedule(timeline, geometry, overrideDrawRegularBar, debugDrawOverlayBar);
        }
    }
}

#if V1_1 || V1_0
using Harmony;
using System.Reflection; // Required for manual reflection in 1.1
#else
using HarmonyLib;
#endif
#if V1_5U
using LudeonTK;
#endif
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static UnityEngine.GUI;
#if V1_2 || V1_1 || V1_0
using MainTabWindow_Schedule = RimWorld.MainTabWindow_Restrict;
#endif

namespace ChronosPointer
{

    [HarmonyPatch(typeof(MainTabWindow_Schedule), nameof(MainTabWindow_Schedule.DoWindowContents))]
    public static class Patch_ScheduleWindow
    {
        #region Values


        // Where the schedule grid starts
        private static float BaseOffsetX = 1f;
        private const float BASE_OFFSET_Y = 40;

        // Each hour cell
        private const float HOUR_BOX_GAP = 2f;

        // Day/night bar
        private const float HOUR_BOX_HEIGHT = 10f;

        // Extra offsets for highlight & line so they don't slip off top/bottom
        private const float PAWN_AREA_TOP_OFFSET = 16f;
        private const float PAWN_AREA_BOTTOM_TRIM = 2f;

        // Define the SolarFlare condition if not available
        private static readonly GameConditionDef SolarFlareDef = DefDatabase<GameConditionDef>.GetNamed("SolarFlare");

        // Flag to track if the day/night colors have been calculated
        public static bool dayNightColorsCalculated = false;


        // Add a static variable to store the last known map
        private static Map lastKnownMap = null;

        private static readonly Color[][] _seasonDaylightCache = new Color[5][];
        public static Season _cachedSeason = Season.Undefined;
        public static Map _cachedMap = null;


        // These are now internal so Dialog_IncidentTesting can see them
        internal static bool isSolarFlare = false;
        internal static bool isEclipse = false;
        internal static bool isToxicFallout = false;
        internal static bool isVolcanicWinter = false;
        internal static bool isAurora = false;

#if V1_0
        public static bool AuroraActive { get { return isAurora || overrideIsAurora; } }
        public static bool SolarFlareActive { get { return isSolarFlare || overrideIsSolarFlare; } }
        public static bool EclipseActive { get { return isEclipse || overrideIsEclipse; } }
        public static bool ToxicFalloutActive { get { return isToxicFallout || overrideIsToxicFallout; } }
        public static bool VolcanicWinterActive { get { return isVolcanicWinter || overrideIsVolcanicWinter; } }
        private static ChronosPointerSettings Settings { get { return ChronosPointerMod.Settings; } }
#else
        public static bool AuroraActive => isAurora || overrideIsAurora;
        public static bool SolarFlareActive => isSolarFlare || overrideIsSolarFlare;
        public static bool EclipseActive => isEclipse || overrideIsEclipse;
        public static bool ToxicFalloutActive => isToxicFallout || overrideIsToxicFallout;
        public static bool VolcanicWinterActive => isVolcanicWinter || overrideIsVolcanicWinter;
        private static ChronosPointerSettings Settings => ChronosPointerMod.Settings;
#endif

        public static bool IsInTestMode = false;
        public static bool overrideIsSolarFlare = false;
        public static bool overrideIsEclipse = false;
        public static bool overrideIsToxicFallout = false;
        public static bool overrideIsVolcanicWinter = false;
        public static bool overrideIsAurora = false;

        public static bool overrideDrawRegularBar = true;
        private static bool debugDrawOverlayBar = true;

        #endregion

        [HarmonyPostfix]
        public static void Postfix(MainTabWindow_Schedule __instance, Rect fillRect)
        {
            bool shouldDrawThisFrame = IsInTestMode || Find.MainTabsRoot.OpenTab == __instance.def;

            if (!IsInTestMode && Find.MainTabsRoot.OpenTab != __instance.def)
            {
                return; // Skip drawing if not active tab and not in test mode
            }


            if (Find.CurrentMap == null) return;

#if V1_1 || V1_0
            // In 1.1, the 'table' field is private. We use reflection to access it.
            var tableFieldInfo = AccessTools.Field(typeof(MainTabWindow_PawnTable), "table");
            var instanceTable = (PawnTable)tableFieldInfo.GetValue(__instance);
#else
            var instanceTable = __instance.table;
#endif
            if (instanceTable == null) return;

#if V1_3 || V1_2 || V1_1 || V1_0
            var instanceTableColumns = instanceTable.ColumnsListForReading; // Change to instanceTable.ColumnsListForReading for version 1.3 | Use instanceTable.Columns for version 1.4 >
#else
            var instanceTableColumns = instanceTable.Columns; // Change to instanceTable.ColumnsListForReading for version 1.3 | Use instanceTable.Columns for version 1.4 >
#endif

#if V1_1 || V1_0
            // In 1.1, these fields are private. Get them once via reflection before they are used.
            var v1_1_cachedColumnWidths = (List<float>)AccessTools.Field(typeof(PawnTable), "cachedColumnWidths").GetValue(instanceTable);
            var v1_1_cachedSize = (Vector2)AccessTools.Field(typeof(PawnTable), "cachedSize").GetValue(instanceTable);
            var v1_1_cachedHeaderHeight = (float)AccessTools.Field(typeof(PawnTable), "cachedHeaderHeight").GetValue(instanceTable);
#endif

            float hourBoxWidth = 19f; // default width for each hour box

            //Sumarbrander to CoolNether123: if we could eliminate this for loop, that would be great.
            for (var i = 0; i < instanceTableColumns.Count; i++)
            {
                if (ReferenceEquals(instanceTableColumns[i].workerClass, typeof(PawnColumnWorker_Timetable)))
                {
#if V1_1 || V1_0
                    hourBoxWidth = (v1_1_cachedColumnWidths[i] / 24f) - HOUR_BOX_GAP;
#else
                    hourBoxWidth = (instanceTable.cachedColumnWidths[i] / 24f) - HOUR_BOX_GAP; //24 hours in a day
#endif
                    break;
                }
#if V1_1 || V1_0
                var width = v1_1_cachedColumnWidths[i];
#else
                var width = instanceTable.cachedColumnWidths[i]; //instanceTableColumns.First().width; 
#endif

                fillRect.x += width;
                fillRect.width -= width;
            }

#if V1_1 || V1_0
            float windowHeight = Mathf.Max(v1_1_cachedSize.y - v1_1_cachedHeaderHeight - PAWN_AREA_BOTTOM_TRIM, 0);
#else
            float windowHeight = Mathf.Max(instanceTable.cachedSize.y - instanceTable.cachedHeaderHeight - PAWN_AREA_BOTTOM_TRIM, 0);
#endif

            foreach (var def in instanceTableColumns)
            {
                if (def.defName == "PawnColumnWorker_Timetable")
                {
                    // If the column is a timetable, set the BaseOffsetX to its width
                    BaseOffsetX = def.width + 1f; // Add 1px for the gap
                    break;
                }
            }

            // Check if the current map has changed
            if (Find.CurrentMap != lastKnownMap)
            {
                // Reset the flag and update the last known map
                dayNightColorsCalculated = false;
                lastKnownMap = Find.CurrentMap;
            }

            // 1) Day/Night Bar
            if (Settings.DrawHourBar)
            {
                bool incidentHappening = IncidentHappening();

                if (overrideDrawRegularBar)
                {
                    //draw regular day/night bar
                    DrawDayNightBar(fillRect, GetDaylightColors(), hourBoxWidth, HOUR_BOX_HEIGHT);
                }

                // Draw incident effects ON TOP of the previously drawn day/night bar.
                if (Settings.DrawIncidentOverlay && debugDrawOverlayBar && incidentHappening)
                {
                    Rect otherRect = fillRect;
                    DrawDayNightBar(otherRect, GetIncidentColors(), hourBoxWidth, HOUR_BOX_HEIGHT);
                }

                if (Settings.DrawHoursBarCursor)
                {
                    DrawDayNightTimeIndicator(fillRect, hourBoxWidth, HOUR_BOX_HEIGHT);
                }
            }
            // 2) Arrow and time-trace
            if (Settings.DrawArrow)
            {
                DrawArrowTexture(fillRect, hourBoxWidth);
            }

            // 3) Highlight bar
            if (Settings.DrawCurrentHourHighlight)
            {
                DrawHighlight(fillRect, windowHeight, hourBoxWidth, HOUR_BOX_HEIGHT);
            }

            // 4) Full-height vertical line
            if (Settings.DrawMainCursor)
            {
                DrawFullHeightCursor(fillRect, windowHeight, hourBoxWidth, HOUR_BOX_HEIGHT);
            }
        }

        static Color GetAuroraColor(float pos)
        {
            return MixColors(Settings.Color_Aurora1, Settings.Color_Aurora2, pos, true);
        }

        static Color MixColors(Color x, Color y, float a, bool includeAlpha = false)
        {
            Vector4 colorX = new Vector4(x.r, x.g, x.b, x.a);
            Vector4 colorY = new Vector4(y.r, y.g, y.b, y.a);
            Vector4 mixedColorVec = colorX * (1f - a) + colorY * a;
            return new Color(mixedColorVec.x, mixedColorVec.y, mixedColorVec.z, includeAlpha ? mixedColorVec.w : y.a);
        }

        #region Day/Night Bar
        private static Color[] GetDaylightColors()
        {
            Map map = Find.CurrentMap;
            Season season = GenLocalDate.Season(map);

            if (_cachedMap != map || _cachedSeason != season)
            {
                _cachedMap = map;
                _cachedSeason = season;
                for (int i = 0; i < _seasonDaylightCache.Length; i++) _seasonDaylightCache[i] = null;
            }

            int idx = (int)season;
            if (idx < 1 || idx >= _seasonDaylightCache.Length) idx = 1;
            if (_seasonDaylightCache[idx] == null)
            {
                Color[] colors = new Color[24];
                long currentAbsTick = GenTicks.TicksAbs;
                float dayPercent = GenLocalDate.DayPercent(map);
                long ticksIntoLocalDay = (long)(dayPercent * (float)GenDate.TicksPerDay);
                long startOfCurrentLocalDayAbsTick = currentAbsTick - ticksIntoLocalDay;

                for (int h = 0; h < 24; h++)
                {
                    long absTickForThisLocalHour = startOfCurrentLocalDayAbsTick + (long)h * GenDate.TicksPerHour;
#if V1_0 || V1_1 || V1_2
                    float sunlight = GenCelestial.CelestialSunGlow(map, (int)absTickForThisLocalHour);
#else
                    float sunlight = GenCelestial.CelestialSunGlow(map.Tile, (int)absTickForThisLocalHour);
#endif
                    colors[h] = GetColorForSunlight(sunlight);
                }
                _seasonDaylightCache[idx] = colors;
            }
            return _seasonDaylightCache[idx];
        }
        private static float GetCurrentDaylightForHour(int localHour)
        {
            long currentAbsTick = GenTicks.TicksAbs;
            float dayPercent = GenLocalDate.DayPercent(Find.CurrentMap);

            long ticksIntoLocalDay = (long)(dayPercent * (float)GenDate.TicksPerDay);
            long startOfCurrentLocalDayAbsTick = currentAbsTick - ticksIntoLocalDay;

            long absTickForThisLocalHour = startOfCurrentLocalDayAbsTick + (long)localHour * GenDate.TicksPerHour;

#if V1_0 || V1_1 || V1_2
            return GenCelestial.CelestialSunGlow(Find.CurrentMap, (int)absTickForThisLocalHour);
#else
            return GenCelestial.CelestialSunGlow(Find.CurrentMap.Tile, (int)absTickForThisLocalHour);
#endif
        }

        private static Color[] GetIncidentColors()
        {
            Color[] colors = new Color[24];

            for (int localHour = 0; localHour < 24; localHour++)
            {
                Color hourColor = Settings._DefaultTransparentColor;

                if (!Settings.DrawIncidentOverlay)
                {
                    colors[localHour] = hourColor;
                    continue;
                }

                if (isToxicFallout)
                {
                    var mixedColor = MixColors(hourColor, Settings.Color_ToxicFallout, 0.5f);
                    hourColor = mixedColor;
                }

                if (isVolcanicWinter)
                {
                    if (hourColor.r == Settings._DefaultTransparentColor.r && hourColor.g == Settings._DefaultTransparentColor.g && hourColor.b == Settings._DefaultTransparentColor.b)
                    {
                        hourColor = Settings.Color_VolcanicWinter;
                    }
                    else
                    {
                        var mixedColor = MixColors(hourColor, Settings.Color_VolcanicWinter, 0.5f, true);
                        hourColor = mixedColor;
                    }
                }

                if (isAurora)
                {
                    var hOffset = (float)localHour / 24f;
                    float loopingTime = 0.5f + 0.5f * Mathf.Cos(Time.time + hOffset);
                    Color auroraShimmerColor = GetAuroraColor(loopingTime);
#if V1_6U
                    auroraShimmerColor.a = Mathf.PerlinNoise1D(loopingTime).Remap(0f, 1f, Settings.AuroraMinOpacity, Settings.AuroraMaxOpacity);
#elif V1_5U
                    auroraShimmerColor.a = Mathf.PerlinNoise(loopingTime, loopingTime).Remap(0f, 1f, Settings.AuroraMinOpacity, Settings.AuroraMaxOpacity);
#else
                    float remappedPerlin = Settings.AuroraMinOpacity + (Settings.AuroraMaxOpacity - Settings.AuroraMinOpacity) * ((Mathf.PerlinNoise(loopingTime, loopingTime) - 0f) / (1f - 0f));
                    auroraShimmerColor.a = remappedPerlin;
#endif
                    hourColor = MixColors(hourColor, auroraShimmerColor, 0.5f);
                }
                colors[localHour] = hourColor;
            }
            return colors;
        }

        private static bool IncidentHappening()
        {
            isAurora = IsInTestMode ? overrideIsAurora : Find.CurrentMap.gameConditionManager.ConditionIsActive(GameConditionDefOf.Aurora) || overrideIsAurora;
            isEclipse = IsInTestMode ? overrideIsEclipse : Find.CurrentMap.gameConditionManager.ConditionIsActive(GameConditionDefOf.Eclipse) || overrideIsEclipse;
            isSolarFlare = IsInTestMode ? overrideIsSolarFlare : Find.CurrentMap.gameConditionManager.ConditionIsActive(SolarFlareDef) || overrideIsSolarFlare;
            isToxicFallout = IsInTestMode ? overrideIsToxicFallout : Find.CurrentMap.gameConditionManager.ConditionIsActive(GameConditionDefOf.ToxicFallout) || overrideIsToxicFallout;
            isVolcanicWinter = IsInTestMode ? overrideIsVolcanicWinter : Find.CurrentMap.gameConditionManager.ConditionIsActive(GameConditionDefOf.VolcanicWinter) || overrideIsVolcanicWinter;

            if (isEclipse || isSolarFlare)
                dayNightColorsCalculated = false;

            return isSolarFlare || isEclipse || isToxicFallout || isVolcanicWinter || isAurora;
        }
        private static Color GetColorForSunlight(float sunlightForHour)
        {
            if (Settings.DrawIncidentOverlay)
            {
                if (isEclipse)
                {
                    if (sunlightForHour > Settings.SunlightThreshold_Night)
                    {
                        return Settings.Color_DawnDusk;
                    }
                }

                if (isSolarFlare)
                {
                    if (sunlightForHour > Settings._SunlightThreshold_Any)
                    {
                        if (sunlightForHour >= Settings.SunlightThreshold_DawnDusk)
                        {
                            return Settings.Color_Day;
                        }
                    }
                }
            }

            if (sunlightForHour <= Settings.SunlightThreshold_Night)
                return Settings.Color_Night;

            if (sunlightForHour < Settings.SunlightThreshold_DawnDusk)
                return Settings.Color_DawnDusk;

            if (sunlightForHour < Settings.SunlightThreshold_SunriseSunset)
                return Settings.Color_SunriseSunset;

            return Settings.Color_Day;
        }

        private static void DrawHighlight(Rect fillRect, float windowHeight, float hourBoxWidth, float barHeight)
        {
            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f;
            int currentHour = (int)currentHourF;

            float colX = fillRect.x + BaseOffsetX
                         + currentHour * (hourBoxWidth + HOUR_BOX_GAP);
            float colY = fillRect.y + BASE_OFFSET_Y + barHeight + PAWN_AREA_TOP_OFFSET;

            Rect highlightRect = new Rect(colX, colY, hourBoxWidth, windowHeight);
            if (Settings.DoFilledHourHighlight)
                Widgets.DrawBoxSolid(highlightRect, Settings.Color_HourHighlight);
            else
#if V1_0 || V1_1 || V1_2 || V1_3
            {
                GUI.color = Settings.Color_HourHighlight;
                Widgets.DrawBox(highlightRect, 2);
                GUI.color = Color.white;
            }
#else
                Widgets.DrawBoxSolidWithOutline(highlightRect, Settings._HighlightInteriorColor, Settings.Color_HourHighlight, 2);
#endif
        }

        private static void DrawDayNightBar(Rect fillRect, Color[] colors, float hourBoxWidth, float hourBoxHeight)
        {
            float baseX = fillRect.x + BaseOffsetX;
            float baseY = fillRect.y + BASE_OFFSET_Y;

            for (int hour = 0; hour < 24; hour++)
            {
                float hourX = baseX + hour * (hourBoxWidth + HOUR_BOX_GAP);
                Rect hourRect = new Rect(hourX, baseY, hourBoxWidth, hourBoxHeight);
                Widgets.DrawBoxSolid(hourRect, colors[hour]);
            }
        }
        #endregion

        #region Time Trace Line
        private static void DrawDayNightTimeIndicator(Rect fillRect, float hourBoxWidth, float lineHeight)
        {
            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f;
            int currentHour = (int)currentHourF;
            float hourProgress = currentHourF - currentHour;

            float lineX = fillRect.x + BaseOffsetX
                        + currentHour * (hourBoxWidth + HOUR_BOX_GAP)
                        + hourProgress * hourBoxWidth;

            float lineY = fillRect.y + BASE_OFFSET_Y;

            long currentAbsoluteTick = GenTicks.TicksAbs;
#if V1_0 || V1_1 || V1_2
            float sunlight = GenCelestial.CelestialSunGlow(Find.CurrentMap, (int)currentAbsoluteTick);
#else
            float sunlight = GenCelestial.CelestialSunGlow(Find.CurrentMap.Tile, (int)currentAbsoluteTick);
#endif

            Color lineColor = !Settings.DoDynamicHoursBarLine ? Settings.Color_HoursBarCursor_Day : (sunlight >= Settings.SunlightThreshold_SunriseSunset) ? Settings.Color_HoursBarCursor_Day : Settings.Color_HoursBarCursor_Night;

            Rect traceRect = new Rect(lineX, lineY, Settings.HoursBarCursorThickness, lineHeight);
            Widgets.DrawBoxSolid(traceRect, lineColor);
        }

        private static void DrawArrowTexture(Rect fillRect, float hourBoxWidth)
        {
            if (ChronosPointerTextures.ArrowTexture == null) return;

            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f;
            int currentHour = (int)currentHourF;
            float hourProgress = currentHourF - currentHour;

            float arrowCenterX = fillRect.x + BaseOffsetX
                                + currentHour * (hourBoxWidth + HOUR_BOX_GAP)
                                + hourProgress * hourBoxWidth + 1f;

            float barTopY = fillRect.y + BASE_OFFSET_Y + 4;

            float arrowWidth = 8f;
            float arrowHeight = 8f;

            float arrowRectX = arrowCenterX - (arrowWidth / 2f);
            float arrowRectY = barTopY - arrowHeight - (!Settings.DrawHourBar ? -2f : 4f);

            Rect arrowRect = new Rect(arrowRectX, arrowRectY, arrowWidth, arrowHeight);

            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;

            GUIUtility.RotateAroundPivot(90f, arrowRect.center);

            GUI.color = Settings.Color_Arrow;
            GUI.DrawTexture(arrowRect.ScaledBy((!Settings.DrawHourBar ? 2 : 1)), ChronosPointerTextures.ArrowTexture);

            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }
        #endregion

        #region Full Height Cursor
        private static void DrawFullHeightCursor(Rect fillRect, float windowHeight, float hourBoxWidth, float hourBoxHeight)
        {
            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f;
            int currentHour = (int)currentHourF;
            float hourProgress = currentHourF - currentHour;

            float cursorX = fillRect.x + BaseOffsetX
                + currentHour * (hourBoxWidth + HOUR_BOX_GAP)
                + hourProgress * hourBoxWidth
                + 1f;

            float cursorThickness = Settings.CursorThickness;

            if (cursorThickness % 2 != 0)
            {
                cursorThickness += 1f;
            }

            cursorX -= cursorThickness / 2f;
            float cursorY = fillRect.y + BASE_OFFSET_Y + hourBoxHeight
                + PAWN_AREA_TOP_OFFSET;

            Rect cursorRect = new Rect(
                cursorX,
                cursorY,
                cursorThickness,
                windowHeight
            );

            Widgets.DrawBoxSolid(cursorRect, Settings.Color_MainCursor);
        }
    }
    #endregion
}
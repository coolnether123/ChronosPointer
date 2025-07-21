using HarmonyLib;
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

        // Extra offsets for highlight & line so they don’t slip off top/bottom
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

        public static bool AuroraActive => isAurora || overrideIsAurora;
        public static bool SolarFlareActive => isSolarFlare || overrideIsSolarFlare;
        public static bool EclipseActive => isEclipse || overrideIsEclipse;
        public static bool ToxicFalloutActive => isToxicFallout || overrideIsToxicFallout;
        public static bool VolcanicWinterActive => isVolcanicWinter || overrideIsVolcanicWinter;

        public static bool IsInTestMode = false;

        private static ChronosPointerSettings Settings => ChronosPointerMod.Settings;

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

            var instanceTable = __instance.table;
            if (instanceTable == null) return;

#if V1_3
            var instanceTableColumns = instanceTable.ColumnsListForReading; // Change to instanceTable.ColumnsListForReading for version 1.3 | Use instanceTable.Columns for version 1.4 >
#else
            var instanceTableColumns = instanceTable.Columns; // Change to instanceTable.ColumnsListForReading for version 1.3 | Use instanceTable.Columns for version 1.4 >
#endif
            float hourBoxWidth = 19f; // default width for each hour box

            //Sumarbrander to CoolNether123: if we could eliminate this for loop, that would be great.
            for (var i = 0; i < instanceTableColumns.Count; i++)
            {
                if (instanceTableColumns[i].workerClass == typeof(PawnColumnWorker_Timetable))
                {
                    hourBoxWidth = (instanceTable.cachedColumnWidths[i] / 24f) - HOUR_BOX_GAP; //24 hours in a day
                    break;
                }
                var width = instanceTable.cachedColumnWidths[i]; //instanceTableColumns.First().width; 

                fillRect.x += width;
                fillRect.width -= width;
            }

            //int incident = IncidentHappening() + incidentSimulator;

            float windowHeight = Mathf.Max(instanceTable.cachedSize.y - instanceTable.cachedHeaderHeight - PAWN_AREA_BOTTOM_TRIM, 0);

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
                //incidentsDirty = true; // Reset incident colors
                pawnCountCalculated = false;
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
                    //otherRect.y -= HOUR_BOX_HEIGHT / 2f;
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
        #region Compatability Patches

        #endregion

        static Color GetAuroraColor(float pos)
        {
            return MixColors(Settings.Color_Aurora1, Settings.Color_Aurora2, pos, true);
        }

        /// <summary>
        /// Return x * (1-a) + y * a, where x and y are colors, and a (between 0 and 1) is the mix factor. If includeAplha is false, the alpha of y is used.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="a"></param>
        /// <param name="includeAlpha"></param>
        /// <returns></returns>
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

            // Invalidate cache on map or season change
            if (_cachedMap != map || _cachedSeason != season)
            {
                _cachedMap = map;
                _cachedSeason = season;
                for (int i = 0; i < _seasonDaylightCache.Length; i++) _seasonDaylightCache[i] = null;
            }

            int idx = (int)season;
            if (idx < 1 || idx >= _seasonDaylightCache.Length) idx = 1; // fallback to Spring if Undefined
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
                    float sunlight = GenCelestial.CelestialSunGlow(map.Tile, (int)absTickForThisLocalHour);
                    
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

            return GenCelestial.CelestialSunGlow(Find.CurrentMap.Tile, (int)absTickForThisLocalHour);
        }

        private static Color[] GetIncidentColors()
        {
            Color[] colors = new Color[24];

            for (int localHour = 0; localHour < 24; localHour++)
            {
                Color hourColor = Settings._DefaultTransparentColor; // Default to transparent white

                // If doIncidentSpecials is false, do not show the incident colors.
                if (!Settings.DrawIncidentOverlay)
                {
                    colors[localHour] = hourColor;
                    continue;
                }

                float sunlight = GetCurrentDaylightForHour(localHour); // Get the current sunlight for the first hour
                //hourColor = GetColorForSunlight(sunlight); // Get the normal color for this hour

                if (isToxicFallout)
                {
                    var mixedColor = MixColors(hourColor, Settings.Color_ToxicFallout, 0.5f); // baseColorTF.b);
                    hourColor = mixedColor;
                }

                if (isVolcanicWinter)
                {
                    if (hourColor.r == Settings._DefaultTransparentColor.r && hourColor.g == Settings._DefaultTransparentColor.g && hourColor.b == Settings._DefaultTransparentColor.b) // mixing white with black makes gray, so if the hourColor is white, set it to black
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
#elif V1_5
                    auroraShimmerColor.a = Mathf.PerlinNoise(loopingTime, loopingTime).Remap(0f, 1f, Settings.AuroraMinOpacity, Settings.AuroraMaxOpacity);

#elif V1_4D

                    float remappedPerlin = Settings.AuroraMinOpacity + (Settings.AuroraMaxOpacity - Settings.AuroraMinOpacity) * ((Mathf.PerlinNoise(loopingTime, loopingTime) - 0f) / (1f - 0f));

                    auroraShimmerColor.a = remappedPerlin; //Mathf.PerlinNoise(loopingTime, loopingTime).Remap(0f, 1f, Settings.AuroraMinOpacity, Settings.AuroraMaxOpacity);
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

                //These are here because they change the base color of the day/night bar; they are not an overlay.
                if (isSolarFlare)
                {
                    if (sunlightForHour > Settings._SunlightThreshold_Any) // Only affect if there's normally some light
                    {
                        // If the base color is Orange or Yellow, Solar Flare makes/keeps it Yellow.
                        if (sunlightForHour >= Settings.SunlightThreshold_DawnDusk)
                        {
                            return Settings.Color_Day;
                        }
                    }
                }
            }
            // Deep night
            if (sunlightForHour <= Settings.SunlightThreshold_Night)
                return Settings.Color_Night;  // Deep Blue

            // Dawn/Dusk
            if (sunlightForHour < Settings.SunlightThreshold_DawnDusk)
                return Settings.Color_DawnDusk; // Light Blue

            // Sunrise/Sunset
            if (sunlightForHour < Settings.SunlightThreshold_SunriseSunset)
                return Settings.Color_SunriseSunset;   // Orange

            // Full daylight
            return Settings.Color_Day; // Default to daylight color;
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
                Widgets.DrawBoxSolidWithOutline(highlightRect, Settings._HighlightInteriorColor, Settings.Color_HourHighlight, 2);
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

        /// <summary>
        /// A small vertical line in the day/night bar, 2px wide for visibility.
        /// </summary>
        private static void DrawDayNightTimeIndicator(Rect fillRect, float hourBoxWidth, float lineHeight)
        {
            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f; // Correct for positioning
            int currentHour = (int)currentHourF;
            float hourProgress = currentHourF - currentHour;

            float lineX = fillRect.x + BaseOffsetX // fillRect here is UseMeForTheXYPosOfDayNightBar
                        + currentHour * (hourBoxWidth + HOUR_BOX_GAP)
                        + hourProgress * hourBoxWidth;

            float lineY = fillRect.y + BASE_OFFSET_Y; // fillRect here is UseMeForTheXYPosOfDayNightBar

            // For sunlight calculation for the dynamic line color, use the current absolute tick:
            long currentAbsoluteTick = GenTicks.TicksAbs;
            float sunlight = GenCelestial.CelestialSunGlow(Find.CurrentMap.Tile, (int)currentAbsoluteTick);

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

            // The line's X (center of arrow)
            float arrowCenterX = fillRect.x + BaseOffsetX
                                + currentHour * (hourBoxWidth + HOUR_BOX_GAP)
                                + hourProgress * hourBoxWidth + 1f;

            // The top of the day/night bar
            float barTopY = fillRect.y + BASE_OFFSET_Y + 4;

            // Will rotate the arrow later
            float arrowWidth = 8f; // Default width
            float arrowHeight = 8f; // Default height

            // Center the arrow horizontally on the line, 
            // so arrowRect.center.x = arrowCenterX
            float arrowRectX = arrowCenterX - (arrowWidth / 2f);

            // Changes the arrow up or down. up is -
            float arrowRectY = barTopY - arrowHeight - (!Settings.DrawHourBar ? -2f : 4f);

            // Build the rect
            Rect arrowRect = new Rect(arrowRectX, arrowRectY, arrowWidth, arrowHeight);

            // Save current matrix
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;

            // Rotate around center by +90 degrees to point downward
            GUIUtility.RotateAroundPivot(90f, arrowRect.center);

            // Draw the arrow
            GUI.color = Settings.Color_Arrow;
            ;

            GUI.DrawTexture(arrowRect.ScaledBy((!Settings.DrawHourBar ? 2 : 1)), ChronosPointerTextures.ArrowTexture);

            // Restore matrix & color
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

            // Calculate cursorX consistently with other elements and shift 1px to the right
            float cursorX = fillRect.x + BaseOffsetX
                + currentHour * (hourBoxWidth + HOUR_BOX_GAP)
                + hourProgress * hourBoxWidth
                + 1f; // Shift 1px to the right

            // Ensure cursorThickness is an even number
            float cursorThickness = Settings.CursorThickness;


            //Sumarbrander to CoolNether123: Is this needed?
            if (cursorThickness % 2 != 0)
            {
                cursorThickness += 1f; // Adjust to the next even number
            }

            // Adjust cursorX to center the thickness
            cursorX -= cursorThickness / 2f;

            // Top offset to match highlight
            float cursorY = fillRect.y + BASE_OFFSET_Y + hourBoxHeight
                + PAWN_AREA_TOP_OFFSET;

            Rect cursorRect = new Rect(
                cursorX,
                cursorY,
                cursorThickness, // Use the adjusted even thickness
                windowHeight
            );

            Widgets.DrawBoxSolid(cursorRect, Settings.Color_MainCursor);
        }


    }
    #endregion
}

using HarmonyLib;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions.Must;
using Verse;
using static UnityStandardAssets.ImageEffects.BloomOptimized;

namespace ChronosPointer
{

    //public static class Patch_DevQuicktest
    //{
    //           [HarmonyPatch(typeof(Root_Play), nameof(Root_Play.SetupForQuickTestPlay))]
    //    public static class Patch_DevQuickTest_DoWindowContents
    //    {
    //        [HarmonyPrefix]
    //        public static bool Prefix()
    //        {
    //            Log.Warning("You are using the Dev Quick Test patch. This is not intended to be in a release. Please report if you see this.");
    //            Current.ProgramState = ProgramState.Entry;
    //            Game.ClearCaches();
    //            Current.Game = new Game();
    //            Current.Game.InitData = new GameInitData();
    //            Current.Game.Scenario = ScenarioDefOf.Crashlanded.scenario;
    //            Find.Scenario.PreConfigure();
    //            Current.Game.storyteller = new Storyteller(StorytellerDefOf.Cassandra, DifficultyDefOf.Rough);
    //            Current.Game.World = WorldGenerator.GenerateWorld(0.03f, GenText.RandomSeedString(), OverallRainfall.Normal, OverallTemperature.Normal, OverallPopulation.AlmostNone, LandmarkDensity.Sparse);
    //            Find.GameInitData.ChooseRandomStartingTile();
    //            Find.GameInitData.mapSize = 250;
    //            Find.Scenario.PostIdeoChosen();
    //            return false;
    //        }
    //    }
    //}

    [HarmonyPatch(typeof(MainTabWindow_Schedule))]
    [HarmonyPatch("DoWindowContents")]
    public static class Patch_ScheduleWindow
    {
        #region Values
        public static Rect UseMeForTheXYPosOfDayNightBar;

        // Where the schedule grid starts
        private static float BaseOffsetX = 1f;// CalculateBaseOffsetX();
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

        // Flag to track if the pawn count has been calculated
        public static bool pawnCountCalculated = false;

        // Array to store the colors for each hour
        private static Color[] dayNightColors = new Color[24];
        private static Color[] incidentColors = new Color[24];

        // Add a static variable to store the last known map
        private static Map lastKnownMap = null;

        private const float SUNLIGHT_NIGHT = 0f;
        private const float SUNLIGHT_ANY = 0.05f;
        private const float SUNLIGHT_DAWN_DUSK = 0.35f;
        private const float SUNLIGHT_SUNRISE_SUNSET = 0.7f;


        private static bool isSolarFlare = false;
        private static bool isEclipse = false;
        private static bool isToxicFallout = false;
        private static bool isVolcanicWinter = false;
        private static bool isAurora = false;

        public static bool AuroraActive => isAurora || overrideIsAurora;
        public static bool SolarFlareActive => isSolarFlare || overrideIsSolarFlare;
        public static bool EclipseActive => isEclipse || overrideIsEclipse;
        public static bool ToxicFalloutActive => isToxicFallout || overrideIsToxicFallout;
        public static bool VolcanicWinterActive => isVolcanicWinter || overrideIsVolcanicWinter;

        public static bool IsInTestMode = false;
        
        [TweakValue("AAA - IsSolarFlare", 0, 1)]
        public static bool overrideIsSolarFlare = false;
        [TweakValue("AAB - IsEclipse", 0, 1)]
        public static bool overrideIsEclipse = false;
        [TweakValue("AAC - IsToxicFallout", 0, 1)]
        public static bool overrideIsToxicFallout = false;
        [TweakValue("AAD - IsVolcanicWinter ", 0, 1)]
        public static bool overrideIsVolcanicWinter = false;
        [TweakValue("AAE - IsAurora", 0, 1)]
        public static bool overrideIsAurora = false;
        [TweakValue("AAF - debugDrawRegularBar", 0, 1)]
        public static bool overrideDrawRegularBar = true;

        [TweakValue("AAG - debugDrawOverlayBar", 0, 1)]
        private static bool debugDrawOverlayBar = true;

        //private static bool incidentsDirty = false; // Flag to track if incidents need recalculation

        #endregion

        [TweakValue("incidentSimulator", 0, 6)]
        private static int incidentSimulator = 5; // For testing purposes, to simulate incidents

        [HarmonyPostfix]
        public static void Postfix(MainTabWindow_Schedule __instance, Rect fillRect)
        {
            if (Find.CurrentMap == null) return;

            var instanceTable = __instance.table;
#if rw_1_3
            var instanceTableColumns = instanceTable.ColumnsListForReading //for version 1.3 | Use instanceTable.Columns for version 1.4 >
#else
            var instanceTableColumns = instanceTable.Columns; // Change to instanceTable.ColumnsListForReading for version 1.3 | Use instanceTable.Columns for version 1.4 >
#endif
            float hourBoxWidth = 19f;

            for (var i = 0; i < instanceTableColumns.Count; i++)
            {
                if (instanceTableColumns[i].workerClass == typeof(PawnColumnWorker_Timetable))
                {
                    //var timetable = instanceTableColumns[i];

                    hourBoxWidth = (instanceTable.cachedColumnWidths[i] / 24f) - HOUR_BOX_GAP; //24 hours in a day
                    break;
                }
                var width = instanceTable.cachedColumnWidths[i]; //instanceTableColumns.First().width; 
                
                fillRect.x += width;
                fillRect.width -= width;
            }
            try
            {

                //int incident = IncidentHappening() + incidentSimulator;

                float windowHeight = Mathf.Max(__instance.table.cachedSize.y - __instance.table.cachedHeaderHeight - PAWN_AREA_BOTTOM_TRIM, 0);

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

                //if(IsIncidentActive())
                //{
                //    incidentsDirty = true; // Set the flag to true if any incident is active
                //}

                // 1) Day/Night Bar
                if (ChronosPointerMod.Settings.showDayNightBar)
                {
                    bool incidentHappening = IncidentHappening();

                    //if (!dayNightColorsCalculated||isSolarFlare||isEclipse)
                    //{
                    //    dayNightColors = GetDaylightColors();
                    //}

                    if (overrideDrawRegularBar)
                    {
                        //draw regular day/night bar
                        DrawDayNightBar(fillRect, GetDaylightColors(), hourBoxWidth, HOUR_BOX_HEIGHT);
                    }

                    // Draw incident effects ON TOP of the previously drawn day/night bar.
                    if (ChronosPointerMod.Settings.doIncidentSpecials && debugDrawOverlayBar && incidentHappening)
                    {
                        Rect otherRect = fillRect;
                        //otherRect.y -= HOUR_BOX_HEIGHT / 2f;
                        DrawDayNightBar(otherRect, GetIncidentColors(), hourBoxWidth, HOUR_BOX_HEIGHT);
                    }

                    if (ChronosPointerMod.Settings.showDayNightIndicator)
                    {
                        DrawDayNightTimeIndicator(fillRect, hourBoxWidth, HOUR_BOX_HEIGHT);
                    }
                }
                // 2) Arrow and time-trace
                if (ChronosPointerMod.Settings.enableArrow)
                {
                    DrawArrowTexture(fillRect, hourBoxWidth);
                }


                // 3) Highlight bar
                if (ChronosPointerMod.Settings.showHighlight)
                {
                    DrawHighlight(fillRect, windowHeight, hourBoxWidth, HOUR_BOX_HEIGHT);
                }

                // 4) Full-height vertical line
                if (ChronosPointerMod.Settings.showPawnLine)
                {
                    DrawFullHeightCursor(fillRect, windowHeight, hourBoxWidth, HOUR_BOX_HEIGHT);
                }
            }
            catch (Exception e)
            {
                Log.Error($"ChronosPointer: Error in schedule patch - {e.Message}\n{e.StackTrace}");
            }

            //if(incidentsDirty != IsIncidentActive())
            //{
            //    incidentsDirty = true;
            //}
        }

        #region Compatability Patches
        // Define methods for specific fixes
        private static void ApplyFixForMysteriusCustomSchedules()
        {
            // Implement the fix logic for Mysterius.CustomSchedules
            Find.WindowStack.Add(new Dialog_MessageBox("Custom Schedules (continued) is Active. Chronos Pointer will have overlap", "OK"));
            //Log.Error("Custom Schedules (continued) is Active. Chronos Pointer will have overlap");
        }

        #endregion

        //static Color first_color = ;
        //static Color second_color =;
        //static float position = 0.0f;
        //static float size = 1.0f;
        //static float angle = 0.0f;

        static Color GetAuroraColor(float pos)
        {
            return MixColors(new Color(1.0f, 0.5f, 1.0f, 1.0f), new Color(0.5f, 1.0f, 0.5f, 1.0f), pos, true);
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
        private static void CalculateDayNightColors(int incident)
        {
            long currentAbsTick = GenTicks.TicksAbs;
            float dayPercent = GenLocalDate.DayPercent(Find.CurrentMap); 
            long ticksIntoLocalDay = (long)(dayPercent * (float)GenDate.TicksPerDay);
            long startOfCurrentLocalDayAbsTick = currentAbsTick - ticksIntoLocalDay; 

            for (int localHour = 0; localHour < 24; localHour++)
            {
                long absTickForThisLocalHour = startOfCurrentLocalDayAbsTick + (long)localHour * GenDate.TicksPerHour;
                float sunlight = GenCelestial.CelestialSunGlow(Find.CurrentMap.Tile, (int)absTickForThisLocalHour);
                Color baseSunlightColor = GetColorForSunlight(sunlight); // Get the normal color for this hour

                // Start with the actual sunlight color for this hour
                dayNightColors[localHour] = baseSunlightColor;

                
            }
        }

        private static Color[] GetDaylightColors()
        {
            Color[] colors = new Color[24];
            for (int localHour = 0; localHour < 24; localHour++)
            {
                colors[localHour] = GetColorForSunlight(GetCurrentDaylightForHour(localHour)); // Get the normal color for this hour
            }
            dayNightColorsCalculated = true;

            return colors;
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
                Color hourColor = new Color(1,1,1, 0); // Default to transparent

                // If doIncidentSpecials is false, do not show the incident colors.
                if (!ChronosPointerMod.Settings.doIncidentSpecials)
                {
                    colors[localHour] = hourColor;
                    continue;
                }

                float sunlight = GetCurrentDaylightForHour(localHour); // Get the current sunlight for the first hour
                //hourColor = GetColorForSunlight(sunlight); // Get the normal color for this hour

                if (isToxicFallout)
                {
                    var mixedColor = MixColors(hourColor, new Color(hourColor.r*0.25f, hourColor.g * 1.6f, hourColor.b*0.25f, 0.75f), 0.5f); // baseColorTF.b);
                    hourColor = mixedColor;
                }

                if (isVolcanicWinter)
                {
                    if(hourColor.r == 1f && hourColor.g == 1f && hourColor.b == 1f)
                    {
                        hourColor = Color.black;
                    }
                    //hourColor = new Color(0.5f, 0.5f, 0.5f, 0.75f); // Default to gray if no sunlight
                    var mixedColor = MixColors(hourColor, new Color(0f, 0f, 0f, 0.75f), 0.5f, true);
                    hourColor = mixedColor;
                }

                if (isAurora)
                {
                    var hOffset = (float)localHour / 24f;
                    float loopingTime = 0.5f + 0.5f * Mathf.Cos(Time.time + hOffset);
                    Color auroraShimmerColor = GetAuroraColor(loopingTime);     //Settings.minOpacity, MaxOpacity
                    auroraShimmerColor.a = Mathf.PerlinNoise1D(loopingTime).Remap(0f, 1f, 0.1f, 0.75f);
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


            if(isEclipse || isSolarFlare)
                dayNightColorsCalculated = false;

            return isSolarFlare || isEclipse || isToxicFallout || isVolcanicWinter || isAurora;
        }
        private static Color GetColorForSunlight(float sunlightForHour)
        {
            if (ChronosPointerMod.Settings.doIncidentSpecials)
            {
                if (isEclipse)
                {
                    if (sunlightForHour > SUNLIGHT_NIGHT)
                    {
                        return ChronosPointerMod.Settings.dawnDuskColor;
                    }
                }

                //These are here because they change the base color of the day/night bar; they are not an overlay.
                if (isSolarFlare)
                {
                    if (sunlightForHour > SUNLIGHT_ANY) // Only affect if there's normally some light
                    {
                        // If the base color is Orange or Yellow, Solar Flare makes/keeps it Yellow.
                        if (sunlightForHour >= SUNLIGHT_DAWN_DUSK)
                        {
                            return ChronosPointerMod.Settings.dayColor;
                        }
                    }
                }
            }
            // Deep night
            if (sunlightForHour == SUNLIGHT_NIGHT)
                return ChronosPointerMod.Settings.nightColor;  // Deep Blue

            // Dawn/Dusk
            if (sunlightForHour < SUNLIGHT_DAWN_DUSK)
                return ChronosPointerMod.Settings.dawnDuskColor; // Light Blue

            // Sunrise/Sunset
            if (sunlightForHour < SUNLIGHT_SUNRISE_SUNSET)
                return ChronosPointerMod.Settings.sunriseSunsetColor;   // Orange

            // Full daylight
            return ChronosPointerMod.Settings.dayColor; // Default to daylight color;
        }

        private static void DrawHighlight(Rect fillRect, float windowHeight, float hourBoxWidth, float barHeight)
        {
            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f;
            int currentHour = (int)currentHourF;

            float colX = fillRect.x + BaseOffsetX
                         + currentHour * (hourBoxWidth + HOUR_BOX_GAP);
            float colY = fillRect.y + BASE_OFFSET_Y + barHeight + PAWN_AREA_TOP_OFFSET;

            Rect highlightRect = new Rect(colX, colY, hourBoxWidth, windowHeight);
            if (ChronosPointerMod.Settings.hollowHourHighlight)
                Widgets.DrawBoxSolidWithOutline(highlightRect, new Color(0, 0, 0, 0), ChronosPointerMod.Settings.highlightColor, 2);
            else
                Widgets.DrawBoxSolid(highlightRect, ChronosPointerMod.Settings.highlightColor);
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

            Color lineColor = !ChronosPointerMod.Settings.useDynamicTimeTraceLine ? ChronosPointerMod.Settings.timeTraceColorDay : (sunlight >= 0.7f) ? ChronosPointerMod.Settings.timeTraceColorDay : ChronosPointerMod.Settings.timeTraceColorNight;

            Rect traceRect = new Rect(lineX, lineY, ChronosPointerMod.Settings.dayNightBarCursorThickness, lineHeight);
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
            float arrowRectY = barTopY - arrowHeight - (!ChronosPointerMod.Settings.showDayNightBar ? -2f : 4f);

            // Build the rect
            Rect arrowRect = new Rect(arrowRectX, arrowRectY, arrowWidth, arrowHeight);

            // Save current matrix
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;

            // Rotate around center by +90 degrees to point downward
            GUIUtility.RotateAroundPivot(90f, arrowRect.center);

            // Draw the arrow
            GUI.color = ChronosPointerMod.Settings.arrowColor;
            ;

            GUI.DrawTexture(arrowRect.ScaledBy((!ChronosPointerMod.Settings.showDayNightBar ? 2 : 1)), ChronosPointerTextures.ArrowTexture);

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
            float cursorThickness = ChronosPointerMod.Settings.cursorThickness;
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

            Widgets.DrawBoxSolid(cursorRect, ChronosPointerMod.Settings.bottomCursorColor);
        }

        
    }
    #endregion


}
using System;
using System.Collections.Generic;

using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;

using Verse;

namespace ChronosPointer
{
    [HarmonyPatch(typeof(MainTabWindow_Schedule))]
    [HarmonyPatch("DoWindowContents")]
    public static class Patch_ScheduleWindow
    {
        #region Values
        public static Rect UseMeForTheXYPosOfDayNightBar;

        // Where the schedule grid starts
        private static float BaseOffsetX = 1f;// CalculateBaseOffsetX();
        private const float BaseOffsetY = 40;

        // Each hour cell
        private const float HourBoxWidth = 19f;
        private const float HourBoxGap = 2f;

        // Pawn row
        private const float PawnRowHeight = 28f; 
        private const float PawnRowGap = 2f;

        // Day/night bar
        private const float BarHeight = 10f;

        // Extra offsets for highlight & line so they don’t slip off top/bottom
        private const float PawnAreaTopOffset = 16f;
        private const float PawnAreaBottomTrim = 2f;

        // Define the SolarFlare condition if not available
        private static readonly GameConditionDef SolarFlareDef = DefDatabase<GameConditionDef>.GetNamed("SolarFlare");

        // Flag to track if the day/night colors have been calculated
        public static bool dayNightColorsCalculated = false;

        // Flag to track if the pawn count has been calculated
        public static bool pawnCountCalculated = false;

        // Array to store the colors for each hour
        private static Color[] dayNightColors = new Color[24];

        // Add a static variable to store the last known map
        private static Map lastKnownMap = null;

        //cached number of pawns for the full height and highlight bars
        private static int pawnCount = 0;

        // Make pawn width smaller if CompactWorkTab is active
        private static int CompactWorkTab = 0;

        // Custom Schedules (continued) mod ID
        // Change the single string to an array of strings
        private static readonly string[] customSchedulesModIds = new string[]
        {
            "Mysterius.CustomSchedules",
            "Mlie.CompactWorkTab"
        };
        #endregion

        [HarmonyPostfix]
        public static void Postfix(MainTabWindow_Schedule __instance, Rect fillRect)
        {
            if (Find.CurrentMap == null) return;

            var instanceTable = __instance.table;
            var instanceTableColumns = instanceTable.Columns; // Change to instanceTable.ColumnsListForReading for version 1.3 | Use instanceTable.Columns for version 1.4 >
            for (var i = 0; i < instanceTableColumns.Count; i++)
            {
                if (instanceTableColumns[i].workerClass == typeof(PawnColumnWorker_Timetable))
                {
                    break;
                }
                fillRect.x += instanceTable.cachedColumnWidths[i];
            }

            try
            {

                // Iterate over each mod ID and check if it's active
                foreach (var modId in customSchedulesModIds)
                {
                    bool isModActive = ModLister.AllInstalledMods.Any(mod =>
                        mod.Active && mod.PackageId.Equals(modId, StringComparison.OrdinalIgnoreCase));

                    if (isModActive)
                    {
                        // Trigger specific fixes based on the active mod
                        switch (modId)
                        {
                            case "Mysterius.CustomSchedules":

                                ApplyFixForMysteriusCustomSchedules();
                                break;
                            case "Mlie.CompactWorkTab":
                                ApplyFixForMlieCompactWorkTab();
                                break;
                        }
                    }
                }
                int incident = IncidentHappening();


                //if (!pawnCountCalculated)
                pawnCount = GetPawnCount(__instance);

                // Check if the current map has changed
                if (Find.CurrentMap != lastKnownMap)
                {
                    // Reset the flag and update the last known map
                    dayNightColorsCalculated = false;
                    pawnCountCalculated = false;
                    lastKnownMap = Find.CurrentMap;
                }

                // 1) Day/Night Bar
                if (ChronosPointerMod.Settings.showDayNightBar)
                {
                    if (!dayNightColorsCalculated || incident >= 0)
                    {
                        CalculateDayNightColors(incident);

                    }
                    DrawDayNightBar(fillRect, dayNightColors);
                    if (incident == 5) // Check if Aurora is the active incident
                    {
                        // Create a NEW array specifically for the Aurora overlay colors.
                        Color[] auroraEffectOverlay = new Color[24];

                        float time = Time.time; // Cache Time.time for slight optimization and consistency within this frame
                        Color auroraShimmerColor = new Color(
                            0.3f + Mathf.Abs(Mathf.Sin(time * 0.7f + 0.5f)) * 0.5f,  // Red component (e.g., varying between 0.3 and 0.8)
                            0.5f + Mathf.Abs(Mathf.Sin(time * 0.9f + 1.0f)) * 0.4f,  // Green component (e.g., varying between 0.5 and 0.9)
                            0.6f + Mathf.Abs(Mathf.Sin(time * 0.5f + 1.5f)) * 0.4f,  // Blue component (e.g., varying between 0.6 and 1.0)
                            0.25f + Mathf.Abs(Mathf.Sin(time * 0.4f)) * 0.20f        // Alpha component (e.g., varying between 0.25 and 0.45 for transparency)
                        );

                        // Apply this shimmering color to all hours for the overlay.
                        for (int hour = 0; hour < 24; hour++)
                        {
                            auroraEffectOverlay[hour] = auroraShimmerColor;
                        }

                        // Draw the Aurora overlay ON TOP of the previously drawn day/night bar.
                        DrawDayNightBar(fillRect, auroraEffectOverlay);
                    }

                    if (ChronosPointerMod.Settings.showDayNightIndicator)
                    {
                        DrawDayNightTimeIndicator(fillRect);
                    }
                }
                // 2) Arrow and time-trace
                if (ChronosPointerMod.Settings.enableArrow)
                {
                    DrawArrowTexture(fillRect);
                }

                //Don't draw the pawn bars if there are no pawns
                if (pawnCount > 0)
                {

                    // 3) Highlight bar
                    if (ChronosPointerMod.Settings.showHighlight)
                    {


                        DrawHighlight(fillRect, pawnCount);
                    }

                    // 4) Full-height vertical line
                    if (ChronosPointerMod.Settings.showPawnLine)
                    {
                        DrawFullHeightCursor(fillRect, pawnCount);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"ChronosPointer: Error in schedule patch - {e.Message}\n{e.StackTrace}");
            }
        }

        #region Compatability Patches
        /*private static float CalculateBaseOffsetX()
        {
            float offsetX = 202f + (ModsConfig.BiotechActive ? 26f : 0) + (ModsConfig.IdeologyActive ? 26f : 0);

            // Check specifically for the "defaults.1trickPwnyta" mod
            if (ModLister.AllInstalledMods.Any(mod => mod.Active && mod.PackageId.Equals("defaults.1trickPwnyta", StringComparison.OrdinalIgnoreCase)))
            {
                offsetX += 36f;
            }

            return offsetX;
        }*/

        // Define methods for specific fixes
        private static void ApplyFixForMysteriusCustomSchedules()
        {
            // Implement the fix logic for Mysterius.CustomSchedules
            Log.Error("Custom Schedules (continued) is Active. Chronos Pointer will have overlap");
        }

        private static void ApplyFixForMlieCompactWorkTab()
        {
            CompactWorkTab = 1;
        }

        #endregion


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

                switch (incident)
                {
                    case 1: // Solar Flare
                        if (sunlight > 0.05f) // Only affect if there's normally some light
                        {
                            // If the base color is Orange or Yellow, Solar Flare makes/keeps it Yellow.
                            if (baseSunlightColor == new Color(1f, 0.5f, 0f) || baseSunlightColor == Color.yellow)
                            {
                                dayNightColors[localHour] = Color.yellow;
                            }
                        }
                        break;
                    case 2: // Eclipse
                        dayNightColors[localHour] = new Color(0f, 0f, 0.5f);  // Deep Blue
                        break;
                    case 3: // Toxic Fallout (modifies base color)
                        Color baseColorTF = dayNightColors[localHour];
                        dayNightColors[localHour] = new Color(baseColorTF.r, baseColorTF.g * 1.6f, baseColorTF.b);
                        break;
                    case 4: // Volcanic Winter (modifies base color)
                        Color baseColorVW = dayNightColors[localHour];
                        dayNightColors[localHour] = new Color(baseColorVW.r * 0.5f, baseColorVW.g * 0.5f, baseColorVW.b * 0.5f);
                        break;
                        // case 0 or default: no incident, base color from sunlight is used.
                        // case 5 (Aurora) is handled separately as an overlay in Postfix.
                }
            }
            dayNightColorsCalculated = true;
        }

        private static int IncidentHappening()
        {
            int incident = 0;

            // Check for solar flare all yellow
            bool isSolarFlare = Find.CurrentMap.gameConditionManager.ConditionIsActive(SolarFlareDef);
            //bool isSolarFlare = false;
            // Check for eclipse all dark blue
            bool isEclipse = Find.CurrentMap.gameConditionManager.ConditionIsActive(GameConditionDefOf.Eclipse);
            // Green tinge
            bool isToxicFallout = Find.CurrentMap.gameConditionManager.ConditionIsActive(GameConditionDefOf.ToxicFallout);
            bool isVolcanicWinter = Find.CurrentMap.gameConditionManager.ConditionIsActive(GameConditionDefOf.VolcanicWinter);
            bool isAurora = Find.CurrentMap.gameConditionManager.ConditionIsActive(GameConditionDefOf.Aurora);

            if (isSolarFlare == true)
            {
                incident = 1;
            }
            else if (isEclipse == true)
            {
                incident = 2;
            }
            else if (isToxicFallout == true)
            {
                incident = 3;
            }
            else if (isVolcanicWinter == true)
            {
                incident = 4;
            }
            else if (isAurora == true)
            {
                incident = 5;
            }

            return incident;
        }

        private static void DrawDayNightBar(Rect fillRect, Color[] colors)
        {
            float baseX = fillRect.x + BaseOffsetX;
            float baseY = fillRect.y + BaseOffsetY;


            for (int hour = 0; hour < 24; hour++)
            {
                float hourX = baseX + hour * (HourBoxWidth + HourBoxGap);
                Rect hourRect = new Rect(hourX, baseY, HourBoxWidth, BarHeight);

                Widgets.DrawBoxSolid(hourRect, colors[hour]);
            }

        }
        private static Color GetColorForSunlight(float sunlight)
        {
            
            // Deep night
            if (sunlight == 0f)
                return new Color(0f, 0f, 0.5f);  // Deep Blue

            // Dawn/Dusk
            if (sunlight < 0.35f)
                return new Color(0.5f, 0.5f, 1f); // Light Blue

            // Sunrise/Sunset
            if (sunlight < 0.7f)
                return new Color(1f, 0.5f, 0f);   // Orange

            // Full daylight
            return Color.yellow;
        }
        private static void DrawHighlight(Rect fillRect, int pawnCount)
        {
            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f;
            int currentHour = (int)currentHourF;

            float colX = fillRect.x + BaseOffsetX
                         + currentHour * (HourBoxWidth + HourBoxGap);
            float colY = fillRect.y + BaseOffsetY + BarHeight + PawnAreaTopOffset;


            float totalHeight = pawnCount * (PawnRowHeight + PawnRowGap);
            // Trim from bottom
            totalHeight -= PawnAreaBottomTrim;



            Rect highlightRect = new Rect(colX, colY, HourBoxWidth, totalHeight);
            if (ChronosPointerMod.Settings.hollowHourHighlight)
                Widgets.DrawBoxSolidWithOutline(highlightRect, new Color(0, 0, 0, 0), ChronosPointerMod.Settings.highlightColor, 2);
            else
                Widgets.DrawBoxSolid(highlightRect, ChronosPointerMod.Settings.highlightColor);
        }
        #endregion

        #region Time Trace Line

        static int GetPawnCount(MainTabWindow_Schedule __instance)
        {
            //var babyList = Find.CurrentMap.mapPawns.SpawnedBabiesInFaction(Find.FactionManager.OfPlayer).ToList();
            int babyCount = 0;

            if (__instance == null)
            {
                Log.Error("Instance is null!");
                return 0;
            }

            //babies that are held do not count as spawned, but do still count as pawns. So when a mother breastfeeds her baby, the pawn highlight bar is off by the number of breastfed babies.

            // Check if the method exists and is accessible

            var field = __instance.GetType().GetProperty("Pawns", BindingFlags.Instance | BindingFlags.NonPublic);

            

            if (field == null)
            {
                Log.Error("Field is null!");
                return 0;
            }
            var pawnsIEnumerable = field.GetValue(__instance) as IEnumerable<Pawn>;
            if (pawnsIEnumerable == null)
            {
                Log.Error("pawnsIEnum is null!");
                return 0;
            }
            int totalHeight = pawnsIEnumerable.Count(); 
        
            pawnCountCalculated = true;
        
            return totalHeight;
        }

        /// <summary>
        /// A small vertical line in the day/night bar, 2px wide for visibility.
        /// </summary>
        private static void DrawDayNightTimeIndicator(Rect fillRect)
        {
            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f; // Correct for positioning
            int currentHour = (int)currentHourF;
            float hourProgress = currentHourF - currentHour;

            float lineX = fillRect.x + BaseOffsetX // fillRect here is UseMeForTheXYPosOfDayNightBar
                        + currentHour * (HourBoxWidth + HourBoxGap)
                        + hourProgress * HourBoxWidth;

            float lineY = fillRect.y + BaseOffsetY; // fillRect here is UseMeForTheXYPosOfDayNightBar
            float lineHeight = BarHeight;

            // For sunlight calculation for the dynamic line color, use the current absolute tick:
            long currentAbsoluteTick = GenTicks.TicksAbs;
            float sunlight = GenCelestial.CelestialSunGlow(Find.CurrentMap.Tile, (int)currentAbsoluteTick);

            Color lineColor = !ChronosPointerMod.Settings.useDynamicTimeTraceLine ? ChronosPointerMod.Settings.timeTraceColorDay : (sunlight >= 0.7f) ? ChronosPointerMod.Settings.timeTraceColorDay : ChronosPointerMod.Settings.timeTraceColorNight;

            Rect traceRect = new Rect(lineX, lineY, 2f, lineHeight);
            Widgets.DrawBoxSolid(traceRect, lineColor);
        }

        private static void DrawArrowTexture(Rect fillRect)
        {
            if (ChronosPointerTextures.ArrowTexture == null) return;

            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f;
            int currentHour = (int)currentHourF;
            float hourProgress = currentHourF - currentHour;

            // The line's X (center of arrow)
            float arrowCenterX = fillRect.x + BaseOffsetX
                                + currentHour * (HourBoxWidth + HourBoxGap)
                                + hourProgress * HourBoxWidth + 1f;

            // The top of the day/night bar
            float barTopY = fillRect.y + BaseOffsetY + 4;

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
        private static void DrawFullHeightCursor(Rect fillRect, int pawnCount)
        {
            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f;
            int currentHour = (int)currentHourF;
            float hourProgress = currentHourF - currentHour;

            // Calculate cursorX consistently with other elements and shift 1px to the right
            float cursorX = fillRect.x + BaseOffsetX
                + currentHour * (HourBoxWidth + HourBoxGap)
                + hourProgress * HourBoxWidth
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
            float cursorY = fillRect.y + BaseOffsetY + BarHeight
                + PawnAreaTopOffset;

            float totalHeight = pawnCount
                * (PawnRowHeight + PawnRowGap);

            // Trim from bottom
            totalHeight -= PawnAreaBottomTrim;

            Rect cursorRect = new Rect(
                cursorX,
                cursorY,
                cursorThickness, // Use the adjusted even thickness
                totalHeight
            );

            Widgets.DrawBoxSolid(cursorRect, ChronosPointerMod.Settings.bottomCursorColor);
        }


        // Method to reset the flag when the scheduler is closed

    }
    #endregion


}
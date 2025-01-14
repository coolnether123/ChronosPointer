﻿using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using Verse;

namespace ChronosPointer
{
    [HarmonyPatch(typeof(MainTabWindow_Schedule))]
    [HarmonyPatch("DoWindowContents")]
    public static class Patch_ScheduleWindow
    {
        // Where the schedule grid starts
        private const float BaseOffsetX = 202f;
        private const float BaseOffsetY = 40f;

        // Each hour cell
        private const float HourBoxWidth = 19f;
        private const float HourBoxGap = 2f;

        // Pawn row
        private const float PawnRowHeight = 28f; // user wants 28f
        private const float PawnRowGap = 2f;

        // Day/night bar
        private const float BarHeight = 10f;

        // Extra offsets for highlight & line so they don’t slip off top/bottom
        // (Adjust to taste if you see they still slip a bit.)
        private const float PawnAreaTopOffset = 16f;
        private const float PawnAreaBottomTrim = 2f;

        // Define the SolarFlare condition if not available
        private static readonly GameConditionDef SolarFlareDef = DefDatabase<GameConditionDef>.GetNamed("SolarFlare");

        // Flag to track if the day/night colors have been calculated
        private static bool dayNightColorsCalculated = false;

        // Array to store the colors for each hour
        private static Color[] dayNightColors = new Color[24];

        // Add a static variable to store the last known map
        private static Map lastKnownMap = null;

        // Timer for color animation
        private static float animationTimer = 0f;
        private static float animationDuration = 2f; // 2 seconds duration
        private static Color[] initialColors = new Color[24];
        private static Color targetColor = new Color(0.5f, 0f, 0.5f, 0.5f);

        [HarmonyPostfix]
        public static void Postfix(MainTabWindow_Schedule __instance, Rect fillRect)
        {
            if (Find.CurrentMap == null) return;

            try
            {
                int incident = IncidentHappening();
                // Check if the current map has changed
                if (Find.CurrentMap != lastKnownMap)
                {
                    // Reset the flag and update the last known map
                    dayNightColorsCalculated = false;
                    lastKnownMap = Find.CurrentMap;
                }

                // 1) Day/Night Bar
                if (ChronosPointerMod.Settings.showDayNightBar)
                {
                    if (!dayNightColorsCalculated || incident > 0)
                    {
                        CalculateDayNightColors(incident);
                        
                    }
                        DrawDayNightBar(fillRect, dayNightColors);
                    if (incident == 5)
                    {
                        Color[] Aurora = dayNightColors;
                        Color newColor = new Color(0.5f, Mathf.Abs(Mathf.Sin(Time.time)), 0.5f, Mathf.Abs(Mathf.Sin(Time.time * 0.6f)) * 0.5f);
                        for (int hour = 0; hour < 24; hour++)
                        {
                            Aurora[hour] = newColor;
                        }
                        DrawDayNightBar(fillRect, Aurora);

                    }
                }

                // 2) Highlight bar
                if (ChronosPointerMod.Settings.showHighlight)
                {
                    DrawHighlight(fillRect);
                }

                // 3) Arrow and time-trace
                if (ChronosPointerMod.Settings.enableArrow)
                {
                    DrawArrowTexture(fillRect);
                }

                // 4) Full-height vertical line
                if (ChronosPointerMod.Settings.showPawnLine)
                {
                    DrawFullHeightCursor(fillRect);
                }
            }
            catch (Exception e)
            {
                Log.Error($"ChronosPointer: Error in schedule patch - {e.Message}\n{e.StackTrace}");
            }
        }

        private static void CalculateDayNightColors(int incident)
        {
            Log.Message("incident " + incident);
            for (int hour = 0; hour < 24; hour++)
            {
                switch (incident)
                {
                    case 1: // Solar Flare
                        dayNightColors[hour] = Color.yellow;
                        break;
                    case 2: // Eclipse
                        dayNightColors[hour] = new Color(0f, 0f, 0.5f);  // Deep Blue
                        break;
                    case 3: // Toxic Fallout
                        Color tingeGreen = dayNightColors[hour];
                        dayNightColors[hour] = new Color(tingeGreen.r, tingeGreen.g * 1.6f, tingeGreen.b);
                        break;
                    case 4: // Volcanic Winter
                        Color darker = dayNightColors[hour];
                        dayNightColors[hour] = new Color(darker.r * 0.5f, darker.g * 0.5f, darker.b * 0.5f);
                        break;
                    default:
                        
                        float sunlight = GenCelestial.CelestialSunGlow(Find.CurrentMap.Tile, hour * 2500);
                        Log.Message("At hour " + hour + " sunlight == " + sunlight);
                        dayNightColors[hour] = GetColorForSunlight(sunlight);
                        break;
                }
            }
        }

        private static int IncidentHappening()
        {
            int incident = 5;

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
        private static void DrawHighlight(Rect fillRect)
        {
            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f;
            int currentHour = (int)currentHourF;

            float colX = fillRect.x + BaseOffsetX
                         + currentHour * (HourBoxWidth + HourBoxGap);
            float colY = fillRect.y + BaseOffsetY + BarHeight + PawnAreaTopOffset;

            var babyList = Find.CurrentMap.mapPawns.SpawnedBabiesInFaction(Find.FactionManager.OfPlayer).ToList();
            int babyCount = 0;
            if (babyList != null)
                babyCount = babyList.Count;

            float totalHeight = (Find.CurrentMap.mapPawns.ColonistCount
                - babyCount)
                * (PawnRowHeight + PawnRowGap);
            // Trim from bottom
            totalHeight -= PawnAreaBottomTrim;

            Rect highlightRect = new Rect(colX, colY, HourBoxWidth, totalHeight);
            if (ChronosPointerMod.Settings.hollowHourHighlight)
                Widgets.DrawBoxSolidWithOutline(highlightRect, new Color(0, 0, 0, 0), ChronosPointerMod.Settings.highlightColor, 2);
            else
                Widgets.DrawBoxSolid(highlightRect, ChronosPointerMod.Settings.highlightColor);
        }

        #region Time Trace Line
        /// <summary>
        /// A small vertical line in the day/night bar, 2px wide for visibility.
        /// </summary>
        private static void DrawTimeTraceLine(Rect fillRect)
        {
            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f;
            int currentHour = (int)currentHourF;
            float hourProgress = currentHourF - currentHour;

            float lineX = fillRect.x + BaseOffsetX
                        + currentHour * (HourBoxWidth + HourBoxGap)
                        + hourProgress * HourBoxWidth;

            float lineY = fillRect.y + BaseOffsetY;
            float lineHeight = BarHeight;

            // Get the sunlight value for the current hour
            float sunlight = GenCelestial.CelestialSunGlow(Find.CurrentMap.Tile, (int)(currentHourF * 2500));

            // Determine the color of the line based on the sunlight
            Color lineColor = !ChronosPointerMod.Settings.useDynamicTimeTraceLine ? ChronosPointerMod.Settings.timeTraceColorDay : (sunlight >= 0.7f) ? ChronosPointerMod.Settings.timeTraceColorDay : ChronosPointerMod.Settings.timeTraceColorNight;

            // 2 px wide
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

        private static void DrawFullHeightCursor(Rect fillRect)
        {
            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f;
            int currentHour = (int)currentHourF;
            float hourProgress = currentHourF - currentHour;

            float cursorX = fillRect.x + BaseOffsetX
                + currentHour * (HourBoxWidth + HourBoxGap)
                + hourProgress * HourBoxWidth;

            // Top offset to match highlight
            float cursorY = fillRect.y + BaseOffsetY + BarHeight
                + PawnAreaTopOffset;

            var babyList = Find.CurrentMap.mapPawns.SpawnedBabiesInFaction(Find.FactionManager.OfPlayer).ToList();
            int babyCount = 0;
            if (babyList != null)
                babyCount = babyList.Count;

            float totalHeight = (Find.CurrentMap.mapPawns.ColonistCount
                - babyCount)
                * (PawnRowHeight + PawnRowGap);

            // Trim from bottom
            totalHeight -= PawnAreaBottomTrim;

            Rect cursorRect = new Rect(
                cursorX,
                cursorY,
                2f, // Default width
                totalHeight
            );

            Widgets.DrawBoxSolid(cursorRect, ChronosPointerMod.Settings.bottomCursorColor);
        }

        // Method to reset the flag when the scheduler is closed
        public static void OnSchedulerClosed()
        {
            dayNightColorsCalculated = false;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using PawnTableGrouped;
using Verse;

namespace ChronosPointer
{
    /// <summary>
    /// Patch_DayNightPositionGetter:
    /// Simple patch to grab the rectangle from each PawnColumnWorker_Timetable cell,
    /// so we can track the X/Y for drawing our bar above it.
    /// </summary>
    [HarmonyPatch(typeof(PawnColumnWorker_Timetable))]
    [HarmonyPatch("DoCell")]
    public static class Patch_DayNightPositionGetter
    {
        static bool hasRect = false;

        [HarmonyPostfix]
        public static void Postfix(PawnColumnWorker_Timetable __instance, Rect rect, Pawn pawn, PawnTable table)
        {
            // The first time we see a Timetable cell, store the rect so that
            // we know where to draw the Day/Night bar in Patch_ScheduleWindow
            if (!hasRect)
            {
                hasRect = true;
                Patch_ScheduleWindow.UseMeForTheXYPosOfDayNightBar = rect;
            }
        }
    }

    /// <summary>
    /// Patch_ScheduleWindow:
    /// Main patch that intercepts MainTabWindow_Schedule.DoWindowContents, 
    /// draws the day/night bar, highlight, etc.
    /// Also references the *real* PawnTableGroupedModel from the constructor patch
    /// (so that collapsing groups actually reduces the count).
    /// </summary>
    [HarmonyPatch(typeof(MainTabWindow_Schedule))]
    [HarmonyPatch("DoWindowContents")]
    public static class Patch_ScheduleWindow
    {
        #region Values / Fields

        // This rect is captured from PawnColumnWorker_Timetable above
        public static Rect UseMeForTheXYPosOfDayNightBar;

        // Offsets & constants
        private const float BaseOffsetY = 20f;  // Move day/night bar above numbers
        private const float HourBoxWidth = 19f;  // Each hour cell matches pawn box width
        private const float HourBoxGap = 2f;
        private const float PawnRowHeight = 28f;  // user wants 28f
        private const float PawnRowGap = 2f;
        private const float BarHeight = 10f;
        private const float PawnAreaTopOffset = 16f;  // extra offset for highlight
        private const float PawnAreaBottomTrim = 2f;   // bottom trim for highlight

        // Condition definitions
        private static readonly GameConditionDef SolarFlareDef =
            DefDatabase<GameConditionDef>.GetNamed("SolarFlare");

        // We store day/night colors for 24 hours
        private static Color[] dayNightColors = new Color[24];

        // Track if dayNightColors have been calculated
        public static bool dayNightColorsCalculated = false;

        // Track the last known map so we can reset the bar if the user changes maps
        private static Map lastKnownMap = null;

        // This is the final number of pawns we show highlight lines for
        private static int pawnCount = 0;

        // Example: custom schedules mod detection
        private static readonly string[] customSchedulesModIds =
        {
            "Mysterius.CustomSchedules"
        };

        #endregion

        #region Postfix Patch

        [HarmonyPostfix]
        public static void Postfix(MainTabWindow_Schedule __instance, Rect fillRect)
        {
            if (Find.CurrentMap == null) return;

            // Position the day/night bar using the rect captured from Timetable columns
            fillRect = UseMeForTheXYPosOfDayNightBar;
            // Move up above the numbers
            fillRect.y -= 18f;

            try
            {
                // Check if any known "CustomSchedules" mods are active
                foreach (var modId in customSchedulesModIds)
                {
                    bool isModActive = ModLister.AllInstalledMods.Any(mod =>
                        mod.Active && mod.PackageId.Equals(modId, StringComparison.OrdinalIgnoreCase));

                    if (isModActive)
                    {
                        // Trigger specific fix or log if needed
                        switch (modId)
                        {
                            case "Mysterius.CustomSchedules":
                                ApplyFixForMysteriusCustomSchedules();
                                break;
                        }
                    }
                }

                // Check for solar flare/eclipse/etc. (affect bar color)
                int incident = IncidentHappening();

                // 1) Recalculate the pawn count from the real grouped model
                pawnCount = GetPawnCount(__instance);

                // If the map changed, reset dayNightColors
                if (Find.CurrentMap != lastKnownMap)
                {
                    dayNightColorsCalculated = false;
                    lastKnownMap = Find.CurrentMap;
                }

                // ============== Begin drawing the Chronos Pointer UI ==============

                // (A) Day/Night Bar
                if (ChronosPointerMod.Settings.showDayNightBar)
                {
                    if (!dayNightColorsCalculated || incident > 0)
                    {
                        CalculateDayNightColors(incident);
                    }
                    DrawDayNightBar(fillRect, dayNightColors);

                    // Aurora overlay if incident=5
                    if (incident == 5)
                    {
                        Color[] Aurora = dayNightColors;
                        Color newColor = new Color(
                            0.5f,
                            Mathf.Abs(Mathf.Sin(Time.time)),
                            0.5f,
                            Mathf.Abs(Mathf.Sin(Time.time * 0.6f)) * 0.5f
                        );
                        for (int hour = 0; hour < 24; hour++)
                        {
                            Aurora[hour] = newColor;
                        }
                        DrawDayNightBar(fillRect, Aurora);
                    }

                    // Optional small vertical line showing current time
                    if (ChronosPointerMod.Settings.showDayNightIndicator)
                    {
                        DrawDayNightTimeIndicator(fillRect);
                    }
                }

                // (B) Arrow texture
                if (ChronosPointerMod.Settings.enableArrow)
                {
                    DrawArrowTexture(fillRect);
                }

                // (C) If we actually have pawns, draw highlight bars & lines
                if (pawnCount > 0)
                {
                    // (3) Highlight bar
                    if (ChronosPointerMod.Settings.showHighlight)
                    {
                        DrawHighlight(fillRect, pawnCount);
                    }

                    // (4) Full-height cursor line
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

        #endregion

        #region Real PawnTableGroupedModel Lookup

        /// <summary>
        /// Gets the visible pawn count from the *real* PawnTableGroupedModel
        /// if PawnTableGrouped is active. Otherwise uses a fallback.
        /// </summary>
        public static int GetPawnCount(MainTabWindow_Schedule scheduleInstance)
        {
            if (scheduleInstance == null)
            {
                Log.Error("[ChronosPointer] GetPawnCount: scheduleInstance is null!");
                return 0;
            }

            // 1) Reflect the private 'table' field from the base class MainTabWindow_PawnTable
            Type baseType = scheduleInstance.GetType().BaseType; // => MainTabWindow_PawnTable
            FieldInfo tableField = baseType.GetField("table", BindingFlags.Instance | BindingFlags.NonPublic);
            if (tableField == null)
            {
                Log.Error("[ChronosPointer] Could not find 'table' field in MainTabWindow_PawnTable!");
                return 0;
            }

            // PawnTable used by the schedule
            PawnTable pawnTable = tableField.GetValue(scheduleInstance) as PawnTable;
            if (pawnTable == null)
            {
                Log.Error("[ChronosPointer] PawnTable is null from reflection?");
                return 0;
            }

            // 2) Check if PawnTableGrouped mod is active
            bool isGroupedModActive = ModLister.AllInstalledMods.Any(mod =>
                mod.Active && mod.PackageId.Equals("name.krypt.rimworld.pawntablegrouped", StringComparison.OrdinalIgnoreCase));

            if (isGroupedModActive)
            {
                // 3) See if we captured a real model for this PawnTable
                if (Patch_PawnTableGroupedModel_Constructor.ActiveGroupedModels
                    .TryGetValue(pawnTable, out PawnTableGroupedModel realModel))
                {
                    // If realModel has groups, sum only the expanded ones
                    if (realModel.Groups != null && realModel.Groups.Count > 0)
                    {
                        int expandedCount = 0;
                        foreach (var group in realModel.Groups)
                        {
                            if (group != null && realModel.IsExpanded(group))
                            {
                                expandedCount += group.Pawns?.Count ?? 0;
                            }
                        }
                        // Debug:
                        // Log.Message($"[ChronosPointer] Summed expanded groups => {expandedCount} pawns");
                        return expandedCount;
                    }
                    else
                    {
                        // If no groups or null => fallback to direct table
                        int directCount = pawnTable.PawnsListForReading.Count;
                        // Log.Message($"[ChronosPointer] realModel has no groups => fallback {directCount}");
                        return directCount;
                    }
                }
                else
                {
                    // We didn't see a constructor call for this PawnTable => fallback
                    int directCount = pawnTable.PawnsListForReading.Count;
                    // Log.Message($"[ChronosPointer] No real model => fallback {directCount}");
                    return directCount;
                }
            }
            else
            {
                // PawnTableGrouped not active => fallback
                return GetDefaultPawnCount(scheduleInstance);
            }
        }

        /// <summary>
        /// If the PawnTableGrouped mod is not active or we couldn't find a real model,
        /// we reflect the 'Pawns' property on MainTabWindow_Schedule for a direct list.
        /// </summary>
        private static int GetDefaultPawnCount(MainTabWindow_Schedule __instance)
        {
            var pawnsProp = __instance.GetType().GetProperty("Pawns", BindingFlags.Instance | BindingFlags.NonPublic);
            if (pawnsProp == null)
            {
                Log.Error("[ChronosPointer] GetDefaultPawnCount: 'Pawns' property is null!");
                return 0;
            }

            var pawnsIEnumerable = pawnsProp.GetValue(__instance) as IEnumerable<Pawn>;
            if (pawnsIEnumerable == null)
            {
                Log.Error("[ChronosPointer] pawnsIEnumerable is null in fallback!");
                return 0;
            }

            return pawnsIEnumerable.Count();
        }

        #endregion

        #region Mod-Specific Fixes & Utility

        private static void ApplyFixForMysteriusCustomSchedules()
        {
            // Insert your fix logic if needed; right now we log a warning
            Log.Error("Custom Schedules (continued) is Active. Overlap may occur.");
        }

        private static int IncidentHappening()
        {
            int incident = 0;
            bool isSolarFlare = Find.CurrentMap.gameConditionManager.ConditionIsActive(SolarFlareDef);
            bool isEclipse = Find.CurrentMap.gameConditionManager.ConditionIsActive(GameConditionDefOf.Eclipse);
            bool isToxicFallout = Find.CurrentMap.gameConditionManager.ConditionIsActive(GameConditionDefOf.ToxicFallout);
            bool isVolcanicWinter = Find.CurrentMap.gameConditionManager.ConditionIsActive(GameConditionDefOf.VolcanicWinter);
            bool isAurora = Find.CurrentMap.gameConditionManager.ConditionIsActive(GameConditionDefOf.Aurora);

            if (isSolarFlare)
                incident = 1;
            else if (isEclipse)
                incident = 2;
            else if (isToxicFallout)
                incident = 3;
            else if (isVolcanicWinter)
                incident = 4;
            else if (isAurora)
                incident = 5;

            return incident;
        }

        /// <summary>
        /// Rebuild dayNightColors array for all 24 hours, tinted by incident if needed.
        /// </summary>
        private static void CalculateDayNightColors(int incident)
        {
            for (int hour = 0; hour < 24; hour++)
            {
                switch (incident)
                {
                    case 1:
                        dayNightColors[hour] = Color.yellow; // solar flare
                        break;
                    case 2:
                        dayNightColors[hour] = new Color(0f, 0f, 0.5f); // eclipse
                        break;
                    case 3:
                        Color tinge = dayNightColors[hour];
                        dayNightColors[hour] = new Color(tinge.r, tinge.g * 1.6f, tinge.b);
                        break;
                    case 4:
                        Color d = dayNightColors[hour];
                        dayNightColors[hour] = new Color(d.r * 0.5f, d.g * 0.5f, d.b * 0.5f);
                        break;
                    default:
                        float sunlight = GenCelestial.CelestialSunGlow(Find.CurrentMap.Tile, hour * 2500);
                        dayNightColors[hour] = GetColorForSunlight(sunlight);
                        break;
                }
            }
            dayNightColorsCalculated = true;
        }

        /// <summary>
        /// Simple day/night color logic from deep night to sunrise to midday
        /// </summary>
        private static Color GetColorForSunlight(float sunlight)
        {
            // Deep night
            if (sunlight == 0f)
                return new Color(0f, 0f, 0.5f);

            // Dawn/Dusk
            if (sunlight < 0.35f)
                return new Color(0.5f, 0.5f, 1f);

            // Sunrise/Sunset
            if (sunlight < 0.7f)
                return new Color(1f, 0.5f, 0f);

            // Full daylight
            return Color.yellow;
        }

        #endregion

        #region Drawing Extra

        /// <summary>
        /// Draw the day/night bar across 24 hours
        /// </summary>
        private static void DrawDayNightBar(Rect fillRect, Color[] colors)
        {
            float baseX = fillRect.x;
            float baseY = fillRect.y + BaseOffsetY;

            for (int hour = 0; hour < 24; hour++)
            {
                float hourX = baseX + hour * (HourBoxWidth + HourBoxGap);
                Rect hourRect = new Rect(hourX, baseY, HourBoxWidth, BarHeight);
                Widgets.DrawBoxSolid(hourRect, colors[hour]);
            }
        }

        /// <summary>
        /// Small vertical indicator line on day/night bar showing current time
        /// </summary>
        private static void DrawDayNightTimeIndicator(Rect fillRect)
        {
            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f;
            int currentHour = (int)currentHourF;
            float hourProgress = currentHourF - currentHour;

            float lineX = fillRect.x + currentHour * (HourBoxWidth + HourBoxGap)
                        + hourProgress * HourBoxWidth;
            float lineY = fillRect.y + BaseOffsetY;
            float lineHeight = BarHeight;

            float sunlight = GenCelestial.CelestialSunGlow(Find.CurrentMap.Tile, (int)(currentHourF * 2500));

            Color lineColor = !ChronosPointerMod.Settings.useDynamicTimeTraceLine
                ? ChronosPointerMod.Settings.timeTraceColorDay
                : (sunlight >= 0.7f
                    ? ChronosPointerMod.Settings.timeTraceColorDay
                    : ChronosPointerMod.Settings.timeTraceColorNight);

            // 2 px wide
            Rect traceRect = new Rect(lineX, lineY, 2f, lineHeight);
            Widgets.DrawBoxSolid(traceRect, lineColor);
        }

        /// <summary>
        /// Arrow texture pointing downward at current time
        /// </summary>
        private static void DrawArrowTexture(Rect fillRect)
        {
            if (ChronosPointerTextures.ArrowTexture == null) return;

            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f;
            int currentHour = (int)currentHourF;
            float hourProgress = currentHourF - currentHour;

            float arrowCenterX = fillRect.x + currentHour * (HourBoxWidth + HourBoxGap)
                                + hourProgress * HourBoxWidth + 1f;

            float barTopY = fillRect.y + BaseOffsetY + 4;
            float arrowWidth = 8f;
            float arrowHeight = 8f;

            float arrowRectX = arrowCenterX - (arrowWidth / 2f);
            float arrowRectY = barTopY - arrowHeight
                - (!ChronosPointerMod.Settings.showDayNightBar ? -2f : 4f);

            Rect arrowRect = new Rect(arrowRectX, arrowRectY, arrowWidth, arrowHeight);

            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;

            // Rotate 90 degrees around center, so arrow points downward
            GUIUtility.RotateAroundPivot(90f, arrowRect.center);

            GUI.color = ChronosPointerMod.Settings.arrowColor;
            GUI.DrawTexture(
                arrowRect.ScaledBy(
                    (!ChronosPointerMod.Settings.showDayNightBar ? 2 : 1)
                ),
                ChronosPointerTextures.ArrowTexture
            );

            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        /// <summary>
        /// Draw a highlight rectangle for the current hour across all pawns
        /// </summary>
        private static void DrawHighlight(Rect fillRect, int pawnCount)
        {
            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f;
            int currentHour = (int)currentHourF;

            float colX = fillRect.x + currentHour * (HourBoxWidth + HourBoxGap);
            float colY = fillRect.y + BaseOffsetY + BarHeight + PawnAreaTopOffset;

            float totalHeight = pawnCount * (PawnRowHeight + PawnRowGap);
            totalHeight -= PawnAreaBottomTrim; // trim from bottom

            Rect highlightRect = new Rect(colX, colY, HourBoxWidth, totalHeight);

            if (ChronosPointerMod.Settings.hollowHourHighlight)
            {
                Widgets.DrawBoxSolidWithOutline(
                    highlightRect,
                    new Color(0, 0, 0, 0),
                    ChronosPointerMod.Settings.highlightColor,
                    2
                );
            }
            else
            {
                Widgets.DrawBoxSolid(highlightRect, ChronosPointerMod.Settings.highlightColor);
            }
        }

        /// <summary>
        /// Draw a full-height vertical line at current hour from top to bottom
        /// </summary>
        private static void DrawFullHeightCursor(Rect fillRect, int pawnCount)
        {
            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f;
            int currentHour = (int)currentHourF;
            float hourProgress = currentHourF - currentHour;

            float cursorX = fillRect.x + currentHour * (HourBoxWidth + HourBoxGap)
                + hourProgress * HourBoxWidth + 1f; // shift 1 px

            float cursorThickness = ChronosPointerMod.Settings.cursorThickness;
            // ensure even thickness
            if (cursorThickness % 2 != 0)
                cursorThickness += 1f;

            // center it
            cursorX -= cursorThickness / 2f;

            float cursorY = fillRect.y + BaseOffsetY + BarHeight + PawnAreaTopOffset;

            float totalHeight = pawnCount * (PawnRowHeight + PawnRowGap);
            totalHeight -= PawnAreaBottomTrim;

            Rect cursorRect = new Rect(cursorX, cursorY, cursorThickness, totalHeight);
            Widgets.DrawBoxSolid(cursorRect, ChronosPointerMod.Settings.bottomCursorColor);
        }

        #endregion
    }
}

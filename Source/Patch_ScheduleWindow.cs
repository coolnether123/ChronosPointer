using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using PawnTableGrouped;

namespace ChronosPointer
{
    /// <summary>
    /// This patch captures real PawnTableGroupedModel instances whenever
    /// (PawnTable, PawnTableDef) constructor is called. 
    /// We'll store them in a dictionary so we can see expansions/collapses.
    /// </summary>
    [HarmonyPatch(typeof(PawnTableGroupedModel))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new Type[] { typeof(PawnTable), typeof(PawnTableDef) })]
    public static class Patch_PawnTableGroupedModel_Constructor
    {
        /// <summary>
        /// Dictionary: PawnTable => actual PawnTableGroupedModel instance
        /// so we know the real expanded/collapsed groups the mod is using.
        /// </summary>
        public static Dictionary<PawnTable, PawnTableGroupedModel> ActiveGroupedModels
            = new Dictionary<PawnTable, PawnTableGroupedModel>();

        // Postfix captures the constructor parameters by index:
        // __0 => PawnTable table
        // __1 => PawnTableDef def
        // __instance => the newly created PawnTableGroupedModel
        public static void Postfix(PawnTable __0, PawnTableDef __1, PawnTableGroupedModel __instance)
        {
            if (__0 != null && __instance != null)
            {
                ActiveGroupedModels[__0] = __instance;
                // Now we won't create a new model ourselves; we'll use this real one.
            }
        }
    }

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
            if (!hasRect)
            {
                hasRect = true;
                Patch_ScheduleWindow.UseMeForTheXYPosOfDayNightBar = rect;
            }
        }
    }

    /// <summary>
    /// Main patch for MainTabWindow_Schedule.DoWindowContents
    /// (Your original code + the grouped fix in GetPawnCount).
    /// </summary>
    [HarmonyPatch(typeof(MainTabWindow_Schedule))]
    [HarmonyPatch("DoWindowContents")]
    public static class Patch_ScheduleWindow
    {
        #region Values
        // Additional offsets for grouped pawns mod
        private static float GroupedModOffsetX = 0f;
        private static float GroupedModOffsetY = 0f;
        public static Rect UseMeForTheXYPosOfDayNightBar;

        // Where the schedule grid starts
        private static float BaseOffsetX = 1f;
        private const float BaseOffsetY = 40;

        // Each hour cell
        private const float HourBoxWidth = 19f;
        private const float HourBoxGap = 2f;

        // Pawn row
        private const float PawnRowHeight = 28f; // user wants 28f
        private const float PawnRowGap = 2f;

        // Day/night bar
        private const float BarHeight = 10f;

        // Extra offsets for highlight & line so they don’t slip off top/bottom
        private const float PawnAreaTopOffset = 16f;
        private const float PawnAreaBottomTrim = 2f;

        // SolarFlare condition if not available
        private static readonly GameConditionDef SolarFlareDef = DefDatabase<GameConditionDef>.GetNamed("SolarFlare");

        // Track day/night color calc
        public static bool dayNightColorsCalculated = false;

        // Track if the pawn count has been calculated
        public static bool pawnCountCalculated = false;

        // Colors for each hour
        private static Color[] dayNightColors = new Color[24];

        // Store the last known map
        private static Map lastKnownMap = null;

        // Cached number of pawns for highlight bars
        private static int pawnCount = 0;

        // Custom Schedules mod checks
        private static readonly string[] customSchedulesModIds = new string[]
        {
            "Mysterius.CustomSchedules"
        };
        #endregion

        [HarmonyPostfix]
        public static void Postfix(MainTabWindow_Schedule __instance, Rect fillRect)
        {
            if (Find.CurrentMap == null) return;

            // Check if the grouped pawns mod is active
            bool isGroupedModActive = ModLister.AllInstalledMods.Any(mod =>
                mod.Active && mod.PackageId.Equals("name.krypt.rimworld.pawntablegrouped", StringComparison.OrdinalIgnoreCase));

            // Set offsets based on mod status
            if (isGroupedModActive)
            {
                GroupedModOffsetX = 173f; // Example offset value for X (positive is right)
                GroupedModOffsetY = -30f; // Example offset value for Y (positive is down)
            }
            else
            {
                GroupedModOffsetX = 0f;
                GroupedModOffsetY = 0f;
            }

            // Use the rect from the timetable cell
            fillRect = UseMeForTheXYPosOfDayNightBar;

            try
            {
                // (1) Check for certain custom schedules mods
                foreach (var modId in customSchedulesModIds)
                {
                    bool isModActive = ModLister.AllInstalledMods.Any(mod =>
                        mod.Active && mod.PackageId.Equals(modId, StringComparison.OrdinalIgnoreCase));

                    if (isModActive)
                    {
                        switch (modId)
                        {
                            case "Mysterius.CustomSchedules":
                                ApplyFixForMysteriusCustomSchedules();
                                break;
                        }
                    }
                }

                // (2) Incidents (SolarFlare, Eclipse, etc.)
                int incident = IncidentHappening();

                // (3) Calculate how many pawns to draw for the highlight
                pawnCount = GetPawnCount(__instance);

                // (4) If map changed, reset dayNightColors
                if (Find.CurrentMap != lastKnownMap)
                {
                    dayNightColorsCalculated = false;
                    pawnCountCalculated = false;
                    lastKnownMap = Find.CurrentMap;
                }

                // 1) Day/Night Bar
                if (ChronosPointerMod.Settings.showDayNightBar)
                {
                    // Adjust fillRect with the offsets
                    fillRect.x += GroupedModOffsetX;
                    fillRect.y += GroupedModOffsetY;

                    if (!dayNightColorsCalculated || incident > 0)
                    {
                        CalculateDayNightColors(incident);
                    }
                    DrawDayNightBar(fillRect, dayNightColors);

                    if (incident == 5) // Aurora
                    {
                        Color[] Aurora = dayNightColors;
                        Color newColor = new Color(0.5f,
                            Mathf.Abs(Mathf.Sin(Time.time)),
                            0.5f,
                            Mathf.Abs(Mathf.Sin(Time.time * 0.6f)) * 0.5f);
                        for (int hour = 0; hour < 24; hour++)
                        {
                            Aurora[hour] = newColor;
                        }
                        DrawDayNightBar(fillRect, Aurora);
                    }

                    if (ChronosPointerMod.Settings.showDayNightIndicator)
                    {
                        DrawDayNightTimeIndicator(fillRect);
                    }
                }

                // 2) Arrow
                if (ChronosPointerMod.Settings.enableArrow)
                {
                    DrawArrowTexture(fillRect);
                }

                // 3) Pawn highlight bars only if we have pawns
                if (pawnCount > 0)
                {
                    // 3a) Highlight bar
                    if (ChronosPointerMod.Settings.showHighlight)
                    {
                        DrawHighlight(fillRect, pawnCount);
                    }

                    // 3b) Full-height line
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
        private static void ApplyFixForMysteriusCustomSchedules()
        {
            Log.Error("Custom Schedules (continued) is Active. Chronos Pointer will have overlap issues.");
        }
        #endregion

        #region Day/Night Bar
        private static void CalculateDayNightColors(int incident)
        {
            for (int hour = 0; hour < 24; hour++)
            {
                switch (incident)
                {
                    case 1: // Solar Flare
                        dayNightColors[hour] = Color.yellow;
                        break;
                    case 2: // Eclipse
                        dayNightColors[hour] = new Color(0f, 0f, 0.5f);
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
                        dayNightColors[hour] = GetColorForSunlight(sunlight);
                        break;
                }
            }
            dayNightColorsCalculated = true;
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
            {
                incident = 1;
            }
            else if (isEclipse)
            {
                incident = 2;
            }
            else if (isToxicFallout)
            {
                incident = 3;
            }
            else if (isVolcanicWinter)
            {
                incident = 4;
            }
            else if (isAurora)
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
            if (sunlight == 0f)
                return new Color(0f, 0f, 0.5f);  // Deep Blue
            if (sunlight < 0.35f)
                return new Color(0.5f, 0.5f, 1f); // Light Blue
            if (sunlight < 0.7f)
                return new Color(1f, 0.5f, 0f);   // Orange
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
            totalHeight -= PawnAreaBottomTrim; // Trim from bottom

            Rect highlightRect = new Rect(colX, colY, HourBoxWidth, totalHeight);
            if (ChronosPointerMod.Settings.hollowHourHighlight)
                Widgets.DrawBoxSolidWithOutline(
                    highlightRect,
                    new Color(0, 0, 0, 0),
                    ChronosPointerMod.Settings.highlightColor, 2);
            else
                Widgets.DrawBoxSolid(highlightRect, ChronosPointerMod.Settings.highlightColor);
        }
        #endregion

        #region Time Trace Line

        /// <summary>
        /// Modified GetPawnCount to handle grouped pawns if PawnTableGrouped is active.
        /// Otherwise, uses original reflection approach.
        /// </summary>
        static int GetPawnCount(MainTabWindow_Schedule __instance)
        {
            if (__instance == null)
            {
                Log.Error("Instance is null!");
                return 0;
            }

            // 1) Reflect the private 'Pawns' property for fallback
            var pawnsProp = __instance.GetType().GetProperty("Pawns", BindingFlags.Instance | BindingFlags.NonPublic);
            if (pawnsProp == null)
            {
                Log.Error("Field is null!");
                return 0;
            }
            var pawnsIEnumerable = pawnsProp.GetValue(__instance) as IEnumerable<Pawn>;
            if (pawnsIEnumerable == null)
            {
                Log.Error("pawnsIEnum is null!");
                return 0;
            }
            int defaultCount = pawnsIEnumerable.Count();

            // 2) Check if PawnTableGrouped mod is active
            bool isGroupedModActive = ModLister.AllInstalledMods.Any(mod =>
                mod.Active && mod.PackageId.Equals("name.krypt.rimworld.pawntablegrouped", StringComparison.OrdinalIgnoreCase));
            if (!isGroupedModActive)
            {
                // Just do the old approach
                pawnCountCalculated = true;
                return defaultCount;
            }

            try
            {
                // 3) Reflect the private PawnTable field from base class
                Type baseType = __instance.GetType().BaseType; // MainTabWindow_PawnTable
                FieldInfo tableField = baseType.GetField("table", BindingFlags.Instance | BindingFlags.NonPublic);
                if (tableField == null)
                {
                    Log.Error("Could not find 'table' field via reflection in MainTabWindow_PawnTable!");
                    return defaultCount;
                }

                PawnTable pawnTable = tableField.GetValue(__instance) as PawnTable;
                if (pawnTable == null)
                {
                    Log.Error("PawnTable is null!");
                    return defaultCount;
                }

                // 4) See if we have a *real* PawnTableGroupedModel for that table
                if (Patch_PawnTableGroupedModel_Constructor.ActiveGroupedModels
                        .TryGetValue(pawnTable, out PawnTableGroupedModel realModel))
                {
                    // sum up expanded groups
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
                        // Add the number of groups to the expanded count
                        expandedCount += realModel.Groups.Count;

                        // if expandedCount is 0, maybe everything is collapsed
                        // which is correct => 0 pawns showing
                        pawnCountCalculated = true;
                        return expandedCount;
                    }
                    else
                    {
                        // real model has no groups => fallback
                        pawnCountCalculated = true;
                        return defaultCount;
                    }
                }
                else
                {
                    // We don't have a stored model => fallback
                    pawnCountCalculated = true;
                    return defaultCount;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error in GetPawnCount for grouped model => {e}");
                pawnCountCalculated = true;
                return defaultCount;
            }
        }

        /// <summary>
        /// A small vertical line in the day/night bar (2px wide).
        /// </summary>
        private static void DrawDayNightTimeIndicator(Rect fillRect)
        {
            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f;
            int currentHour = (int)currentHourF;
            float hourProgress = currentHourF - currentHour;

            float lineX = fillRect.x + BaseOffsetX
                        + currentHour * (HourBoxWidth + HourBoxGap)
                        + hourProgress * HourBoxWidth;

            float lineY = fillRect.y + BaseOffsetY;
            float lineHeight = BarHeight;

            float sunlight = GenCelestial.CelestialSunGlow(Find.CurrentMap.Tile, (int)(currentHourF * 2500));

            Color lineColor = !ChronosPointerMod.Settings.useDynamicTimeTraceLine
                ? ChronosPointerMod.Settings.timeTraceColorDay
                : (sunlight >= 0.7f
                    ? ChronosPointerMod.Settings.timeTraceColorDay
                    : ChronosPointerMod.Settings.timeTraceColorNight);

            Rect traceRect = new Rect(lineX, lineY, 2f, lineHeight);
            Widgets.DrawBoxSolid(traceRect, lineColor);
        }

        private static void DrawArrowTexture(Rect fillRect)
        {
            if (ChronosPointerTextures.ArrowTexture == null) return;

            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f;
            int currentHour = (int)currentHourF;
            float hourProgress = currentHourF - currentHour;

            float arrowCenterX = fillRect.x + BaseOffsetX
                                 + currentHour * (HourBoxWidth + HourBoxGap)
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

            GUIUtility.RotateAroundPivot(90f, arrowRect.center);

            GUI.color = ChronosPointerMod.Settings.arrowColor;
            GUI.DrawTexture(arrowRect.ScaledBy((!ChronosPointerMod.Settings.showDayNightBar ? 2 : 1)),
                ChronosPointerTextures.ArrowTexture);

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

            float cursorX = fillRect.x + BaseOffsetX
                + currentHour * (HourBoxWidth + HourBoxGap)
                + hourProgress * HourBoxWidth
                + 1f; // Shift 1px

            float cursorThickness = ChronosPointerMod.Settings.cursorThickness;
            if (cursorThickness % 2 != 0)
            {
                cursorThickness += 1f;
            }
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

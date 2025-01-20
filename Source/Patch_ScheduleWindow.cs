using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using PawnTableGrouped;

namespace ChronosPointer
{
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

    [HarmonyPatch(typeof(MainTabWindow_Schedule))]
    [HarmonyPatch("DoWindowContents")]
    public static class Patch_ScheduleWindow
    {
        #region Values
        public static Rect UseMeForTheXYPosOfDayNightBar;

        // Where the schedule grid starts
        public static float BaseOffsetX = 1f;
        public static float BaseOffsetY = 40;

        // Each hour cell
        public static float HourBoxWidth = 19f;
        public static float HourBoxGap = 2f;

        // Pawn row
        public static float PawnRowHeight = 28f;
        public static float PawnRowGap = 2f;

        // Day/night bar
        public static float BarHeight = 10f;

        // Extra offsets for highlight & line so they don’t slip off top/bottom
        public static float PawnAreaTopOffset = 16f;
        public static float PawnAreaBottomTrim = 2f;

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

        // Custom Schedules (continued) mod ID
        private static readonly string[] customSchedulesModIds = new string[]
        {
            "Mysterius.CustomSchedules",
            "name.krypt.rimworld.pawntablegrouped"
        };
        #endregion

        [HarmonyPostfix]
        public static void Postfix(MainTabWindow_Schedule __instance, Rect fillRect)
        {
            // Schedule the patch to be applied later, ensuring proper initialization
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                try
                {
                    ApplyPatches(__instance, fillRect);
                }
                catch (Exception e)
                {
                    Log.Error($"ChronosPointer: Error in scheduled patch - {e.Message}\n{e.StackTrace}");
                }
            });
        }

        public static void ApplyPatches(MainTabWindow_Schedule __instance, Rect fillRect)
        {
            if (Find.CurrentMap == null) return;

            fillRect = UseMeForTheXYPosOfDayNightBar;

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
                    }
                }
            }
            int incident = IncidentHappening();

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

            // Apply Grouped Pawns Lists patch
            ChronosPointerPatches.ApplyPatches(fillRect);
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
                        //Log.Message("[Chronos Pointer] At hour " + hour + " sunlight == " + sunlight);
                        dayNightColors[hour] = GetColorForSunlight(sunlight);
                        break;
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
            if (__instance == null)
            {
                Log.Error("Instance is null!");
                return 0;
            }

            // Check if the "Grouped Pawns Lists" mod is enabled for the Schedule table
            if (ChronosPointerPatches.IsModEnabled("name.krypt.rimworld.pawntablegrouped"))
            {
                // Use reflection to get the private field "table" from the instance
                var tableField = typeof(MainTabWindow_PawnTable).GetField("table", BindingFlags.Instance | BindingFlags.NonPublic);

                if (tableField == null)
                {
                    Log.Error("Failed to get PawnTable field from MainTabWindow_Schedule.");
                    return 0;
                }

                // Get the PawnTable instance from the field
                var pawnTable = tableField.GetValue(__instance) as PawnTable;

                // Ensure pawnTable is not null
                if (pawnTable == null)
                {
                    Log.Error("Failed to get PawnTable instance from field.");
                    return 0;
                }

                // Use reflection to get the "Implementation" property from the PawnTable
                var groupedTable = (PawnTableGrouped.PawnTableGroupedImpl)pawnTable.GetType().GetProperty("Implementation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(pawnTable);

                if (groupedTable != null)
                {
                    Log.Message($"Grouped Table is found");
                    // Use reflection to get the "model" field from the groupedTable
                    var model = (PawnTableGrouped.PawnTableGroupedModel)ChronosPointerPatches.m_PawnTableGroupedImpl_model.GetValue(groupedTable);

                    // Use reflection to invoke the "get_Groups" method to get the list of groups
                    var groups = (List<PawnTableGrouped.PawnTableGroup>)ChronosPointerPatches.m_PawnTableGroupModel_get_Groups.Invoke(model, new object[] { });

                    if (groups != null)
                    {
                        Log.Message($"Found Groups");
                        int totalCount = 0;
                        foreach (var group in groups)
                        {
                            Log.Message($"Checking group");
                            // Use reflection to invoke the "get_IsExpanded" method to check if the group is expanded
                            bool isExpanded = (bool)ChronosPointerPatches.m_PawnTableGroupModel_get_IsExpanded.Invoke(model, new object[] { group });
                            if (isExpanded)
                            {
                                // Add the count of pawns in the group if it is expanded
                                totalCount += group.Pawns.Count;
                            }
                        }
                        return totalCount;
                    }
                    else
                    {
                        Log.Error($"Found NO Groups!!");
                    }
                }
                else
                {
                    Log.Error($"Found NO groupedTable!!");
                }
            }

            // Default behavior if "Grouped Pawns Lists" is not active or "Schedule" is not enabled in its settings
            // Use reflection to get the "Pawns" property from the instance
            var pawnsField = __instance.GetType().GetProperty("Pawns", BindingFlags.Instance | BindingFlags.NonPublic);
            if (pawnsField == null)
            {
                Log.Error("Failed to get Pawns property from MainTabWindow_Schedule.");
                return 0;
            }

            // Get the IEnumerable<Pawn> from the Pawns property
            var pawnsIEnumerable = pawnsField.GetValue(__instance) as IEnumerable<Pawn>;
            if (pawnsIEnumerable == null)
            {
                Log.Error("Failed to get Pawns as IEnumerable<Pawn>.");
                return 0;
            }

            return pawnsIEnumerable.Count();
        }

        /// <summary>
        /// A small vertical line in the day/night bar, 2px wide for visibility.
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
            float barTopY = fillRect.y + BaseOffsetY;

            // Will rotate the arrow later
            float arrowWidth = 8f; // Default width
            float arrowHeight = 8f; // Default height

            // Center the arrow horizontally on the line, 
            // so arrowRect.center.x = arrowCenterX
            float arrowRectX = arrowCenterX - (arrowWidth / 2f);

            // Changes the arrow up or down. up is -
            float arrowRectY = barTopY - arrowHeight - (!ChronosPointerMod.Settings.showDayNightBar ? -2f : 4f) - 28f;

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
namespace ChronosPointer
{
    public static class ChronosPointerPatches
    {
        // These fields and methods are now public static
        public static FieldInfo m_PawnTableGroupedImpl_model = null;
        public static MethodInfo m_PawnTableGroupModel_get_Groups = null;
        public static MethodInfo m_PawnTableGroupModel_get_IsExpanded = null;
        private static readonly BindingFlags AllFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static bool IsModEnabled(string packageId)
        {
            return ModLister.AllInstalledMods.Any(mod =>
                mod.Active && mod.PackageId.Equals(packageId, StringComparison.OrdinalIgnoreCase));
        }

        public static void ApplyPatches(Rect fillRect)
        {
            // Additional spacing for Grouped Pawns Lists mod
            if (PawnTableGrouped.Mod.Settings.pawnTablesEnabled.Contains("Schedule"))
            {
                // Cache reflection info only if it has not been cached yet
                if (m_PawnTableGroupedImpl_model == null)
                {
                    m_PawnTableGroupedImpl_model = AccessTools.Field(typeof(PawnTableGrouped.PawnTableGroupedImpl), "model");
                }
                if (m_PawnTableGroupModel_get_Groups == null)
                {
                    m_PawnTableGroupModel_get_Groups = AccessTools.PropertyGetter(typeof(PawnTableGrouped.PawnTableGroupedModel), "Groups");
                }
                if (m_PawnTableGroupModel_get_IsExpanded == null)
                {
                    m_PawnTableGroupModel_get_IsExpanded = AccessTools.Method(typeof(PawnTableGrouped.PawnTableGroupedModel), "IsExpanded");
                }

                // Get the active table without using MainButtonDefOf.Schedule
                MainTabWindow_Schedule mainTabWindow = Find.WindowStack.WindowOfType<MainTabWindow_Schedule>();
                if (mainTabWindow == null)
                {
                    Log.Error("ChronosPointer: Schedule tab is not open.");
                    return; // Schedule tab is not open
                }

                // Get the PawnTable instance
                var pawnTableField = typeof(MainTabWindow_PawnTable).GetField("table", BindingFlags.Instance | BindingFlags.NonPublic);
                if (pawnTableField == null)
                {
                    Log.Error("ChronosPointer: Could not find 'table' field in MainTabWindow_PawnTable.");
                    return;
                }

                var pawnTable = pawnTableField.GetValue(mainTabWindow) as PawnTable;
                if (pawnTable == null)
                {
                    Log.Error("ChronosPointer: Could not find PawnTable in MainTabWindow_Schedule.");
                    return;
                }

                var groupedTable = pawnTable.GetType().GetProperty("Implementation", AllFlags)?.GetValue(pawnTable) as PawnTableGrouped.PawnTableGroupedImpl;
                if (groupedTable == null)
                {
                    Log.Error("ChronosPointer: Could not find GroupedTable implementation.");
                    return;
                }

                // Get groups and expanded groups
                var model = m_PawnTableGroupedImpl_model.GetValue(groupedTable) as PawnTableGrouped.PawnTableGroupedModel;
                if (model == null)
                {
                    Log.Error("ChronosPointer: Could not find PawnTableGroupedModel.");
                    return;
                }

                var groups = m_PawnTableGroupModel_get_Groups.Invoke(model, new object[] { }) as List<PawnTableGrouped.PawnTableGroup>;
                if (groups != null)
                {
                    ApplyGroupedPawnListsSpacing(fillRect, groups, model);
                }
            }
        }

        // Updated mod checker
        private static bool IsModInstalled(string modName)
        {
            return LoadedModManager.RunningMods.Any(mod => mod.Name.Equals(modName, StringComparison.OrdinalIgnoreCase));
        }

        private static void ApplyGroupedPawnListsSpacing(Rect fillRect, List<PawnTableGrouped.PawnTableGroup> groups, PawnTableGrouped.PawnTableGroupedModel model)
        {
            // Implement the logic for applying spacing to grouped pawn lists
            // This is a placeholder implementation and should be replaced with actual logic
            foreach (var group in groups)
            {
                // Example logic: Log the group count or another available property
                Log.Message($"Applying spacing for group with {group.Pawns.Count()} pawns.");
            }
        }
    }
}
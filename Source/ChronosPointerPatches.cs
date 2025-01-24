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
    public static class ChronosPointerPatches
    {
        // These fields and methods are now public static
        public static FieldInfo m_PawnTableGroupedImpl_model = null;
        public static MethodInfo m_PawnTableGroupModel_get_Groups = null;
        public static MethodInfo m_PawnTableGroupModel_get_IsExpanded = null;
        // Flag to track if reflection has been performed
        private static bool reflectionCached = false;


        public static bool IsModEnabled(string packageId)
        {
            return ModLister.AllInstalledMods.Any(mod =>
                mod.Active && mod.PackageId.Equals(packageId, StringComparison.OrdinalIgnoreCase));
        }

        public static void ApplyPatches(Rect fillRect)
        {
            Log.Message("Applying patches for Grouped Pawns Lists mod.");

            if (!IsModEnabled("name.krypt.rimworld.pawntablegrouped"))
            {
                Log.Message("Grouped Pawns Lists mod is not enabled.");
                return;
            }


            Log.Message("Grouped Pawns Lists mod is enabled.");

            // Cache reflection info only once
            if (!reflectionCached)
            {
                m_PawnTableGroupedImpl_model = AccessTools.Field(typeof(PawnTableGroupedImpl), "model");
                Log.Message("Cached m_PawnTableGroupedImpl_model field.");
                m_PawnTableGroupModel_get_Groups = AccessTools.PropertyGetter(typeof(PawnTableGroupedModel), "Groups");
                Log.Message("Cached m_PawnTableGroupModel_get_Groups method.");

                m_PawnTableGroupModel_get_IsExpanded = AccessTools.Method(typeof(PawnTableGroupedModel), "IsExpanded");
                Log.Message("Cached m_PawnTableGroupModel_get_IsExpanded method.");

                reflectionCached = true;
            }


            // Get the active table without using MainButtonDefOf.Schedule
            MainTabWindow_Schedule mainTabWindow = Find.WindowStack.WindowOfType<MainTabWindow_Schedule>();
            if (mainTabWindow == null)
            {
                Log.Error("ChronosPointer: Schedule tab is not open.");
                return; // Schedule tab is not open
            }
            Log.Message("Schedule tab is open.");

            // Get the PawnTable instance
            var pawnTableField = AccessTools.Field(typeof(MainTabWindow_PawnTable), "table");
            if (pawnTableField == null)
            {
                Log.Error("ChronosPointer: Could not find 'table' field in MainTabWindow_PawnTable.");
                return;
            }
            Log.Message("Found 'table' field in MainTabWindow_PawnTable.");

            var pawnTable = pawnTableField.GetValue(mainTabWindow) as PawnTable;
            if (pawnTable == null)
            {
                Log.Error("ChronosPointer: Could not find PawnTable in MainTabWindow_Schedule.");
                return;
            }
            Log.Message("Found PawnTable in MainTabWindow_Schedule.");


            var implementationProperty = AccessTools.Property(typeof(PawnTable), "Implementation");
            if (implementationProperty == null)
            {
                Log.Error("ChronosPointer: Could not find 'Implementation' property in PawnTable.");
                return;
            }

            var groupedTable = implementationProperty.GetValue(pawnTable) as PawnTableGroupedImpl;
            if (groupedTable == null)
            {
                //If grouped pawns lists is not enabled for this pawn table, just return.
                return;
            }
            Log.Message("Found GroupedTable implementation.");


            // Get groups and expanded groups
            var model = m_PawnTableGroupedImpl_model?.GetValue(groupedTable) as PawnTableGroupedModel;
            if (model == null)
            {
                Log.Error("ChronosPointer: Could not find PawnTableGroupedModel.");
                return;
            }
            Log.Message("Found PawnTableGroupedModel.");


            var groups = m_PawnTableGroupModel_get_Groups?.Invoke(model, null) as List<PawnTableGroup>;
            if (groups != null)
            {
                Log.Message("Found groups in PawnTableGroupedModel.");
                ApplyGroupedPawnListsSpacing(fillRect, groups, model);
            }
            else
            {
                Log.Error("ChronosPointer: No groups found in PawnTableGroupedModel.");
            }

        }

        private static void ApplyGroupedPawnListsSpacing(Rect fillRect, List<PawnTableGroup> groups, PawnTableGroupedModel model)
        {
            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f;
            int currentHour = (int)currentHourF;
            float hourProgress = currentHourF - currentHour;

            // Calculate cursorX consistently with other elements and shift 1px to the right
            float cursorX = Patch_ScheduleWindow.UseMeForTheXYPosOfDayNightBar.x + Patch_ScheduleWindow.BaseOffsetX
                + currentHour * (Patch_ScheduleWindow.HourBoxWidth + Patch_ScheduleWindow.HourBoxGap)
                + hourProgress * Patch_ScheduleWindow.HourBoxWidth
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
            float cursorY = fillRect.y + Patch_ScheduleWindow.BaseOffsetY + Patch_ScheduleWindow.BarHeight + Patch_ScheduleWindow.PawnAreaTopOffset - 42f;


            var totalHeight = 0;

            foreach (var group in groups)
            {
                bool isExpanded = (bool)m_PawnTableGroupModel_get_IsExpanded.Invoke(model, new object[] { group });


                if (isExpanded)
                {
                   // totalHeight += group.Pawns.Count() * (Patch_ScheduleWindow.PawnRowHeight + Patch_ScheduleWindow.PawnRowGap);
                }
                else
                {
                   // totalHeight += Patch_ScheduleWindow.PawnRowHeight + Patch_ScheduleWindow.PawnRowGap;
                }
            }
            Rect cursorRect = new Rect(
                      cursorX,
                      cursorY,
                      cursorThickness,
                       totalHeight
                  );
            Widgets.DrawBoxSolid(cursorRect, ChronosPointerMod.Settings.bottomCursorColor);
        }
    }
}
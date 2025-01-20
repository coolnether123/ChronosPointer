using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using PawnTableGrouped;
using System.Linq;

namespace ChronosPointer
{
    public static class GroupedPawnsListsPatch
    {
        // These fields and methods are now public static
        public static FieldInfo m_PawnTableGroupedImpl_model = null;
        public static MethodInfo m_PawnTableGroupModel_get_Groups = null;
        public static MethodInfo m_PawnTableGroupModel_get_IsExpanded = null;
        private static readonly BindingFlags AllFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

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
                    return; // Schedule tab is not open
                }

                // Get the PawnTable instance
                var pawnTableField = typeof(MainTabWindow_PawnTable).GetField("table", BindingFlags.Instance | BindingFlags.NonPublic);
                var pawnTable = pawnTableField?.GetValue(mainTabWindow) as PawnTable;

                if (pawnTable == null)
                {
                    Log.Error($"ChronosPointer: Could not find PawnTable in MainTabWindow_Schedule.");
                    return;
                }

                var groupedTable = (PawnTableGrouped.PawnTableGroupedImpl)pawnTable.GetType().GetProperty("Implementation", AllFlags).GetValue(pawnTable);

                if (groupedTable != null)
                {
                    // Get groups and expanded groups
                    var model = (PawnTableGrouped.PawnTableGroupedModel)m_PawnTableGroupedImpl_model.GetValue(groupedTable);
                    var groups = (List<PawnTableGrouped.PawnTableGroup>)m_PawnTableGroupModel_get_Groups.Invoke(model, new object[] { });

                    if (groups != null)
                    {
                        ApplyGroupedPawnListsSpacing(fillRect, groups, model);
                    }
                }
            }
        }

        private static void ApplyGroupedPawnListsSpacing(Rect fillRect, List<PawnTableGrouped.PawnTableGroup> groups, PawnTableGrouped.PawnTableGroupedModel model)
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

            float totalHeight = 0;
            int groupIndex = 0;

            foreach (var group in groups)
            {
                // Use IsExpanded method via reflection
                bool isExpanded = (bool)m_PawnTableGroupModel_get_IsExpanded.Invoke(model, new object[] { group });

                if (isExpanded)
                {
                    float groupHeight = group.Pawns.Count() * (Patch_ScheduleWindow.PawnRowHeight + Patch_ScheduleWindow.PawnRowGap);

                    Rect cursorRect = new Rect(
                        cursorX,
                        cursorY + totalHeight,
                        cursorThickness, // Use the adjusted even thickness
                        groupHeight
                    );

                    Widgets.DrawBoxSolid(cursorRect, ChronosPointerMod.Settings.bottomCursorColor);

                    totalHeight += groupHeight;
                }
                else
                {
                    // Draw a shorter cursor for collapsed groups
                    float collapsedGroupHeight = Patch_ScheduleWindow.PawnRowHeight + Patch_ScheduleWindow.PawnRowGap; // Height for a single row

                    Rect cursorRect = new Rect(
                        cursorX,
                        cursorY + totalHeight,
                        cursorThickness,
                        collapsedGroupHeight
                    );

                    Widgets.DrawBoxSolid(cursorRect, ChronosPointerMod.Settings.bottomCursorColor);

                    totalHeight += collapsedGroupHeight;
                }

                groupIndex++;
            }
        }
    }
}
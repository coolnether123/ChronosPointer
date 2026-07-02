using ChronosPointer.Api;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ChronosPointer.RimWorld
{
    internal static class ChronosScheduleGeometryFactory
    {
        public const float DefaultBaseOffsetX = ChronosScheduleGeometryDefaults.BaseOffsetX;
        public const float BaseOffsetY = ChronosScheduleGeometryDefaults.BaseOffsetY;
        public const float HourBoxGap = ChronosScheduleGeometryDefaults.HourBoxGap;
        public const float HourBoxHeight = ChronosScheduleGeometryDefaults.HourBoxHeight;
        public const float PawnAreaTopOffset = ChronosScheduleGeometryDefaults.PawnAreaTopOffset;

        private const float PawnAreaBottomTrim = 2f;
        private const float DefaultHourBoxWidth = ChronosScheduleGeometryDefaults.HourBoxWidth;

        public static bool TryCreate(MainTabWindow_PawnTable scheduleWindow, Rect fillRect, out ChronosScheduleGeometrySnapshot geometry)
        {
            geometry = null;
            PawnTable table = GetPawnTable(scheduleWindow);
            if (scheduleWindow == null || table == null)
            {
                return false;
            }

#if !V1_4U
            var columns = table.ColumnsListForReading;
#else
            var columns = table.Columns;
#endif
            if (columns == null)
            {
                return false;
            }

            Rect adjustedFillRect = fillRect;
            float hourBoxWidth = DefaultHourBoxWidth;
            float baseOffsetX = DefaultBaseOffsetX;

            for (int i = 0; i < columns.Count; i++)
            {
                float width = GetColumnWidth(table, columns[i], i);
                if (IsTimetableColumn(columns[i].workerClass))
                {
                    hourBoxWidth = (width / 24f) - HourBoxGap;
                    baseOffsetX = DefaultBaseOffsetX;
                    break;
                }

                adjustedFillRect.x += width;
                adjustedFillRect.width -= width;
            }

            float headerHeight = GetHeaderHeight(table);
            float windowHeight = Mathf.Max(GetTableHeight(table) - headerHeight - PawnAreaBottomTrim, 0f);
            geometry = new ChronosScheduleGeometrySnapshot(
                adjustedFillRect,
                baseOffsetX,
                BaseOffsetY,
                hourBoxWidth,
                HourBoxGap,
                HourBoxHeight,
                PawnAreaTopOffset,
                windowHeight,
                headerHeight,
                Time.frameCount,
                true);

            return true;
        }

        private static bool IsTimetableColumn(System.Type workerClass)
        {
            return workerClass != null && typeof(PawnColumnWorker_Timetable).IsAssignableFrom(workerClass);
        }

        private static PawnTable GetPawnTable(MainTabWindow_PawnTable scheduleWindow)
        {
            if (scheduleWindow == null)
            {
                return null;
            }

#if V1_2U
            return scheduleWindow.table;
#else
            return AccessTools.Field(scheduleWindow.GetType(), "table")?.GetValue(scheduleWindow) as PawnTable
                ?? AccessTools.Field(typeof(MainTabWindow_PawnTable), "table")?.GetValue(scheduleWindow) as PawnTable;
#endif
        }

        private static float GetTableHeight(PawnTable table)
        {
#if V1_2U
            return table.cachedSize.y;
#else
            return table.Size.y;
#endif
        }

        private static float GetHeaderHeight(PawnTable table)
        {
#if V1_2U
            return table.cachedHeaderHeight;
#else
            return table.HeaderHeight;
#endif
        }

        private static float GetColumnWidth(PawnTable table, PawnColumnDef column, int index)
        {
            float cachedWidth = GetCachedColumnWidth(table, index);
            if (cachedWidth > 0f)
            {
                return cachedWidth;
            }

            if (column == null)
            {
                return 0f;
            }

#if V0_19U || V1_0U || V1_1U || V1_2U || V1_3U || V1_4U || V1_5U || V1_6U
            if (column.width > 0f)
            {
                return column.width;
            }
#endif

            return column.Worker.GetOptimalWidth(table);
        }

        private static float GetCachedColumnWidth(PawnTable table, int index)
        {
            if (table == null || index < 0)
            {
                return 0f;
            }

#if V1_2U
            if (table.cachedColumnWidths != null && index >= 0 && index < table.cachedColumnWidths.Count)
            {
                return table.cachedColumnWidths[index];
            }
#else
            List<float> cachedColumnWidths = AccessTools.Field(typeof(PawnTable), "cachedColumnWidths")?.GetValue(table) as List<float>;
            if (cachedColumnWidths != null && index < cachedColumnWidths.Count)
            {
                return cachedColumnWidths[index];
            }
#endif

            return 0f;
        }
    }
}

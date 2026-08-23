using ChronosPointer.Api;
using RimWorld;
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

        public static bool TryCreate(MainTabWindow_Schedule scheduleWindow, Rect fillRect, out ChronosScheduleGeometrySnapshot geometry)
        {
            geometry = null;
            if (scheduleWindow == null || scheduleWindow.table == null)
            {
                return false;
            }

            PawnTable table = scheduleWindow.table;
#if V1_3
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

            for (int i = 0; i < columns.Count; i++)
            {
                float width = GetColumnWidth(table, columns[i], i);
                if (IsTimetableColumn(columns[i].workerClass))
                {
                    hourBoxWidth = (width / 24f) - HourBoxGap;
                    break;
                }

                adjustedFillRect.x += width;
                adjustedFillRect.width -= width;
            }

            float windowHeight = Mathf.Max(table.cachedSize.y - table.cachedHeaderHeight - PawnAreaBottomTrim, 0f);
            geometry = new ChronosScheduleGeometrySnapshot(
                adjustedFillRect,
                DefaultBaseOffsetX,
                BaseOffsetY,
                hourBoxWidth,
                HourBoxGap,
                HourBoxHeight,
                PawnAreaTopOffset,
                windowHeight,
                table.cachedHeaderHeight,
                Time.frameCount,
                true);

            return true;
        }

        private static bool IsTimetableColumn(System.Type workerClass)
        {
            return workerClass != null && typeof(PawnColumnWorker_Timetable).IsAssignableFrom(workerClass);
        }

        private static float GetColumnWidth(PawnTable table, PawnColumnDef column, int index)
        {
            if (table.cachedColumnWidths != null && index >= 0 && index < table.cachedColumnWidths.Count)
            {
                return table.cachedColumnWidths[index];
            }

            return column != null ? column.width : 0f;
        }
    }
}

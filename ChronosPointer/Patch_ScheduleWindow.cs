using System;
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

        [HarmonyPostfix]
        public static void Postfix(MainTabWindow_Schedule __instance, Rect fillRect)
        {
            if (Find.CurrentMap == null) return;

            try
            {
                // 1) Day/Night Bar
                if (ChronosPointerMod.Settings.showDayNightBar)
                {
                    DrawDayNightBar(fillRect);
                    if (ChronosPointerMod.Settings.showDayNightIndicator)
                    {
                        DrawTimeTraceLine(fillRect);
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

        #region Day/Night Bar
        private static void DrawDayNightBar(Rect fillRect)
        {
            float baseX = fillRect.x + BaseOffsetX;
            float baseY = fillRect.y + BaseOffsetY;

            for (int hour = 0; hour < 24; hour++)
            {
                float hourX = baseX + hour * (HourBoxWidth + HourBoxGap);
                Rect hourRect = new Rect(hourX, baseY, HourBoxWidth, BarHeight);

                float sunlight = GenCelestial.CelestialSunGlow(Find.CurrentMap.Tile, hour * 2500);
                Color c = GetColorForSunlight(sunlight);
                Widgets.DrawBoxSolid(hourRect, c);
            }
        }

        private static Color GetColorForSunlight(float sunlight)
        {
            // Deep night
            if (sunlight < 0.15f)
                return new Color(0f, 0f, 0.5f);  // Deep Blue

            // Dawn/Dusk
            if (sunlight < 0.3f)
                return new Color(0.5f, 0.5f, 1f); // Light Blue

            // Sunrise/Sunset
            if (sunlight < 0.7f)
                return new Color(1f, 0.5f, 0f);   // Orange

            // Full daylight
            return Color.yellow;
        }
        #endregion

        #region Highlight
        /// <summary>
        /// Yellow highlight with top/bottom trim so it doesn't slip off.
        /// Top offset: PawnAreaTopOffset (16 px).
        /// Bottom trim: PawnAreaBottomTrim (6 px).
        /// </summary>
        private static void DrawHighlight(Rect fillRect)
        {
            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f;
            int currentHour = (int)currentHourF;

            float colX = fillRect.x + BaseOffsetX
                         + currentHour * (HourBoxWidth + HourBoxGap);
            float colY = fillRect.y + BaseOffsetY + BarHeight + PawnAreaTopOffset;

            //var pawns = PawnsFinder.AllMaps_FreeColonists.Where(p => p.timetable != null).ToList();
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
        #endregion

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
        #endregion

        #region Arrow Texture
        /// <summary>
        /// Draws the arrow texture, rotated 90 degrees to point downward,
        /// hovering just above the time trace line.
        /// </summary>
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

        #region Full-Height Cursor
        /// <summary>
        /// White vertical line across pawns, with the same top offset & bottom trim as highlight.
        /// So it's not 10 px too tall.
        /// </summary>
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

            // Calculate total rows
            // var pawns = .Where(p => p.timetable != null).ToList();

            //Find.ColonistBar.Entries[].map;
            //var pawns = PawnsFinder.AllMaps_FreeColonists.Where()
            var babyList = Find.CurrentMap.mapPawns.SpawnedBabiesInFaction(Find.FactionManager.OfPlayer).ToList();
            int babyCount = 0;
            if (babyList != null)
                babyCount= babyList.Count;

            Log.Message(babyList.Count);

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
        #endregion
    }
}
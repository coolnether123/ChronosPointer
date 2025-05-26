using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Mono.Unix.Native;
using RimWorld;
using UnityEngine; // Added for Time.time if you were using it in Aurora
using Verse;

namespace ChronosPointer
{
    [HarmonyPatch(typeof(PawnColumnWorker_Timetable))]
    [HarmonyPatch("DoCell")]
    public static class Patch_DayNightPositionGetter
    {
        static bool hasRect = false; // This should ideally be reset if the UI ever fully reinitializes
        [HarmonyPostfix]
        public static void Postfix(Rect rect, Pawn pawn, PawnTable table) // Removed __instance as it wasn't used
        {
            if (!hasRect || Patch_ScheduleWindow.UseMeForTheXYPosOfDayNightBar == default(Rect)) // Ensure it's a valid rect
            {
                Patch_ScheduleWindow.UseMeForTheXYPosOfDayNightBar = rect;
                if (rect.width > 0 && rect.height > 0) // Basic validation
                {
                    hasRect = true;
                }
            }
        }
    }


    [HarmonyPatch(typeof(MainTabWindow_Schedule))]
    [HarmonyPatch("DoWindowContents")]
    public static class Patch_ScheduleWindow
    {
        #region Values
        public static Rect UseMeForTheXYPosOfDayNightBar;

        public static float BaseOffsetX = 1f;
        public static float BaseOffsetY = 40f; // Made it float consistently

        public static float HourBoxWidth = 19f;
        public static float HourBoxGap = 2f;

        public static float PawnRowHeight = 28f;
        public static float PawnRowGap = 2f;

        public static float BarHeight = 10f;

        public static float PawnAreaTopOffset = 16f;
        public static float PawnAreaBottomTrim = 2f;

        private static readonly GameConditionDef SolarFlareDef = DefDatabase<GameConditionDef>.GetNamed("SolarFlare", false); // Added 'false' to not error if missing (though it shouldn't be)

        public static bool dayNightColorsCalculated = false;
        public static bool pawnCountCalculated = false; // We might not need this if GetPawnCount is called every frame

        private static Color[] dayNightColors = new Color[24];
        private static Map lastKnownMap = null;
        private static int pawnCount = 0; // This will be totalEffectiveRows

        private static readonly string gplPackageId = "name.krypt.rimworld.pawntablegrouped";
        private static bool isGPLModActiveCheckedThisFrame = false;
        private static bool isGPLModActiveResult = false;


        // List of mod IDs to check for specific compatibility handling
        private static readonly Dictionary<string, Action> modCompatibilityActions = new Dictionary<string, Action>()
        {
            { "Mysterius.CustomSchedules", ApplyFixForMysteriusCustomSchedules }
            // We will handle GPL separately due to its complexity
        };
        #endregion

        #region GroupedPawnsList_Compatibility_Helpers
        private static bool gplReflectionAttempted = false;
        private static bool gplReflectionSucceeded = false;

        private static Type gpl_PawnTableGroupedImplType = null;
        private static Type gpl_PawnTableGroupedModelType = null;
        private static Type gpl_PawnTableGroupType = null;

        private static FieldInfo gpl_PawnTableGroupedImpl_modelField = null;
        private static FieldInfo gpl_PawnTableGroupedModel_GroupsField = null;
        private static MethodInfo gpl_PawnTableGroupedModel_IsExpandedMethod = null;
        private static FieldInfo gpl_PawnTableGroup_PawnsField = null;

        // For PawnTableExtensions
        private static Type gpl_PawnTableExtensionsType = null;
        private static MethodInfo gpl_TryGetImplementationMethod = null;

        public static float GPLDayNightBarYOffset = -30f;  // tweak this until the bar sits above the hour numbers
        private static float gplHorizontalOffset = 173f; // Offset for the whole block on X axis
        private static float gplVerticalOffset = 0f;   // Offset for the whole block on Y axis
        public static float GPLHighlightYOffset = GPLDayNightBarYOffset;  // reuse the same Y shift
        public static float GPLHighlightHeightAdd = 2f;                     // fills the 2‑px gap

        private static void InitializeGPLReflection()
        {
            if (gplReflectionAttempted) return;
            gplReflectionAttempted = true;
            Log.Message("ChronosPointer: Attempting to Initialize GPL Reflection...");

            if (!isGPLModActiveResult)
            {
                Log.Message("ChronosPointer: InitializeGPLReflection called but isGPLModActiveResult is false. Skipping.");
                return;
            }

            string targetNamespace = "PawnTableGrouped";

            // Get GPL Core Types
            gpl_PawnTableGroupedImplType = AccessTools.TypeByName($"{targetNamespace}.PawnTableGroupedImpl");
            gpl_PawnTableGroupedModelType = AccessTools.TypeByName($"{targetNamespace}.PawnTableGroupedModel");
            gpl_PawnTableGroupType = AccessTools.TypeByName($"{targetNamespace}.PawnTableGroup");

            // NEW: Get PawnTableExtensions type
            gpl_PawnTableExtensionsType = AccessTools.TypeByName($"{targetNamespace}.PawnTableExtensions");

            if (gpl_PawnTableGroupedImplType == null || gpl_PawnTableGroupedModelType == null ||
                gpl_PawnTableGroupType == null || gpl_PawnTableExtensionsType == null) // Added check for Extensions type
            {
                Log.Warning("ChronosPointer: Could not find one or more critical types for Grouped Pawns List compatibility (Core types or PawnTableExtensions). GPL support will be disabled.");
                return;
            }
            Log.Message("ChronosPointer: All required GPL base types (including PawnTableExtensions) FOUND.");

            bool allMembersFound = true;

            // Get members from PawnTableGroupedImpl, PawnTableGroupedModel, PawnTableGroup
            gpl_PawnTableGroupedImpl_modelField = AccessTools.Field(gpl_PawnTableGroupedImplType, "model");
            // ... (rest of your existing member reflection: GroupsField, IsExpandedMethod, PawnsField) ...
            // Ensure these logs and checks are still in place:
            if (gpl_PawnTableGroupedImpl_modelField == null) { /* Log warning, allMembersFound = false; */ } else { /* Log FOUND */ }
            gpl_PawnTableGroupedModel_GroupsField = AccessTools.Field(gpl_PawnTableGroupedModelType, "Groups");
            if (gpl_PawnTableGroupedModel_GroupsField == null) { /* Log warning, allMembersFound = false; */ } else { /* Log FOUND */ }
            gpl_PawnTableGroupedModel_IsExpandedMethod = AccessTools.Method(gpl_PawnTableGroupedModelType, "IsExpanded", new Type[] { gpl_PawnTableGroupType });
            if (gpl_PawnTableGroupedModel_IsExpandedMethod == null) { /* Log warning, allMembersFound = false; */ } else { /* Log FOUND */ }
            gpl_PawnTableGroup_PawnsField = AccessTools.Field(gpl_PawnTableGroupType, "Pawns");
            if (gpl_PawnTableGroup_PawnsField == null) { /* Log warning, allMembersFound = false; */ } else { /* Log FOUND */ }


            // NEW: Get PawnTableExtensions.TryGetImplementation method
            // It's a static method: bool TryGetImplementation(PawnTable table, out PawnTableGroupedImpl implementation)
            // The 'out' parameter type is gpl_PawnTableGroupedImplType.MakeByRefType() for reflection
            Log.Message($"ChronosPointer: Trying to get Method 'TryGetImplementation' from type {gpl_PawnTableExtensionsType.FullName}");
            gpl_TryGetImplementationMethod = AccessTools.Method(gpl_PawnTableExtensionsType, "TryGetImplementation",
                new Type[] { typeof(PawnTable), gpl_PawnTableGroupedImplType.MakeByRefType() });
            if (gpl_TryGetImplementationMethod == null)
            {
                Log.Warning($"ChronosPointer: FAILED to find Method 'TryGetImplementation' in {gpl_PawnTableExtensionsType.FullName}");
                allMembersFound = false;
            }
            else
            {
                Log.Message("ChronosPointer: Method 'TryGetImplementation' FOUND.");
            }


            if (!allMembersFound)
            {
                Log.Warning("ChronosPointer: Failed to cache one or more required reflection members for Grouped Pawns List. GPL support will be disabled.");
                return;
            }

            gplReflectionSucceeded = true;
            Log.Message("[ChronosPointer] Grouped Pawns List reflection successful. Compatibility fully enabled for this session.");
        }

        private static bool TryGetGPLModelAndGroups(PawnTable vanillaPawnTable,
                                            out object modelInstance,
                                            out System.Collections.IList groupsList)
        {
            modelInstance = null;
            groupsList = null;

            if (!gplReflectionSucceeded || gpl_TryGetImplementationMethod == null) // Check if our key method was found
            {
                return false;
            }
            if (vanillaPawnTable == null) return false;

            try
            {
                object[] parameters = new object[] { vanillaPawnTable, null }; // Second param is for 'out PawnTableGroupedImpl'
                bool success = (bool)gpl_TryGetImplementationMethod.Invoke(null, parameters); // Static method, so first arg is null

                if (success)
                {
                    object gplImplementationInstance = parameters[1]; // The 'out' parameter value is here

                    if (gplImplementationInstance == null || gplImplementationInstance.GetType() != gpl_PawnTableGroupedImplType)
                    {
                        Log.ErrorOnce("ChronosPointer: GPL TryGetImplementation succeeded but returned null or wrong type for impl.", "GPLCompatImplTypeError".GetHashCode());
                        return false;
                    }

                    modelInstance = gpl_PawnTableGroupedImpl_modelField.GetValue(gplImplementationInstance);
                    if (modelInstance == null)
                    {
                        Log.ErrorOnce("ChronosPointer: GPL Compat - Model instance is null (from reflected TryGetImplementation).", "GPLCompatModelNull4".GetHashCode());
                        return false;
                    }

                    object groupsObj = gpl_PawnTableGroupedModel_GroupsField.GetValue(modelInstance);
                    if (groupsObj == null)
                    {
                        Log.ErrorOnce("ChronosPointer: GPL Compat - Groups object is null.", "GPLCompatGroupsNull4".GetHashCode());
                        return false;
                    }

                    groupsList = groupsObj as System.Collections.IList;
                    if (groupsList == null)
                    {
                        Log.ErrorOnce("ChronosPointer: GPL Compat - Groups object is not an IList.", "GPLCompatGroupsNotIList4".GetHashCode());
                        return false;
                    }
                    // Log.Message("ChronosPointer: TryGetGPLModelAndGroups returning TRUE. groupsList count: " + groupsList.Count);
                    return true;
                }
                else
                {
                    // TryGetImplementation returned false, meaning GPL is not active for this table. This is normal.
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.ErrorOnce($"ChronosPointer: Exception in TryGetGPLModelAndGroups (using TryGetImplementation): {ex.ToString()}", ex.GetHashCode() + 6);
                return false;
            }
        }

        private static List<Pawn> GetPawnsFromGPLGroup(object groupObj)
        {
            // Ensure reflection succeeded and the field was found.
            if (groupObj == null || !gplReflectionSucceeded || gpl_PawnTableGroup_PawnsField == null || groupObj.GetType() != gpl_PawnTableGroupType)
            {
                return new List<Pawn>();
            }

            try
            {
                // Pawns are from a field, so use gpl_PawnTableGroup_PawnsField
                var pawnsListObj = gpl_PawnTableGroup_PawnsField.GetValue(groupObj);
                return pawnsListObj as List<Pawn> ?? new List<Pawn>();
            }
            catch (Exception ex)
            {
                Log.ErrorOnce($"ChronosPointer: Exception in GetPawnsFromGPLGroup for groupObj type {groupObj.GetType().FullName}: {ex.ToString()}", groupObj.GetHashCode() + 3);
                return new List<Pawn>();
            }
        }

        private static int CountExpandedGroups(object modelInstance, System.Collections.IList groupsList)
        {
            if (modelInstance == null || groupsList == null) return 0;
            int count = 0;
            foreach (var groupObj in groupsList)
            {
                if (IsGPLGroupExpanded(modelInstance, groupObj))
                    count++;
            }
            return count;
        }


        private static bool IsGPLGroupExpanded(object modelInstance, object groupObj)
        {
            if (modelInstance == null || groupObj == null || !gplReflectionSucceeded || gpl_PawnTableGroupedModel_IsExpandedMethod == null ||
                modelInstance.GetType() != gpl_PawnTableGroupedModelType || groupObj.GetType() != gpl_PawnTableGroupType)
            {
                return false; // Default to not expanded if types don't match or reflection failed
            }

            try
            {
                return (bool)gpl_PawnTableGroupedModel_IsExpandedMethod.Invoke(modelInstance, new object[] { groupObj });
            }
            catch (Exception ex)
            {
                Log.ErrorOnce($"ChronosPointer: Exception in IsGPLGroupExpanded: {ex.ToString()}", modelInstance.GetHashCode() + groupObj.GetHashCode() + 4);
                return false; // Default to not expanded on error
            }
        }
        #endregion

        [HarmonyPostfix]
        public static void Postfix(MainTabWindow_Schedule __instance, Rect fillRect)
        {
            if (Find.CurrentMap == null) return;
            if (UseMeForTheXYPosOfDayNightBar.width <= 0 || UseMeForTheXYPosOfDayNightBar.height <= 0) return;

            Rect drawingAnchorRect = UseMeForTheXYPosOfDayNightBar; // Start with the base position

            // Cache GPL active status per frame
            if (!isGPLModActiveCheckedThisFrame)
            {
                isGPLModActiveResult = ModLister.AllInstalledMods.Any(mod =>
                    mod.Active && mod.PackageId.Equals(gplPackageId, StringComparison.OrdinalIgnoreCase));
                isGPLModActiveCheckedThisFrame = true;
                // No log here, already in Initialize
                if (isGPLModActiveResult) InitializeGPLReflection();
            }

            // --- APPLY GPL Global Offset if active AND reflection succeeded ---
            if (isGPLModActiveResult && gplReflectionSucceeded)
            {
                // Apply the horizontal and vertical offsets to the anchor point
                drawingAnchorRect.x += gplHorizontalOffset;
                drawingAnchorRect.y += gplVerticalOffset;

                // Optional: If the offset fundamentally changes where the Day/Night bar
                // is relative to the pawn list *within* your block, you might need to
                // adjust BaseOffsetY or PawnAreaTopOffset here based on GPL active state.
                // For now, assume they remain constant relative to the offset anchor.
            }
            // --- END APPLYING OFFSET ---

            try
            {
                foreach (var modActionEntry in modCompatibilityActions)
                {
                    if (ModLister.AllInstalledMods.Any(mod =>
                        mod.Active && mod.PackageId.Equals(modActionEntry.Key, StringComparison.OrdinalIgnoreCase)))
                    {
                        modActionEntry.Value?.Invoke();
                    }
                }

                int incident = IncidentHappening();
                pawnCount = GetPawnCount(__instance);

                if (Find.CurrentMap != lastKnownMap)
                {
                    dayNightColorsCalculated = false;
                    lastKnownMap = Find.CurrentMap;
                }

                if (ChronosPointerMod.Settings.showDayNightBar)
                {
                    if (!dayNightColorsCalculated || incident >= 0)
                    {
                        CalculateDayNightColors(incident);
                    }
                    DrawDayNightBar(drawingAnchorRect, dayNightColors);

                    if (incident == 5)
                    {
                        Color[] auroraEffectOverlay = new Color[24];
                        float time = Time.time;
                        Color auroraShimmerColor = new Color(
                            0.3f + Mathf.Abs(Mathf.Sin(time * 0.7f + 0.5f)) * 0.5f,
                            0.5f + Mathf.Abs(Mathf.Sin(time * 0.9f + 1.0f)) * 0.4f,
                            0.6f + Mathf.Abs(Mathf.Sin(time * 0.5f + 1.5f)) * 0.4f,
                            0.25f + Mathf.Abs(Mathf.Sin(time * 0.4f)) * 0.20f
                        );
                        for (int hour = 0; hour < 24; hour++)
                        {
                            auroraEffectOverlay[hour] = auroraShimmerColor;
                        }
                        DrawDayNightBar(drawingAnchorRect, auroraEffectOverlay);
                    }

                    if (ChronosPointerMod.Settings.showDayNightIndicator)
                    {
                        DrawDayNightTimeIndicator(drawingAnchorRect);
                    }
                }

                if (ChronosPointerMod.Settings.enableArrow)
                {
                    DrawArrowTexture(drawingAnchorRect);
                }

                if (pawnCount > 0)
                {
                    if (ChronosPointerMod.Settings.showHighlight)
                    {
                        DrawHighlight(drawingAnchorRect, __instance, pawnCount); // Pass adjusted rect
                    }
                    if (ChronosPointerMod.Settings.showPawnLine)
                    {
                        DrawFullHeightCursor(drawingAnchorRect, __instance, pawnCount); // Pass adjusted rect
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"ChronosPointer: Error in schedule patch - {e.Message}\n{e.StackTrace}");
            }

            isGPLModActiveCheckedThisFrame = false;
        }

        private static void ApplyFixForMysteriusCustomSchedules()
        {
            Log.ErrorOnce("ChronosPointer: Custom Schedules (continued) is Active. Chronos Pointer may have UI overlap.", "CSCompatWarning".GetHashCode());
        }

        #region Day/Night Bar and Time Calculations
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
                Color baseSunlightColor = GetColorForSunlight(sunlight);
                dayNightColors[localHour] = baseSunlightColor;

                switch (incident)
                {
                    case 1: // Solar Flare
                        if (sunlight > 0.05f)
                        {
                            if (baseSunlightColor == new Color(1f, 0.5f, 0f) || baseSunlightColor == Color.yellow)
                            {
                                dayNightColors[localHour] = Color.yellow;
                            }
                            // Light Blue stays Light Blue
                        }
                        break;
                    case 2: dayNightColors[localHour] = new Color(0f, 0f, 0.5f); break; // Eclipse
                    case 3: dayNightColors[localHour] = new Color(dayNightColors[localHour].r, dayNightColors[localHour].g * 1.6f, dayNightColors[localHour].b); break; // Toxic Fallout
                    case 4: dayNightColors[localHour] = new Color(dayNightColors[localHour].r * 0.5f, dayNightColors[localHour].g * 0.5f, dayNightColors[localHour].b * 0.5f); break; // Volcanic Winter
                }
            }
            dayNightColorsCalculated = true;
        }

        private static int IncidentHappening()
        {
            if (Find.CurrentMap == null || Find.CurrentMap.gameConditionManager == null) return 0;
            if (SolarFlareDef != null && Find.CurrentMap.gameConditionManager.ConditionIsActive(SolarFlareDef)) return 1;
            if (Find.CurrentMap.gameConditionManager.ConditionIsActive(GameConditionDefOf.Eclipse)) return 2;
            if (Find.CurrentMap.gameConditionManager.ConditionIsActive(GameConditionDefOf.ToxicFallout)) return 3;
            if (Find.CurrentMap.gameConditionManager.ConditionIsActive(GameConditionDefOf.VolcanicWinter)) return 4;
            if (Find.CurrentMap.gameConditionManager.ConditionIsActive(GameConditionDefOf.Aurora)) return 5;
            return 0;
        }

        private static void DrawDayNightBar(Rect anchorRect, Color[] colors)
        {
            float baseX = anchorRect.x + BaseOffsetX;
            float baseY = anchorRect.y + BaseOffsetY;

            if (isGPLModActiveResult && gplReflectionSucceeded)
                baseY += GPLDayNightBarYOffset;

            for (int hour = 0; hour < 24; hour++)
            {
                Rect hourRect = new Rect(baseX + hour * (HourBoxWidth + HourBoxGap), baseY, HourBoxWidth, BarHeight);
                Widgets.DrawBoxSolid(hourRect, colors[hour]);
            }
        }

        private static Color GetColorForSunlight(float sunlight)
        {
            if (sunlight <= 0f) return new Color(0f, 0f, 0.5f);  // Deep Blue
            if (sunlight < 0.35f) return new Color(0.5f, 0.5f, 1f); // Light Blue
            if (sunlight < 0.7f) return new Color(1f, 0.5f, 0f);   // Orange
            return Color.yellow; // Full daylight
        }

        private static void DrawDayNightTimeIndicator(Rect anchorRect)
        {
            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f;
            float lineX = anchorRect.x + BaseOffsetX + currentHourF * HourBoxWidth + (Mathf.FloorToInt(currentHourF) * HourBoxGap); // Adjusted for gaps
            // Simplified X for continuous movement:
            lineX = anchorRect.x + BaseOffsetX + (currentHourF * (HourBoxWidth + HourBoxGap)) - (currentHourF / (24 - (24 / HourBoxGap)) * (HourBoxGap * 23)); //This is not quite right.
            // Correct X for continuous movement:
            lineX = anchorRect.x + BaseOffsetX + (currentHourF * (HourBoxWidth + HourBoxGap)) - (((currentHourF - Mathf.Floor(currentHourF))) * HourBoxGap);
            // Let's use the simpler calculation that was working before based on hour progress.


            int currentHourInt = (int)currentHourF;
            float hourProgress = currentHourF - currentHourInt;
            lineX = anchorRect.x + BaseOffsetX + currentHourInt * (HourBoxWidth + HourBoxGap) + hourProgress * HourBoxWidth;


            float lineY = anchorRect.y + BaseOffsetY;
            if (isGPLModActiveResult && gplReflectionSucceeded)
                lineY += GPLDayNightBarYOffset;
            long currentAbsoluteTick = GenTicks.TicksAbs;
            float sunlightAtCurrentTick = GenCelestial.CelestialSunGlow(Find.CurrentMap.Tile, (int)currentAbsoluteTick);
            Color lineColor = !ChronosPointerMod.Settings.useDynamicTimeTraceLine ? ChronosPointerMod.Settings.timeTraceColorDay : (sunlightAtCurrentTick >= 0.7f) ? ChronosPointerMod.Settings.timeTraceColorDay : ChronosPointerMod.Settings.timeTraceColorNight;
            Widgets.DrawBoxSolid(new Rect(lineX, lineY, 2f, BarHeight), lineColor);
        }

        private static void DrawArrowTexture(Rect anchorRect)
        {
            if (ChronosPointerTextures.ArrowTexture == null) return;
            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f;
            int currentHourInt = (int)currentHourF;
            float hourProgress = currentHourF - currentHourInt;

            float arrowCenterX = anchorRect.x + BaseOffsetX + currentHourInt * (HourBoxWidth + HourBoxGap) + hourProgress * HourBoxWidth + 1f; // Center of the 2px line
            float barTopY = anchorRect.y + BaseOffsetY;
            if (isGPLModActiveResult && gplReflectionSucceeded)
                barTopY += GPLDayNightBarYOffset + 2;
            float arrowWidth = ChronosPointerMod.Settings.showDayNightBar ? 8f : 12f; // Make arrow bigger if bar is hidden
            float arrowHeight = ChronosPointerMod.Settings.showDayNightBar ? 8f : 12f;
            float arrowRectX = arrowCenterX - (arrowWidth / 2f);
            float arrowRectY = barTopY - arrowHeight - (ChronosPointerMod.Settings.showDayNightBar ? 3f : -2f); // Adjust vertical position

            Rect arrowRect = new Rect(arrowRectX, arrowRectY, arrowWidth, arrowHeight);
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.color = ChronosPointerMod.Settings.arrowColor;
            GUIUtility.RotateAroundPivot(90f, arrowRect.center);
            GUI.DrawTexture(arrowRect, ChronosPointerTextures.ArrowTexture);
            GUI.matrix = oldMatrix;
            GUI.color = Color.white; // Reset GUI.color
        }
        #endregion

        #region Pawn Area Elements (Highlight & Full Height Cursor)

        private static PawnTable GetPawnTableFromScheduleWindow(MainTabWindow_Schedule scheduleWindowInstance)
        {
            if (scheduleWindowInstance == null) return null;
            var pawnTableField = typeof(MainTabWindow_PawnTable).GetField("table", BindingFlags.Instance | BindingFlags.NonPublic);
            if (pawnTableField == null)
            {
                Log.ErrorOnce("ChronosPointer: Could not find 'table' field in MainTabWindow_PawnTable.", "GetPawnTableError".GetHashCode());
                return null;
            }
            return pawnTableField.GetValue(scheduleWindowInstance) as PawnTable;
        }

        public static int GetPawnCount(MainTabWindow_Schedule __instance)
        {
            PawnTable pawnTable = GetPawnTableFromScheduleWindow(__instance);
            if (pawnTable == null) return 0;

            if (isGPLModActiveResult && TryGetGPLModelAndGroups(pawnTable, out object modelInstance, out System.Collections.IList groupsList))
            {
                int effectiveRowCount = 0;
                if (groupsList != null)
                {
                    foreach (var groupObj in groupsList)
                    {
                        if (IsGPLGroupExpanded(modelInstance, groupObj))
                        {
                            effectiveRowCount += GetPawnsFromGPLGroup(groupObj).Count;
                        }
                        else
                        {
                            effectiveRowCount++;
                        }
                    }
                }
                // pawnCountCalculated = true; // Not strictly needed if called every frame
                return effectiveRowCount;
            }
            else
            {
                var pawnsProperty = __instance.GetType().GetProperty("Pawns", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (pawnsProperty == null) return 0;
                var pawnsIEnumerable = pawnsProperty.GetValue(__instance) as IEnumerable<Pawn>;
                if (pawnsIEnumerable == null) return 0;
                // pawnCountCalculated = true; // Not strictly needed
                return pawnsIEnumerable.Count();
            }
        }

        private static void DrawHighlight(Rect anchorRect, MainTabWindow_Schedule scheduleInstance, int totalEffectiveRows)
        {
            PawnTable pawnTable = GetPawnTableFromScheduleWindow(scheduleInstance);
            if (pawnTable == null || totalEffectiveRows == 0) return;

            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f;
            int currentHour = (int)currentHourF;
            float colX = anchorRect.x + BaseOffsetX + currentHour * (HourBoxWidth + HourBoxGap); // Uses the potentially offset anchorRect.x

            // startYForPawnContent is already relative to the (potentially offset) anchorRect
            float startYForPawnContent = anchorRect.y + BaseOffsetY + BarHeight + PawnAreaTopOffset; // Uses the potentially offset anchorRect.y

            if (isGPLModActiveResult && gplReflectionSucceeded && TryGetGPLModelAndGroups(pawnTable, out object modelInstance, out System.Collections.IList groupsList))
            {
                // --- REMOVE THE OLD gplVerticalOffsetFix application here ---
                // float currentDrawingY = startYForPawnContent + gplVerticalOffsetFix; // DELETE THIS LINE
                float currentDrawingY = startYForPawnContent; // Start from the calculated position relative to the offset anchor
                                                              // Log.Message($"GPL DRAW Highlight: StartY: {startYForPawnContent}, CurrentY (initial): {currentDrawingY}"); // Debug Y

                if (groupsList != null)
                {
                    foreach (var groupObj in groupsList)
                    {
                        bool isExpanded = IsGPLGroupExpanded(modelInstance, groupObj);
                        float segmentHeight;
                        int numPawnsInThisSegment = 0;

                        if (isExpanded)
                        {
                            List<Pawn> pawnsInGroup = GetPawnsFromGPLGroup(groupObj);
                            numPawnsInThisSegment = pawnsInGroup.Count;
                        }
                        else
                        {
                            numPawnsInThisSegment = 1;
                        }

                        segmentHeight = numPawnsInThisSegment * PawnRowHeight;
                        if (numPawnsInThisSegment > 0 && isExpanded && numPawnsInThisSegment > 1)
                        {
                            segmentHeight += (numPawnsInThisSegment - 1) * PawnRowGap;
                        }

                        float drawableSegmentHeight = segmentHeight;
                        if (isExpanded && GetPawnsFromGPLGroup(groupObj).Count == 0)
                        {
                            drawableSegmentHeight = 0;
                        }

                        if (drawableSegmentHeight > 0)
                        {
                            Rect groupHighlightRect = new Rect(colX, currentDrawingY, HourBoxWidth, drawableSegmentHeight);
                            if (ChronosPointerMod.Settings.hollowHourHighlight)
                                Widgets.DrawBoxSolidWithOutline(groupHighlightRect, Color.clear, ChronosPointerMod.Settings.highlightColor, 2);
                            else
                                Widgets.DrawBoxSolid(groupHighlightRect, ChronosPointerMod.Settings.highlightColor);
                        }
                        currentDrawingY += segmentHeight + PawnRowGap; // Advance Y for the next segment
                    }
                }
            }
            else // GPL not active or failed, use default logic
            {
                float calculatedTotalHeight = totalEffectiveRows * PawnRowHeight;
                if (totalEffectiveRows > 1)
                {
                    calculatedTotalHeight += (totalEffectiveRows - 1) * PawnRowGap;
                }
                float drawableHeight = calculatedTotalHeight - PawnAreaBottomTrim;

                if (drawableHeight > 0)
                {
                    Rect highlightRect = new Rect(colX, startYForPawnContent, HourBoxWidth, drawableHeight); // Uses startYForPawnContent relative to anchorRect
                    if (ChronosPointerMod.Settings.hollowHourHighlight)
                        Widgets.DrawBoxSolidWithOutline(highlightRect, Color.clear, ChronosPointerMod.Settings.highlightColor, 2);
                    else
                        Widgets.DrawBoxSolid(highlightRect, ChronosPointerMod.Settings.highlightColor);
                }
            }
        }
        private static void DrawFullHeightCursor(Rect anchorRect,MainTabWindow_Schedule scheduleInstance,int totalEffectiveRows)
        {
            PawnTable pawnTable = GetPawnTableFromScheduleWindow(scheduleInstance);
            if (pawnTable == null || totalEffectiveRows == 0) return;

            float currentHourF = GenLocalDate.DayPercent(Find.CurrentMap) * 24f;
            int currentHour = (int)currentHourF;
            float hourProgress = currentHourF - currentHour;
            float cursorX =
                anchorRect.x
                + BaseOffsetX
                + currentHour * (HourBoxWidth + HourBoxGap)
                + hourProgress * HourBoxWidth
                + 1f;
            float cursorThickness = ChronosPointerMod.Settings.cursorThickness;
            if (cursorThickness % 2 != 0) cursorThickness += 1f;
            cursorX -= cursorThickness / 2f;

            float startYForPawnContent =
                anchorRect.y
                + BaseOffsetY
                + BarHeight
                + PawnAreaTopOffset;

            float lineStartY = startYForPawnContent - PawnRowHeight - 3f; // 2px higher, as you found perfect
            float lineEndY = startYForPawnContent; // will be set below

            if (
                isGPLModActiveResult
                && gplReflectionSucceeded
                && TryGetGPLModelAndGroups(
                    pawnTable,
                    out object modelInstance2,
                    out System.Collections.IList groupsList2
                )
            )
            {
                // Calculate total rows: group headers + visible pawns
                if (groupsList2 != null)
                {
                    int totalRows = 0;
                    foreach (var groupObj in groupsList2)
                    {
                        totalRows++; // group header
                        bool isExpanded = IsGPLGroupExpanded(modelInstance2, groupObj);
                        if (isExpanded)
                        {
                            List<Pawn> pawnsInGroup = GetPawnsFromGPLGroup(groupObj);
                            totalRows += pawnsInGroup.Count;
                        }
                    }
                    int totalGaps = Math.Max(0, totalRows - 1);

                    float totalHeight = totalRows * PawnRowHeight + totalGaps * PawnRowGap;
                    lineEndY = lineStartY + totalHeight;
                }
            }
            else // GPL not active or failed, use default logic
            {
                float calculatedTotalHeight = totalEffectiveRows * PawnRowHeight;
                int totalGaps = Math.Max(0, totalEffectiveRows - 1);
                calculatedTotalHeight += totalGaps * PawnRowGap;
                lineEndY = lineStartY + calculatedTotalHeight;
            }

            float lineHeight = lineEndY - lineStartY;

            // Clamp the line height to not exceed the safe drawable area
            const float maxLineHeight = 836f;
            if (lineHeight > maxLineHeight)
                lineHeight = maxLineHeight;

            if (lineHeight > 0)
            {
                Rect cursorRect = new Rect(
                    cursorX,
                    lineStartY,
                    cursorThickness,
                    lineHeight
                );
                Widgets.DrawBoxSolid(
                    cursorRect,
                    ChronosPointerMod.Settings.bottomCursorColor
                );
            }
        }

        #endregion
    }
}
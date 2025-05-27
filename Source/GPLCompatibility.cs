using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ChronosPointer
{
    public static class GPLCompatibility
    {
        #region GPL Detection and Reflection
        private static readonly string gplPackageId = "name.krypt.rimworld.pawntablegrouped";
        private static bool isGPLModActiveCheckedThisFrame = false;
        private static bool isGPLModActiveResult = false;

        private static bool gplReflectionAttempted = false;
        private static bool gplReflectionSucceeded = false;

        private static Type gpl_PawnTableGroupedImplType = null;
        private static Type gpl_PawnTableGroupedModelType = null;
        private static Type gpl_PawnTableGroupType = null;
        private static Type gpl_PawnTableExtensionsType = null;

        private static FieldInfo gpl_PawnTableGroupedImpl_modelField = null;
        private static FieldInfo gpl_PawnTableGroupedModel_GroupsField = null;
        private static MethodInfo gpl_PawnTableGroupedModel_IsExpandedMethod = null;
        private static FieldInfo gpl_PawnTableGroup_PawnsField = null;
        private static MethodInfo gpl_TryGetImplementationMethod = null;

        public static float GPLDayNightBarYOffset = -30f;
        private static float gplHorizontalOffset = 173f;
        private static float gplVerticalOffset = 0f;
        public static float GPLHighlightYOffset = GPLDayNightBarYOffset;
        public static float GPLHighlightHeightAdd = 2f;
        #endregion

        public static bool IsGPLActive()
        {
            if (!isGPLModActiveCheckedThisFrame)
            {
                isGPLModActiveResult = ModLister.AllInstalledMods.Any(mod =>
                    mod.Active && mod.PackageId.Equals(gplPackageId, StringComparison.OrdinalIgnoreCase));
                isGPLModActiveCheckedThisFrame = true;
                if (isGPLModActiveResult) InitializeGPLReflection();
            }
            return isGPLModActiveResult && gplReflectionSucceeded;
        }

        public static void ResetFrameCache()
        {
            isGPLModActiveCheckedThisFrame = false;
        }

        public static Rect ApplyGPLOffset(Rect originalRect)
        {
            if (!IsGPLActive()) return originalRect;

            Rect adjustedRect = originalRect;
            adjustedRect.x += gplHorizontalOffset;
            adjustedRect.y += gplVerticalOffset;
            return adjustedRect;
        }

        private static void InitializeGPLReflection()
        {
            if (gplReflectionAttempted) return;
            gplReflectionAttempted = true;
            //Log.Message("ChronosPointer: Attempting to Initialize GPL Reflection...");

            if (!isGPLModActiveResult)
            {
                //Log.Message("ChronosPointer: InitializeGPLReflection called but isGPLModActiveResult is false. Skipping.");
                return;
            }

            string targetNamespace = "PawnTableGrouped";

            gpl_PawnTableGroupedImplType = AccessTools.TypeByName($"{targetNamespace}.PawnTableGroupedImpl");
            gpl_PawnTableGroupedModelType = AccessTools.TypeByName($"{targetNamespace}.PawnTableGroupedModel");
            gpl_PawnTableGroupType = AccessTools.TypeByName($"{targetNamespace}.PawnTableGroup");
            gpl_PawnTableExtensionsType = AccessTools.TypeByName($"{targetNamespace}.PawnTableExtensions");

            if (gpl_PawnTableGroupedImplType == null || gpl_PawnTableGroupedModelType == null ||
                gpl_PawnTableGroupType == null || gpl_PawnTableExtensionsType == null)
            {
                Log.Warning("ChronosPointer: Could not find one or more critical types for Grouped Pawns List compatibility. GPL support will be disabled.");
                return;
            }

            bool allMembersFound = true;

            gpl_PawnTableGroupedImpl_modelField = AccessTools.Field(gpl_PawnTableGroupedImplType, "model");
            if (gpl_PawnTableGroupedImpl_modelField == null) { Log.Warning("ChronosPointer: GPL model field not found."); allMembersFound = false; }

            gpl_PawnTableGroupedModel_GroupsField = AccessTools.Field(gpl_PawnTableGroupedModelType, "Groups");
            if (gpl_PawnTableGroupedModel_GroupsField == null) { Log.Warning("ChronosPointer: GPL Groups field not found."); allMembersFound = false; }

            gpl_PawnTableGroupedModel_IsExpandedMethod = AccessTools.Method(gpl_PawnTableGroupedModelType, "IsExpanded", new Type[] { gpl_PawnTableGroupType });
            if (gpl_PawnTableGroupedModel_IsExpandedMethod == null) { Log.Warning("ChronosPointer: GPL IsExpanded method not found."); allMembersFound = false; }

            gpl_PawnTableGroup_PawnsField = AccessTools.Field(gpl_PawnTableGroupType, "Pawns");
            if (gpl_PawnTableGroup_PawnsField == null) { Log.Warning("ChronosPointer: GPL Pawns field not found."); allMembersFound = false; }

            gpl_TryGetImplementationMethod = AccessTools.Method(gpl_PawnTableExtensionsType, "TryGetImplementation",
                new Type[] { typeof(PawnTable), gpl_PawnTableGroupedImplType.MakeByRefType() });
            if (gpl_TryGetImplementationMethod == null) { Log.Warning("ChronosPointer: GPL TryGetImplementation method not found."); allMembersFound = false; }

            if (!allMembersFound)
            {
                Log.Warning("ChronosPointer: Failed to cache one or more required reflection members for Grouped Pawns List. GPL support will be disabled.");
                return;
            }

            gplReflectionSucceeded = true;
            //Log.Message("[ChronosPointer] Grouped Pawns List reflection successful. Compatibility fully enabled for this session.");
        }

        public static bool TryGetGPLModelAndGroups(PawnTable vanillaPawnTable,
                                            out object modelInstance,
                                            out System.Collections.IList groupsList)
        {
            modelInstance = null;
            groupsList = null;

            if (!gplReflectionSucceeded || gpl_TryGetImplementationMethod == null)
            {
                return false;
            }
            if (vanillaPawnTable == null) return false;

            try
            {
                object[] parameters = new object[] { vanillaPawnTable, null };
                bool success = (bool)gpl_TryGetImplementationMethod.Invoke(null, parameters);

                if (success)
                {
                    object gplImplementationInstance = parameters[1];

                    if (gplImplementationInstance == null || gplImplementationInstance.GetType() != gpl_PawnTableGroupedImplType)
                    {
                        Log.ErrorOnce("ChronosPointer: GPL TryGetImplementation succeeded but returned null or wrong type for impl.", "GPLCompatImplTypeError".GetHashCode());
                        return false;
                    }

                    modelInstance = gpl_PawnTableGroupedImpl_modelField.GetValue(gplImplementationInstance);
                    if (modelInstance == null)
                    {
                        Log.ErrorOnce("ChronosPointer: GPL Compat - Model instance is null.", "GPLCompatModelNull4".GetHashCode());
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
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.ErrorOnce($"ChronosPointer: Exception in TryGetGPLModelAndGroups: {ex.ToString()}", ex.GetHashCode() + 6);
                return false;
            }
        }

        public static List<Pawn> GetPawnsFromGPLGroup(object groupObj)
        {
            if (groupObj == null || !gplReflectionSucceeded || gpl_PawnTableGroup_PawnsField == null || groupObj.GetType() != gpl_PawnTableGroupType)
            {
                return new List<Pawn>();
            }

            try
            {
                var pawnsListObj = gpl_PawnTableGroup_PawnsField.GetValue(groupObj);
                return pawnsListObj as List<Pawn> ?? new List<Pawn>();
            }
            catch (Exception ex)
            {
                Log.ErrorOnce($"ChronosPointer: Exception in GetPawnsFromGPLGroup: {ex.ToString()}", groupObj.GetHashCode() + 3);
                return new List<Pawn>();
            }
        }

        public static bool IsGPLGroupExpanded(object modelInstance, object groupObj)
        {
            if (modelInstance == null || groupObj == null || !gplReflectionSucceeded || gpl_PawnTableGroupedModel_IsExpandedMethod == null ||
                modelInstance.GetType() != gpl_PawnTableGroupedModelType || groupObj.GetType() != gpl_PawnTableGroupType)
            {
                return false;
            }

            try
            {
                return (bool)gpl_PawnTableGroupedModel_IsExpandedMethod.Invoke(modelInstance, new object[] { groupObj });
            }
            catch (Exception ex)
            {
                Log.ErrorOnce($"ChronosPointer: Exception in IsGPLGroupExpanded: {ex.ToString()}", modelInstance.GetHashCode() + groupObj.GetHashCode() + 4);
                return false;
            }
        }

        public static int GetGPLPawnCount(PawnTable pawnTable)
        {
            if (!TryGetGPLModelAndGroups(pawnTable, out object modelInstance, out System.Collections.IList groupsList))
                return 0;

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
            return effectiveRowCount;
        }

        public static float CalculateGPLTotalHeight(PawnTable pawnTable, float pawnRowHeight, float pawnRowGap)
        {
            if (!TryGetGPLModelAndGroups(pawnTable, out object modelInstance, out System.Collections.IList groupsList))
                return 0f;

            if (groupsList == null) return 0f;

            int totalRows = 0;
            foreach (var groupObj in groupsList)
            {
                totalRows++; // group header
                bool isExpanded = IsGPLGroupExpanded(modelInstance, groupObj);
                if (isExpanded)
                {
                    List<Pawn> pawnsInGroup = GetPawnsFromGPLGroup(groupObj);
                    totalRows += pawnsInGroup.Count;
                }
            }
            int totalGaps = Math.Max(0, totalRows - 1);
            return totalRows * pawnRowHeight + totalGaps * pawnRowGap;
        }
    }
}
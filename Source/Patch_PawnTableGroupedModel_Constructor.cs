using HarmonyLib;
using PawnTableGrouped;
using RimWorld;
using System.Collections.Generic;

[HarmonyPatch(typeof(PawnTableGroupedModel))]
[HarmonyPatch(MethodType.Constructor)]
[HarmonyPatch(new[] { typeof(PawnTable), typeof(PawnTableDef) })]
public static class Patch_PawnTableGroupedModel_Constructor
{
    // Dictionary: PawnTable => PawnTableGroupedModel
    public static Dictionary<PawnTable, PawnTableGroupedModel> ActiveGroupedModels
        = new Dictionary<PawnTable, PawnTableGroupedModel>();

    // Postfix for the constructor (PawnTable table, PawnTableDef def)
    public static void Postfix(
        PawnTable __0,          // constructor param #1
        PawnTableDef __1,       // constructor param #2
        PawnTableGroupedModel __instance
    )
    {
        // Store the "real" model in a dictionary keyed by the PawnTable
        if (__0 != null && __instance != null)
        {
            ActiveGroupedModels[__0] = __instance;
            // Now we know that for PawnTable __0, the mod is using __instance
        }
    }
}

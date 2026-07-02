using System;
using ChronosPointer.Api;
using HarmonyLib;
using Verse;

namespace ChronosPointer
{
    [StaticConstructorOnStartup]
    public static class ChronosPointerMod
    {
        public static ChronosPointerSettings Settings = new ChronosPointerSettings();

        static ChronosPointerMod()
        {
#if VALPHA4
            Log.Message("[ChronosPointer] Alpha4 settings store initialized.");
#else
            try
            {
                new Harmony("CoolNether123.ChronosPointer").PatchAll();
                ChronosPointerApi.NotifyReady();
                Log.Message("[ChronosPointer] Legacy schedule renderer initialized.");
            }
            catch (Exception ex)
            {
                Log.Error("[ChronosPointer] Legacy Harmony patching failed: " + ex);
            }
#endif
        }
    }
}

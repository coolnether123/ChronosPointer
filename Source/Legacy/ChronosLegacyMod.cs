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
        }
    }
}

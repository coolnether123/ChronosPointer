using System;
using ChronosPointer.Api;
using HarmonyLib;
using Verse;

namespace ChronosPointer.Legacy
{
    public class ChronosAlpha4BootstrapDef : Def
    {
        public ChronosAlpha4BootstrapDef()
        {
#if VALPHA4
            ChronosAlpha4Bootstrap.Initialize();
#endif
        }
    }

#if VALPHA4
    internal static class ChronosAlpha4Bootstrap
    {
        private static readonly Harmony Harmony = new Harmony("CoolNether123.ChronosPointer.Alpha4");
        private static bool initialized;

        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;

            try
            {
                Harmony.PatchAll(typeof(ChronosAlpha4Bootstrap).Assembly);
                ChronosPointerApi.NotifyReady();
                Log.Message("[ChronosPointer] Alpha4 overview renderer initialized.");
            }
            catch (Exception ex)
            {
                Log.Error("[ChronosPointer] Alpha4 Harmony patching failed: " + ex);
            }
        }
    }
#endif
}

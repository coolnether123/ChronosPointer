using System;
using ChronosPointer.Api;
using HarmonyLib;
using Verse;

namespace ChronosPointer.Legacy
{
    [StaticConstructorOnStartup]
    public static class ChronosLegacyBootstrap
    {
        private static bool initialized;

        static ChronosLegacyBootstrap()
        {
            EnsureInitialized();
        }

        public static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;

            try
            {
                if (ChronosPointerMod.Settings == null)
                {
                    ChronosPointerMod.Settings = new ChronosPointerSettings();
                }

                var harmony = new Harmony("com.coolnether123.ChronosPointer");
                harmony.PatchAll(typeof(ChronosLegacyBootstrap).Assembly);
                ChronosPointerApi.NotifyReady();
                Log.Message("[ChronosPointer] Legacy static bootstrap applied Harmony patches.");
            }
            catch (Exception exception)
            {
                Log.Error("[ChronosPointer] Legacy static bootstrap failed: " + exception);
            }
        }
    }
}

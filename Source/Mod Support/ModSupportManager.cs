using System;
using System.Collections.Generic;
using Verse;

namespace ChronosPointer.ModSupport
{
    [StaticConstructorOnStartup]
    internal static class ModSupportManager
    {
        private const long VanillaTicksPerDay = 60000L;
        private const long VanillaTicksPerHour = 2500L;

        private static readonly List<IModSupportModule> activeModules = new List<IModSupportModule>();
        private static readonly List<IChronosTimeScaleSupport> timeScaleModules = new List<IChronosTimeScaleSupport>();

        private static readonly List<IModSupportModule> allModules = new List<IModSupportModule>
        {
            new DayStretchSupport(),
            new ClockworkSupport()
        };

        static ModSupportManager()
        {
            for (int i = 0; i < allModules.Count; i++)
            {
                IModSupportModule module = allModules[i];
                if (
#if V1_3
                    ModLister.GetActiveModWithIdentifier(module.PackageId) == null
#else
                    ModLister.GetActiveModWithIdentifier(module.PackageId, ignorePostfix: true) == null
#endif
                    )
                {
                    continue;
                }

                try
                {
                    module.OnModsDetected();
                    activeModules.Add(module);

                    if (module is IChronosTimeScaleSupport timeScaleSupport)
                    {
                        timeScaleModules.Add(timeScaleSupport);
                    }

                    Log.Message($"[ChronosPointer][ModSupport] Registered module: {module.DisplayName} ({module.PackageId}).");
                }
                catch (Exception ex)
                {
                    Log.Warning($"[ChronosPointer][ModSupport] Failed to initialize module {module.DisplayName}: {ex.Message}");
                }
            }
        }

        public static long GetTicksPerDay()
        {
            for (int i = 0; i < timeScaleModules.Count; i++)
            {
                if (timeScaleModules[i].TryGetTicksPerDay(out long ticksPerDay) && ticksPerDay > 0L)
                {
                    return ticksPerDay;
                }
            }

            return VanillaTicksPerDay;
        }

        public static long GetTicksPerHour()
        {
            for (int i = 0; i < timeScaleModules.Count; i++)
            {
                if (timeScaleModules[i].TryGetTicksPerHour(out long ticksPerHour) && ticksPerHour > 0L)
                {
                    return ticksPerHour;
                }
            }

            return VanillaTicksPerHour;
        }
    }
}

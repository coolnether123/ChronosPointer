using System;
using System.Reflection;
using HarmonyLib;

namespace ChronosPointer.ModSupport
{
    internal sealed class DayStretchSupport : ModSupportModuleBase, IChronosTimeScaleSupport
    {
        private const string DayStretchPackageId = "jj.daystretch";
        private const long VanillaTicksPerDay = 60000L;
        private const long VanillaTicksPerHour = 2500L;

        private MethodInfo ticksPerDayLong;
        private MethodInfo ticksPerHourLong;
        private FieldInfo settingsInstance;
        private FieldInfo timeMultiplier;

        public override string PackageId => DayStretchPackageId;
        public override string DisplayName => "DayStretch";

        public override void OnModsDetected()
        {
            Type constantsType = AccessTools.TypeByName("DayStretch.DayConstants");
            if (constantsType != null)
            {
                ticksPerDayLong = AccessTools.Method(constantsType, "TicksPerDayLong");
                ticksPerHourLong = AccessTools.Method(constantsType, "TicksPerHourLong");
            }

            Type settingsType = AccessTools.TypeByName("DayStretch.Settings");
            Type settingsDataType = AccessTools.TypeByName("DayStretch.DayStretch");
            if (settingsType != null && settingsDataType != null)
            {
                settingsInstance = AccessTools.Field(settingsType, "Instance");
                timeMultiplier = AccessTools.Field(settingsDataType, "TimeMultiplier");
            }

            if (ticksPerDayLong == null && settingsInstance == null)
            {
                Warn("DayStretch was active, but its time multiplier API was not found. Chronos will use vanilla day length.");
            }
        }

        public bool TryGetTicksPerDay(out long ticksPerDay)
        {
            return TryGetDayStretchTicks(ticksPerDayLong, VanillaTicksPerDay, out ticksPerDay);
        }

        public bool TryGetTicksPerHour(out long ticksPerHour)
        {
            return TryGetDayStretchTicks(ticksPerHourLong, VanillaTicksPerHour, out ticksPerHour);
        }

        private bool TryGetDayStretchTicks(MethodInfo method, long vanillaTicks, out long ticks)
        {
            ticks = 0L;

            if (method != null && TryInvokeTicksMethod(method, out ticks))
            {
                return true;
            }

            if (TryGetMultiplier(out float multiplier))
            {
                ticks = Math.Max(1L, (long)Math.Round(vanillaTicks * multiplier));
                return true;
            }

            return false;
        }

        private static bool TryInvokeTicksMethod(MethodInfo method, out long ticks)
        {
            ticks = 0L;

            try
            {
                object value = method.Invoke(null, null);
                ticks = Convert.ToInt64(value);
                return ticks > 0L;
            }
            catch
            {
                return false;
            }
        }

        private bool TryGetMultiplier(out float multiplier)
        {
            multiplier = 1f;

            if (settingsInstance == null || timeMultiplier == null)
            {
                return false;
            }

            try
            {
                object settings = settingsInstance.GetValue(null);
                if (settings == null)
                {
                    return false;
                }

                object value = timeMultiplier.GetValue(settings);
                multiplier = Convert.ToSingle(value);
                return multiplier > 0f;
            }
            catch
            {
                return false;
            }
        }
    }
}

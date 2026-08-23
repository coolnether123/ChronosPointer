using System.Collections.Generic;
using ChronosPointer.Api;
using ChronosPointer.ModSupport;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ChronosPointer.Core
{
    internal static class ChronosTimelineService
    {
        private static readonly GameConditionDef SolarFlareDef = DefDatabase<GameConditionDef>.GetNamedSilentFail("SolarFlare");
        private static IReadOnlyList<ChronosHourSegment> cachedDaylightHours;
        private static long cachedTicksPerHour = -1;

        public static bool TryCreateTimeline(Map map, ChronosPointerSettings settings, out ChronosTimelineSnapshot snapshot, bool syncLegacyPatchState = false)
        {
            snapshot = null;
            if (map == null)
            {
                return false;
            }

            ChronosSettingsSnapshot settingsSnapshot = ChronosSettingsSnapshotFactory.Create(settings);
            ChronosIncidentState incidents = CreateIncidentState(map, syncLegacyPatchState);
            float dayPercent = GenLocalDate.DayPercent(map);
            float localHour = dayPercent * 24f;
            int currentHour = Mathf.Clamp((int)localHour, 0, 23);
            long absoluteTick = GenTicks.TicksAbs;
            Season season = GenLocalDate.Season(map);
            long ticksPerDay = ModSupportManager.GetTicksPerDay();
            long ticksPerHour = ModSupportManager.GetTicksPerHour();

            long startOfCurrentLocalDayAbsTick = GetStartOfCurrentLocalDayTick(absoluteTick, dayPercent, ticksPerDay);
            bool useCachedDaylight = !incidents.Any &&
                Patch_ScheduleWindow.dayNightColorsCalculated &&
                Patch_ScheduleWindow._cachedMap == map &&
                Patch_ScheduleWindow._cachedSeason == season &&
                cachedTicksPerHour == ticksPerHour &&
                cachedDaylightHours != null;
            IReadOnlyList<ChronosHourSegment> hours = cachedDaylightHours;
            if (!useCachedDaylight)
            {
                var rebuiltHours = new List<ChronosHourSegment>(24);
                for (int hour = 0; hour < 24; hour++)
                {
                    long absTickForHour = startOfCurrentLocalDayAbsTick + (long)hour * ticksPerHour;
                    float sunlight = GenCelestial.CelestialSunGlow(map.Tile, (int)absTickForHour);
                    ChronosLightBand band = GetLightBand(sunlight, settingsSnapshot);
                    Color baseColor = GetBaseColorForSunlight(sunlight, settingsSnapshot, incidents);
                    Color overlayColor = GetIncidentOverlayColor(hour, settingsSnapshot, incidents);
                    rebuiltHours.Add(new ChronosHourSegment(hour, sunlight, band, baseColor, overlayColor));
                }

                hours = rebuiltHours;
                if (!incidents.Any)
                {
                    cachedDaylightHours = rebuiltHours;
                    cachedTicksPerHour = ticksPerHour;
                    Patch_ScheduleWindow._cachedMap = map;
                    Patch_ScheduleWindow._cachedSeason = season;
                    Patch_ScheduleWindow.dayNightColorsCalculated = true;
                }
            }

            snapshot = new ChronosTimelineSnapshot(
                map,
                map.Tile,
                absoluteTick,
                dayPercent,
                localHour,
                currentHour,
                season,
                incidents,
                hours,
                settingsSnapshot);

            return true;
        }

        public static ChronosIncidentState CreateIncidentState(Map map, bool syncLegacyPatchState = false)
        {
            if (map == null)
            {
                return new ChronosIncidentState(false, false, false, false, false);
            }

            bool aurora = Patch_ScheduleWindow.IsInTestMode
                ? Patch_ScheduleWindow.overrideIsAurora
                : map.gameConditionManager.ConditionIsActive(GameConditionDefOf.Aurora) || Patch_ScheduleWindow.overrideIsAurora;

            bool eclipse = Patch_ScheduleWindow.IsInTestMode
                ? Patch_ScheduleWindow.overrideIsEclipse
                : map.gameConditionManager.ConditionIsActive(GameConditionDefOf.Eclipse) || Patch_ScheduleWindow.overrideIsEclipse;

            bool solarFlare = Patch_ScheduleWindow.IsInTestMode
                ? Patch_ScheduleWindow.overrideIsSolarFlare
                : (SolarFlareDef != null && map.gameConditionManager.ConditionIsActive(SolarFlareDef)) || Patch_ScheduleWindow.overrideIsSolarFlare;

            bool toxicFallout = Patch_ScheduleWindow.IsInTestMode
                ? Patch_ScheduleWindow.overrideIsToxicFallout
                : map.gameConditionManager.ConditionIsActive(GameConditionDefOf.ToxicFallout) || Patch_ScheduleWindow.overrideIsToxicFallout;

            bool volcanicWinter = Patch_ScheduleWindow.IsInTestMode
                ? Patch_ScheduleWindow.overrideIsVolcanicWinter
                : map.gameConditionManager.ConditionIsActive(GameConditionDefOf.VolcanicWinter) || Patch_ScheduleWindow.overrideIsVolcanicWinter;

            if (syncLegacyPatchState)
            {
                Patch_ScheduleWindow.isAurora = aurora;
                Patch_ScheduleWindow.isEclipse = eclipse;
                Patch_ScheduleWindow.isSolarFlare = solarFlare;
                Patch_ScheduleWindow.isToxicFallout = toxicFallout;
                Patch_ScheduleWindow.isVolcanicWinter = volcanicWinter;

                if (eclipse || solarFlare)
                {
                    Patch_ScheduleWindow.dayNightColorsCalculated = false;
                }
            }

            return new ChronosIncidentState(solarFlare, eclipse, toxicFallout, volcanicWinter, aurora);
        }

        public static Color GetCurrentHoursBarCursorColor(Map map, ChronosTimelineSnapshot timeline)
        {
            if (map == null || timeline == null)
            {
                return Color.white;
            }

            ChronosSettingsSnapshot settings = timeline.Settings;
            if (!settings.DoDynamicHoursBarLine)
            {
                return settings.ColorHoursBarCursorDay;
            }

            float sunlight = GenCelestial.CelestialSunGlow(map.Tile, GenTicks.TicksAbs);
            return sunlight >= settings.SunlightThresholdSunriseSunset
                ? settings.ColorHoursBarCursorDay
                : settings.ColorHoursBarCursorNight;
        }

        private static long GetStartOfCurrentLocalDayTick(long absoluteTick, float dayPercent, long ticksPerDay)
        {
            long ticksIntoLocalDay = (long)(dayPercent * ticksPerDay);
            return absoluteTick - ticksIntoLocalDay;
        }

        private static ChronosLightBand GetLightBand(float sunlight, ChronosSettingsSnapshot settings)
        {
            if (sunlight <= settings.SunlightThresholdNight)
            {
                return ChronosLightBand.Night;
            }

            if (sunlight < settings.SunlightThresholdDawnDusk)
            {
                return ChronosLightBand.DawnDusk;
            }

            if (sunlight < settings.SunlightThresholdSunriseSunset)
            {
                return ChronosLightBand.SunriseSunset;
            }

            return ChronosLightBand.Day;
        }

        private static Color GetBaseColorForSunlight(float sunlight, ChronosSettingsSnapshot settings, ChronosIncidentState incidents)
        {
            if (settings.DrawIncidentOverlay)
            {
                if (incidents.Eclipse && sunlight > settings.SunlightThresholdNight)
                {
                    return settings.ColorDawnDusk;
                }

                if (incidents.SolarFlare &&
                    sunlight > settings.SunlightThresholdAny &&
                    sunlight >= settings.SunlightThresholdDawnDusk)
                {
                    return settings.ColorDay;
                }
            }

            switch (GetLightBand(sunlight, settings))
            {
                case ChronosLightBand.Night:
                    return settings.ColorNight;
                case ChronosLightBand.DawnDusk:
                    return settings.ColorDawnDusk;
                case ChronosLightBand.SunriseSunset:
                    return settings.ColorSunriseSunset;
                default:
                    return settings.ColorDay;
            }
        }

        private static Color GetIncidentOverlayColor(int hour, ChronosSettingsSnapshot settings, ChronosIncidentState incidents)
        {
            if (!settings.DrawIncidentOverlay)
            {
                return settings.DefaultTransparentColor;
            }

            Color hourColor = settings.DefaultTransparentColor;

            if (incidents.ToxicFallout)
            {
                hourColor = ChronosColorUtility.Mix(hourColor, settings.ColorToxicFallout, 0.5f);
            }

            if (incidents.VolcanicWinter)
            {
                hourColor = IsDefaultTransparent(hourColor, settings.DefaultTransparentColor)
                    ? settings.ColorVolcanicWinter
                    : ChronosColorUtility.Mix(hourColor, settings.ColorVolcanicWinter, 0.5f, true);
            }

            if (incidents.Aurora)
            {
                float hourOffset = (float)hour / 24f;
                float loopingTime = 0.5f + 0.5f * Mathf.Cos(Time.time + hourOffset);
                Color auroraColor = ChronosColorUtility.Mix(settings.ColorAurora1, settings.ColorAurora2, loopingTime, true);
#if V1_6U
                auroraColor.a = Mathf.PerlinNoise1D(loopingTime).Remap(0f, 1f, settings.AuroraMinOpacity, settings.AuroraMaxOpacity);
#elif V1_5
                auroraColor.a = Mathf.PerlinNoise(loopingTime, loopingTime).Remap(0f, 1f, settings.AuroraMinOpacity, settings.AuroraMaxOpacity);
#elif V1_4D
                auroraColor.a = settings.AuroraMinOpacity + (settings.AuroraMaxOpacity - settings.AuroraMinOpacity) * Mathf.PerlinNoise(loopingTime, loopingTime);
#endif
                hourColor = ChronosColorUtility.Mix(hourColor, auroraColor, 0.5f);
            }

            return hourColor;
        }

        private static bool IsDefaultTransparent(Color value, Color defaultTransparent)
        {
            return value.r == defaultTransparent.r &&
                   value.g == defaultTransparent.g &&
                   value.b == defaultTransparent.b;
        }
    }
}

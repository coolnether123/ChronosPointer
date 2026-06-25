using System;
using System.Reflection;
using ChronosPointer.Api;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ChronosPointer.ModSupport
{
    internal sealed class ClockworkSupport : ModSupportModuleBase
    {
        private const string ClockworkPackageId = "Jaskkro.WorkShift";
        private const string CompatibilityHarmonyId = "CoolNether123.ChronosPointer.clockworkcompat";
        private const float ClockworkHourCellWidth = 28f;
        private const float ClockworkHourCellGap = 2f;
        private const float EmbeddedBarOffsetY = 18f;
        private const float EmbeddedHourBarHeight = 10f;

        private static readonly Harmony CompatibilityHarmony = new Harmony(CompatibilityHarmonyId);
        private static bool patched;

        public override string PackageId => ClockworkPackageId;
        public override string DisplayName => "Clockwork - Precise Priorities";

        public override void OnModsDetected()
        {
            if (patched)
            {
                return;
            }

            Type drawerType = AccessTools.TypeByName("WorkShift.UI.ScheduleGridDrawer");
            if (drawerType == null)
            {
                Warn("ScheduleGridDrawer was not found. Clockwork hour-header replacement was skipped.");
                return;
            }

            bool patchedAnyHeader = PatchHeader(drawerType, "DrawHourHeaders", new[] { typeof(Rect), typeof(Pawn) });
            patchedAnyHeader |= PatchHeader(drawerType, "DrawHourHeadersWorkType", new[] { typeof(Rect) });
            PatchCurrentHourCellOverlay(drawerType);

            patched = true;
            if (!patchedAnyHeader)
            {
                Warn("Clockwork was active, but no compatible hour-header methods were found.");
            }
        }

        private static bool PatchHeader(Type drawerType, string methodName, Type[] argumentTypes)
        {
            MethodInfo method = AccessTools.Method(drawerType, methodName, argumentTypes);
            if (method == null)
            {
                return false;
            }

            CompatibilityHarmony.Patch(method, prefix: new HarmonyMethod(typeof(ClockworkSupport), nameof(ReplaceClockworkHourHeader)));
            return true;
        }

        private static void PatchCurrentHourCellOverlay(Type drawerType)
        {
            MethodInfo method = AccessTools.Method(
                drawerType,
                "DrawHourlyCellBackground",
                new[] { typeof(Rect), typeof(int), typeof(int), typeof(bool) });

            if (method != null)
            {
                CompatibilityHarmony.Patch(method, prefix: new HarmonyMethod(typeof(ClockworkSupport), nameof(SuppressClockworkCurrentHourCellOverlay)));
            }
        }

        private static bool ReplaceClockworkHourHeader(object[] __args)
        {
            if (__args == null || __args.Length == 0 || !(__args[0] is Rect rect))
            {
                return true;
            }

            return !TryDrawEmbeddedChronosHeader(rect);
        }

        private static void SuppressClockworkCurrentHourCellOverlay(ref bool isCurrentHour)
        {
            isCurrentHour = false;
        }

        private static bool TryDrawEmbeddedChronosHeader(Rect rect)
        {
            Map map = ChronosPointer.RimWorld.ChronosRimWorldCompat.CurrentMap();
            if (map == null || rect.width <= 1f || rect.height <= 1f)
            {
                return false;
            }

            ChronosScheduleGeometrySnapshot geometry = ChronosPointerApi.CreateGeometry(
                rect,
                windowHeight: 0f,
                hourBoxWidth: ClockworkHourCellWidth,
                baseOffsetX: 0f,
                baseOffsetY: EmbeddedBarOffsetY,
                hourBoxGap: ClockworkHourCellGap,
                hourBoxHeight: EmbeddedHourBarHeight,
                pawnAreaTopOffset: 0f,
                headerHeight: 0f);

            bool drawn = ChronosPointerApi.TryDrawEmbeddedTimeline(map, geometry, drawIncidentOverlay: true);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            return drawn;
        }
    }
}

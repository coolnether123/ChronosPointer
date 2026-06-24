using System;
using ChronosPointer.Core;
using ChronosPointer.Rendering;
using UnityEngine;
using Verse;

namespace ChronosPointer.Api
{
    public static class ChronosPointerApi
    {
        public const string PackageId = "CoolNether123.ChronosPointer";
        public const string AssemblyName = "ChronosPointer";
        public const int ContractMajor = 1;
        public const int ContractMinor = 0;

        private static ChronosTimelineSnapshot latestTimeline;
        private static ChronosScheduleGeometrySnapshot latestGeometry;

        public static event Action Ready;
        public static event Action<ChronosSettingsSnapshot> SettingsChanged;
        public static event Action<ChronosScheduleGeometrySnapshot> ScheduleLayoutReady;

        public static bool IsReady => ChronosPointerMod.Settings != null;

        public static bool Supports(int major, int minor)
        {
            if (major != ContractMajor)
            {
                return false;
            }

            return ContractMinor >= minor;
        }

        public static bool TryGetTimelineSnapshot(Map map, out ChronosTimelineSnapshot snapshot)
        {
            bool created = ChronosTimelineService.TryCreateTimeline(map, ChronosPointerMod.Settings, out snapshot);
            if (created)
            {
                latestTimeline = snapshot;
            }

            return created;
        }

        public static ChronosTimelineSnapshot GetLatestTimelineSnapshot()
        {
            return latestTimeline;
        }

        public static bool TryGetCurrentScheduleGeometry(out ChronosScheduleGeometrySnapshot geometry)
        {
            geometry = latestGeometry;
            return geometry != null && geometry.IsValid;
        }

        public static IDisposable RegisterOverlay(ChronosOverlayRegistration registration)
        {
            return ChronosOverlayRegistry.Register(registration);
        }

        public static ChronosScheduleGeometrySnapshot CreateGeometry(
            Rect fillRect,
            float windowHeight,
            float hourBoxWidth = ChronosScheduleGeometryDefaults.HourBoxWidth,
            float baseOffsetX = ChronosScheduleGeometryDefaults.BaseOffsetX,
            float baseOffsetY = ChronosScheduleGeometryDefaults.BaseOffsetY,
            float hourBoxGap = ChronosScheduleGeometryDefaults.HourBoxGap,
            float hourBoxHeight = ChronosScheduleGeometryDefaults.HourBoxHeight,
            float pawnAreaTopOffset = ChronosScheduleGeometryDefaults.PawnAreaTopOffset,
            float headerHeight = 0f)
        {
            return new ChronosScheduleGeometrySnapshot(
                fillRect,
                baseOffsetX,
                baseOffsetY,
                Mathf.Max(hourBoxWidth, 0f),
                Mathf.Max(hourBoxGap, 0f),
                Mathf.Max(hourBoxHeight, 0f),
                Mathf.Max(pawnAreaTopOffset, 0f),
                Mathf.Max(windowHeight, 0f),
                Mathf.Max(headerHeight, 0f),
                Time.frameCount,
                hourBoxWidth > 0f);
        }

        public static void DrawTimeline(ChronosTimelineSnapshot timeline, ChronosScheduleGeometrySnapshot geometry, bool drawRegularBar = true, bool drawIncidentOverlay = true)
        {
            ChronosScheduleRenderer.DrawSchedule(timeline, geometry, drawRegularBar, drawIncidentOverlay);
        }

        public static void DrawTimeline(ChronosTimelineSnapshot timeline, ChronosScheduleGeometrySnapshot geometry, ChronosDrawOptions options)
        {
            ChronosScheduleRenderer.DrawSchedule(timeline, geometry, options);
        }

        public static void DrawEmbeddedTimeline(ChronosTimelineSnapshot timeline, ChronosScheduleGeometrySnapshot geometry, bool drawIncidentOverlay = true)
        {
            ChronosDrawOptions options = ChronosDrawOptions.EmbeddedTimeline();
            options.DrawIncidentOverlay = drawIncidentOverlay;
            ChronosScheduleRenderer.DrawSchedule(timeline, geometry, options);
        }

        public static bool TryDrawTimeline(Map map, ChronosScheduleGeometrySnapshot geometry, bool drawRegularBar = true, bool drawIncidentOverlay = true)
        {
            if (!TryGetTimelineSnapshot(map, out ChronosTimelineSnapshot timeline))
            {
                return false;
            }

            DrawTimeline(timeline, geometry, drawRegularBar, drawIncidentOverlay);
            return true;
        }

        public static bool TryDrawEmbeddedTimeline(Map map, ChronosScheduleGeometrySnapshot geometry, bool drawIncidentOverlay = true)
        {
            if (!TryGetTimelineSnapshot(map, out ChronosTimelineSnapshot timeline))
            {
                return false;
            }

            DrawEmbeddedTimeline(timeline, geometry, drawIncidentOverlay);
            return true;
        }

        internal static void NotifyReady()
        {
            Ready?.Invoke();
            NotifySettingsChanged();
        }

        internal static void NotifySettingsChanged()
        {
            SettingsChanged?.Invoke(ChronosSettingsSnapshotFactory.Create(ChronosPointerMod.Settings));
        }

        internal static void SetLatestTimeline(ChronosTimelineSnapshot timeline)
        {
            latestTimeline = timeline;
        }

        internal static void SetLatestGeometry(ChronosScheduleGeometrySnapshot geometry)
        {
            latestGeometry = geometry;
            if (geometry != null && geometry.IsValid)
            {
                ScheduleLayoutReady?.Invoke(geometry);
            }
        }

        internal static ChronosOverlayContext CreateOverlayContext(ChronosTimelineSnapshot timeline, ChronosScheduleGeometrySnapshot geometry)
        {
            return new ChronosOverlayContext(timeline, geometry, Event.current);
        }
    }
}

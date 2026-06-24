using System;
using System.Collections.Generic;
using ChronosPointer.Api;
using Verse;

namespace ChronosPointer.Core
{
    internal static class ChronosOverlayRegistry
    {
        private static readonly List<OverlayEntry> Registrations = new List<OverlayEntry>();

        public static IDisposable Register(ChronosOverlayRegistration registration)
        {
            if (registration == null || registration.Draw == null)
            {
                return DisposableAction.Empty;
            }

            OverlayEntry entry = new OverlayEntry(registration);
            Registrations.Add(entry);
            Sort();
            return new DisposableAction(() => Registrations.Remove(entry));
        }

        public static void DrawLayer(ChronosOverlayLayer layer, ChronosOverlayContext context)
        {
            if (context == null || Registrations.Count == 0)
            {
                return;
            }

            for (int i = 0; i < Registrations.Count; i++)
            {
                OverlayEntry entry = Registrations[i];
                if (entry.Registration.Layer != layer)
                {
                    continue;
                }

                try
                {
                    entry.Registration.Draw(context);
                }
                catch (Exception ex)
                {
                    Log.Error($"[ChronosPointer] Overlay from '{entry.Registration.OwnerPackageId ?? "unknown"}' failed: {ex}");
                }
            }
        }

        private static void Sort()
        {
            Registrations.Sort((left, right) =>
            {
                int layerCompare = left.Registration.Layer.CompareTo(right.Registration.Layer);
                return layerCompare != 0
                    ? layerCompare
                    : left.Registration.SortOrder.CompareTo(right.Registration.SortOrder);
            });
        }

        private sealed class OverlayEntry
        {
            public OverlayEntry(ChronosOverlayRegistration registration)
            {
                Registration = registration;
            }

            public ChronosOverlayRegistration Registration { get; private set; }
        }

        private sealed class DisposableAction : IDisposable
        {
            public static readonly IDisposable Empty = new DisposableAction(null);

            private Action disposeAction;

            public DisposableAction(Action disposeAction)
            {
                this.disposeAction = disposeAction;
            }

            public void Dispose()
            {
                Action action = disposeAction;
                disposeAction = null;
                action?.Invoke();
            }
        }
    }
}

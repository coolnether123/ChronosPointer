using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ChronosPointer.RimWorld
{
    internal static class ChronosRimWorldCompat
    {
#if V0_16U || V0_15U || V0_14U || V0_13U
        public static Window GetScheduleTabWindow()
        {
            return null;
        }

        public static float CelestialSunGlow(Map map, int ticksAbs)
        {
#if V0_16U
            return map != null ? GenCelestial.CurCelestialSunGlow(map) : 0f;
#else
            return GenCelestial.CurCelestialSunGlow();
#endif
        }

        public static Map CurrentMap()
        {
#if V0_16U
            return Find.VisibleMap;
#else
            return Find.Map;
#endif
        }

        public static int TicksAbs()
        {
            return GenTicks.TicksAbs;
        }

        public static float DayPercent(Map map)
        {
#if V0_16U
            return map != null ? GenLocalDate.DayPercent(map) : 0f;
#else
            return GenDate.CurrentDayPercent;
#endif
        }

        public static Season Season(Map map)
        {
#if V0_16U
            return map != null ? GenLocalDate.Season(map) : (Season)0;
#else
            return GenDate.CurrentSeason;
#endif
        }

        public static int MapTile(Map map)
        {
#if V0_16U
            return map != null ? map.Tile : -1;
#else
            return -1;
#endif
        }

        public static float SliderLabeled(Listing_Standard listing, string label, float value, float min, float max, string tooltip = null)
        {
            return value;
        }

        public static bool IsModActive(string packageId)
        {
            return false;
        }

        public static void DrawBoxSolid(Rect rect, Color color)
        {
#if V0_13U
            Color oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = oldColor;
#else
            Widgets.DrawBoxSolid(rect, color);
#endif
        }

        public static void DrawBoxSolidWithOutline(Rect rect, Color interiorColor, Color outlineColor, int thickness)
        {
            DrawBoxSolid(rect, interiorColor);
            Color oldColor = GUI.color;
            GUI.color = outlineColor;
            Widgets.DrawBox(rect, thickness);
            GUI.color = oldColor;
        }
#else
        public static Window GetScheduleTabWindow()
        {
            IEnumerable<MainButtonDef> buttons = GetMainButtons();
            if (buttons == null)
            {
                return null;
            }

            foreach (MainButtonDef button in buttons)
            {
                if (button == null || button.TabWindow == null)
                {
                    continue;
                }

#if V1_3U
                if (button.TabWindow is MainTabWindow_Schedule)
#else
                if (button.TabWindow is MainTabWindow_Restrict)
#endif
                {
                    return button.TabWindow;
                }
            }

            return null;
        }

        private static IEnumerable<MainButtonDef> GetMainButtons()
        {
            if (Find.MainButtonsRoot == null)
            {
                return null;
            }

#if V1_2U
            return Find.MainButtonsRoot.allButtonsInOrder;
#else
            object root = Find.MainButtonsRoot;
            object value = AccessTools.Field(root.GetType(), "allButtonsInOrder")?.GetValue(root)
                ?? AccessTools.Field(root.GetType(), "allButtons")?.GetValue(root)
                ?? AccessTools.Field(root.GetType(), "buttons")?.GetValue(root);

            if (value is IEnumerable<MainButtonDef> typedButtons)
            {
                return typedButtons;
            }

            if (value is IEnumerable enumerable)
            {
                List<MainButtonDef> buttons = new List<MainButtonDef>();
                foreach (object item in enumerable)
                {
                    if (item is MainButtonDef button)
                    {
                        buttons.Add(button);
                    }
                }

                return buttons;
            }

            return null;
#endif
        }

        public static float CelestialSunGlow(Map map, int ticksAbs)
        {
#if V1_3U
            return GenCelestial.CelestialSunGlow(map.Tile, ticksAbs);
#elif V0_18U || V0_19U || V1_0U || V1_1U || V1_2U
            return GenCelestial.CelestialSunGlow(map, ticksAbs);
#else
            return GenCelestial.CurCelestialSunGlow(map);
#endif
        }

        public static int TicksAbs()
        {
            return GenTicks.TicksAbs;
        }

        public static float DayPercent(Map map)
        {
            return GenLocalDate.DayPercent(map);
        }

        public static Season Season(Map map)
        {
            return GenLocalDate.Season(map);
        }

        public static int MapTile(Map map)
        {
            if (map == null)
            {
                return -1;
            }

            return map.Tile;
        }

        public static Map CurrentMap()
        {
#if V0_19U || V1_0U || V1_1U || V1_2U || V1_3U || V1_4U || V1_5U || V1_6U
            return Find.CurrentMap;
#else
            return Find.VisibleMap;
#endif
        }

        public static float SliderLabeled(Listing_Standard listing, string label, float value, float min, float max, string tooltip = null)
        {
            Rect rect = listing.GetRect(24f);
            Widgets.Label(rect.LeftHalf(), label);
            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }

            return Widgets.HorizontalSlider(rect.RightHalf(), value, min, max);
        }

        public static bool IsModActive(string packageId)
        {
            if (string.IsNullOrEmpty(packageId))
            {
                return false;
            }

#if V1_1U
            return ModsConfig.IsActive(packageId);
#else
#if V0_18U || V0_19U || V1_0U
            IEnumerable runningMods = LoadedModManager.RunningModsListForReading;
#else
            IEnumerable runningMods = LoadedModManager.RunningMods;
#endif
            foreach (object mod in runningMods)
            {
                object metadata = mod;
                PropertyInfo metadataProperty = mod.GetType().GetProperty("Meta") ?? mod.GetType().GetProperty("meta");
                if (metadataProperty != null)
                {
                    metadata = metadataProperty.GetValue(mod, null);
                }

                if (metadata == null)
                {
                    continue;
                }

                string id = GetStringMember(metadata, "PackageId")
                    ?? GetStringMember(metadata, "packageId")
                    ?? GetStringMember(metadata, "Identifier")
                    ?? GetStringMember(metadata, "identifier")
                    ?? GetStringMember(metadata, "Name")
                    ?? GetStringMember(metadata, "name");

                if (string.Equals(id, packageId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
#endif
        }

#if !V1_1U
        private static string GetStringMember(object instance, string name)
        {
            PropertyInfo property = instance.GetType().GetProperty(name);
            if (property != null)
            {
                return property.GetValue(instance, null) as string;
            }

            FieldInfo field = instance.GetType().GetField(name);
            return field != null ? field.GetValue(instance) as string : null;
        }
#endif

        public static void DrawBoxSolidWithOutline(Rect rect, Color interiorColor, Color outlineColor, int thickness)
        {
#if V1_3U
            Widgets.DrawBoxSolidWithOutline(rect, interiorColor, outlineColor, thickness);
#else
            Widgets.DrawBoxSolid(rect, interiorColor);
            Color oldColor = GUI.color;
            GUI.color = outlineColor;
            Widgets.DrawBox(rect, thickness);
            GUI.color = oldColor;
#endif
        }

        public static void DrawBoxSolid(Rect rect, Color color)
        {
            Widgets.DrawBoxSolid(rect, color);
        }
#endif
    }
}

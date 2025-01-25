using UnityEngine;
using Verse;
using RimWorld;
using ColourPicker;
using UnityEngine.UI;
using System;

namespace ChronosPointer
{
    /// <summary>
    /// Mod entry point. Loads settings, applies Harmony patches, and provides
    /// the in-game settings window for Chronos Pointer.
    /// </summary>
    public class ChronosPointerMod : Mod
    {
        public static ChronosPointerSettings Settings;

        // Example: thickness default
        public static float cursorThickness = 2f;

        // For scrolling in settings UI
        private Vector2 scrollPosition = Vector2.zero;

        public ChronosPointerMod(ModContentPack content) : base(content)
        {
            // Load settings
            Settings = GetSettings<ChronosPointerSettings>();

            // Apply Harmony patches
            var harmony = new HarmonyLib.Harmony("com.coolnether123.ChronosPointer");
            harmony.PatchAll();
            Log.Message("[ChronosPointer] Harmony patches applied successfully.");
        }

        public override string SettingsCategory()
        {
            return "Chronos Pointer";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            float height = 1000f;
            Rect viewRect = new Rect(0f, 40f, inRect.width - 16f, height);

            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(viewRect);

            if (listingStandard.ButtonText("Reset to Default"))
            {
                Settings.ResetToDefaults();
            }
            listingStandard.Gap(20f);

            listingStandard.CheckboxLabeled("- Show Arrow", ref Settings.enableArrow);
            listingStandard.CheckboxLabeled("- Show Day/Night Bar", ref Settings.showDayNightBar);

            // Gray out sub-options if day/night bar is disabled
            if (!Settings.showDayNightBar) GUI.color = Color.gray;
            listingStandard.CheckboxLabeled("    Show Day/Night Bar Time Indicator", ref Settings.showDayNightIndicator);
            GUI.color = Color.white;

            if (!Settings.showDayNightIndicator || !Settings.showDayNightBar) GUI.color = Color.gray;
            listingStandard.CheckboxLabeled("    Dynamic Day/Night Bar Cursor Color", ref Settings.useDynamicTimeTraceLine);
            GUI.color = Color.white;

            listingStandard.CheckboxLabeled("- Show Pawn Section Time Indicator", ref Settings.showPawnLine);
            listingStandard.CheckboxLabeled("- Show Current Hour Highlight", ref Settings.showHighlight);

            if (!Settings.showHighlight) GUI.color = Color.gray;
            listingStandard.CheckboxLabeled("- Hollow Current Hour Highlight", ref Settings.hollowHourHighlight);
            GUI.color = Color.white;

            listingStandard.Gap(25);

            // If the user wants the pawn line, let them configure thickness
            if (Settings.showPawnLine)
            {
                listingStandard.Label($"Pawn Section Time Indicator Thickness: {Settings.cursorThickness:F1}");
                float newThickness = listingStandard.Slider(Settings.cursorThickness, 2f, 10f);
                // Round to nearest even
                Settings.cursorThickness = Mathf.Round(newThickness / 2f) * 2f;
            }

            // Arrow color picking (grey out if arrow is not enabled)
            if (!Settings.enableArrow) GUI.color = Color.gray;

            // Show arrow color
            Rect arrowColorRect = listingStandard.GetRect(10f);
            Widgets.DrawBoxSolid(arrowColorRect, Settings.arrowColor);
            if (listingStandard.ButtonText("Change arrow color"))
            {
                Find.WindowStack.Add(new Dialog_ColourPicker(Settings.arrowColor, newColor =>
                {
                    Settings.arrowColor = newColor;
                }));
            }
            listingStandard.Gap(10f);
            GUI.color = Color.white;

            // Day/Night bar colors
            if (!Settings.showDayNightBar) GUI.color = Color.gray;
            if (!Settings.showDayNightIndicator) GUI.color = Color.gray;

            // Day (or single) color
            Rect dayColorRect = listingStandard.GetRect(10f);
            Widgets.DrawBoxSolid(dayColorRect, Settings.timeTraceColorDay);
            if (listingStandard.ButtonText("Change Time Trace Color" + (Settings.useDynamicTimeTraceLine ? " Day" : "")))
            {
                Find.WindowStack.Add(new Dialog_ColourPicker(Settings.timeTraceColorDay, newColor =>
                {
                    Settings.timeTraceColorDay = newColor;
                }));
            }
            listingStandard.Gap(10f);

            // Night color (only relevant if dynamic line is on)
            if (!Settings.useDynamicTimeTraceLine) GUI.color = Color.gray;
            Rect nightColorRect = listingStandard.GetRect(10f);
            Widgets.DrawBoxSolid(nightColorRect, Settings.timeTraceColorNight);
            if (listingStandard.ButtonText("Change Time Trace Color Night"))
            {
                Find.WindowStack.Add(new Dialog_ColourPicker(Settings.timeTraceColorNight, newColor =>
                {
                    Settings.timeTraceColorNight = newColor;
                }));
            }
            listingStandard.Gap(10f);
            GUI.color = Color.white;

            // Pawn line color
            if (!Settings.showPawnLine) GUI.color = Color.gray;
            Rect pawnLineRect = listingStandard.GetRect(10f);
            Widgets.DrawBoxSolid(pawnLineRect, Settings.bottomCursorColor);
            if (listingStandard.ButtonText("Change pawn section time indicator color"))
            {
                Find.WindowStack.Add(new Dialog_ColourPicker(Settings.bottomCursorColor, newColor =>
                {
                    Settings.bottomCursorColor = newColor;
                }));
            }
            listingStandard.Gap(10f);
            GUI.color = Color.white;

            // Current hour highlight color
            if (!Settings.showHighlight) GUI.color = Color.gray;
            Rect highlightRect = listingStandard.GetRect(10f);
            Widgets.DrawBoxSolid(highlightRect, Settings.highlightColor);
            if (listingStandard.ButtonText("Change current-hour color"))
            {
                Find.WindowStack.Add(new Dialog_ColourPicker(Settings.highlightColor, newColor =>
                {
                    Settings.highlightColor = newColor;
                }));
            }
            listingStandard.Gap(10f);
            GUI.color = Color.white;

            listingStandard.End();
        }
    }
}

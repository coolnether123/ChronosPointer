using UnityEngine;
using Verse;
using RimWorld;
using ColourPicker;
using UnityEngine.UI;
using System;

namespace ChronosPointer
{
    public class ChronosPointerMod : Mod
    {
        public static ChronosPointerSettings Settings;
        public static float cursorThickness = 2f; // Default thickness
        private Vector2 scrollPosition = Vector2.zero;

        public ChronosPointerMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<ChronosPointerSettings>();
            // Harmony patch
            var harmony = new HarmonyLib.Harmony("com.coolnether123.ChronosPointer");
            harmony.PatchAll();
            //Log.Message("ChronosPointer: Harmony patches applied via ChronosPointerMod constructor.");
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

            // Gray out unapplicable settings
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

            // Add a slider for cursor thickness
            if (Settings.showPawnLine)
            {
                listingStandard.Label($"Pawn Section Time Indicator Thickness: {Settings.cursorThickness:F1}");
                float newThickness = listingStandard.Slider(Settings.cursorThickness, 2f, 10f);
                Settings.cursorThickness = Mathf.Round(newThickness / 2f) * 2f; // Round to nearest even number
            }

            // If the setting isn't being used, gray the color picker out so it's obvious it's not being used.
            if (!Settings.enableArrow)
            {
                GUI.color = Color.gray;
            }

            // Arrow Color
            Widgets.DrawBoxSolid(listingStandard.GetRect(10), Settings.arrowColor);
            if (listingStandard.ButtonText("Change arrow color", "Tag"))
                Find.WindowStack.Add(new Dialog_ColourPicker(Settings.arrowColor, newColor =>
                {
                    Settings.arrowColor = newColor;
                }));
            listingStandard.Gap(10f);

            // Reset GUI color after to make sure the next color is the right color
            GUI.color = Color.white;

            // Day/Night bar day and night colors
            if (!Settings.showDayNightBar)
            {
                GUI.color = Color.gray;
            }
            if (!Settings.showDayNightIndicator)
            {
                GUI.color = Color.gray;
            }

            // Day/Default
            Widgets.DrawBoxSolid(listingStandard.GetRect(10), Settings.timeTraceColorDay);
            if (listingStandard.ButtonText("Change Time Trace Color" + (Settings.useDynamicTimeTraceLine ? " Day" : "")))
                Find.WindowStack.Add(new Dialog_ColourPicker(Settings.timeTraceColorDay, newColor =>
                {
                    Settings.timeTraceColorDay = newColor;
                }));

            if (!Settings.useDynamicTimeTraceLine)
            {
                GUI.color = Color.gray;
            }
            if (!Settings.useDynamicTimeTraceLine) GUI.color = Color.gray;

            // Night
            listingStandard.Gap(10f); Widgets.DrawBoxSolid(listingStandard.GetRect(10), Settings.timeTraceColorNight);
            if (listingStandard.ButtonText("Change Time Trace Color Night"))
                Find.WindowStack.Add(new Dialog_ColourPicker(Settings.timeTraceColorNight, newColor =>
                {
                    Settings.timeTraceColorNight = newColor;
                }));
            listingStandard.Gap(10f);

            GUI.color = Color.white;
            // Pawn cursor
            if (!Settings.showPawnLine)
            {
                GUI.color = Color.gray;
            }
            Widgets.DrawBoxSolid(listingStandard.GetRect(10), Settings.bottomCursorColor);
            if (listingStandard.ButtonText("Change pawn section time indicator color"))
                Find.WindowStack.Add(new Dialog_ColourPicker(Settings.bottomCursorColor, newColor =>
                {
                    Settings.bottomCursorColor = newColor;
                }));
            listingStandard.Gap(10f);
            GUI.color = Color.white;

            if (!Settings.showHighlight)
            {
                GUI.color = Color.gray;
            }

            // Current hour highlight
            Widgets.DrawBoxSolid(listingStandard.GetRect(10), Settings.highlightColor);
            if (listingStandard.ButtonText("Change current-hour color"))
                Find.WindowStack.Add(new Dialog_ColourPicker(Settings.highlightColor, newColor =>
                {
                    Settings.highlightColor = newColor;
                }));
            listingStandard.Gap(10f);

            GUI.color = Color.white;

            listingStandard.End();
        }
    }
}
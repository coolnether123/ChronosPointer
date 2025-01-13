using UnityEngine;
using Verse;
using RimWorld;

namespace ChronosPointer
{
    public class ChronosPointerMod : Mod
    {
        public static ChronosPointerSettings Settings;
        private Vector2 scrollPosition = Vector2.zero;

        public ChronosPointerMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<ChronosPointerSettings>();

            // Harmony patch
            var harmony = new HarmonyLib.Harmony("com.coolnether123.ChronosPointer");
            harmony.PatchAll();
            Log.Message("ChronosPointer: Harmony patches applied via ChronosPointerMod constructor.");
        }

        public override string SettingsCategory()
        {
            return "Chronos Cursor";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            float height = 1000f;
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, height);
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);

            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(viewRect);

            if (listingStandard.ButtonText("Reset to Default"))
            {
                Settings.ResetToDefaults();
            }
            listingStandard.Gap(20f);

            listingStandard.CheckboxLabeled("Enable Arrow", ref Settings.enableArrow);
            listingStandard.CheckboxLabeled("Show Highlight", ref Settings.showHighlight);
            listingStandard.CheckboxLabeled("Show Day/Night Bar", ref Settings.showDayNightBar);
            listingStandard.CheckboxLabeled("Show Time Trace Line", ref Settings.showTimeTraceLine);
            listingStandard.Gap(20f);

            Settings.arrowColor = DrawColorSliders(listingStandard.GetRect(80), "Arrow Color", Settings.arrowColor);
            listingStandard.Gap(10f);
            Settings.highlightColor = DrawColorSliders(listingStandard.GetRect(80), "Highlight Color", Settings.highlightColor);
            listingStandard.Gap(10f);
            Settings.topCursorColor = DrawColorSliders(listingStandard.GetRect(80), "Top Cursor Color", Settings.topCursorColor);
            listingStandard.Gap(10f);
            Settings.bottomCursorColor = DrawColorSliders(listingStandard.GetRect(80), "Bottom Cursor Color", Settings.bottomCursorColor);
            listingStandard.Gap(10f);
            Settings.pawnBarColor = DrawColorSliders(listingStandard.GetRect(80), "Pawn Bar Color", Settings.pawnBarColor);
            listingStandard.Gap(10f);
            Settings.dayNightBarColor = DrawColorSliders(listingStandard.GetRect(80), "Day/Night Bar Color", Settings.dayNightBarColor);

            listingStandard.End();
            Widgets.EndScrollView();

            Settings.Write();
        }

        private Color DrawColorSliders(Rect rect, string label, Color color)
        {
            Widgets.Label(rect, label);
            rect.y += 20f;
            rect.height = 20f;

            color.r = Widgets.HorizontalSlider(rect, color.r, 0f, 1f, false, $"R: {color.r:F2}");
            rect.y += 25f;
            color.g = Widgets.HorizontalSlider(rect, color.g, 0f, 1f, false, $"G: {color.g:F2}");
            rect.y += 25f;
            color.b = Widgets.HorizontalSlider(rect, color.b, 0f, 1f, false, $"B: {color.b:F2}");

            return color;
        }
    }
}

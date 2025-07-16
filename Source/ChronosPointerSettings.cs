using ColourPicker;
using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace ChronosPointer
{
    public class ChronosPointerSettings : ModSettings
    {


        // Method to ensure cursorThickness is always an even number
        public void ValidateCursorThickness(ref float cursor)
        {
            if (cursor % 2 != 0)
            {
                cursor += 1f; // Adjust to the next even number
            }
        }
        // Toggles
        public bool DoArrowDraw = true;
        public bool DoHighlightDraw = true;
        public bool DoDayNightBarDraw = true;
        public bool DoDayNightIndicatorDraw = true;
        public bool DoDynamicTimeTraceLineDraw = true;
        public bool DoPawnLineDraw = true;
        public bool DoHollowHour = false;
        public bool DoIncidentSpecialDraw = true;
        public bool DoLoadWarnings = true;

        // Floats
        public float CursorThickness = 2f; // Default thickness
        public float DayNightBarCursorThickness = 2f; // Default thickness
        public float AuroraMinOpacity = 0.1f;
        public float AuroraMaxOpacity = 0.75f;
        public float SunlightThreshold_Night = 0.0f;
        public float SunlightThreshold_Any = 0.05f;
        public float SunlightThreshold_DawnDusk = 0.35f;
        public float SunlightThreshold_SunriseSunset = 0.7f;

        // Ints
        public int HighlightBorderThickness = 2; // Default thickness

        // Colors
        public Color ArrowColor = Color.red;
        public Color HighlightColor = new Color(1f, 1f, 0f, 0.3f);
        public Color BottomCursorColor = Color.white;
        public Color TimeTraceColorDay = Color.black;
        public Color TimeTraceColorNight = Color.white;
        public Color NightColor = new Color(0f, 0f, 0.5f); //Deep blue
        public Color DawnDuskColor = new Color(0.5f, 0.5f, 1f); // Light Blue
        public Color SunriseSunsetColor = new Color(1f, 0.5f, 0f);   // Orange;
        public Color DayColor = Color.yellow;
        public Color VolcanicWinterColor = new Color(0f, 0f, 0f, 0.75f);
        public Color ToxicFalloutColor = new Color(0.25f, 1.6f, 0.25f, 0.75f);
        public Color AuroraColor1 = new Color(1.0f, 0.5f, 1.0f, 1.0f);
        public Color AuroraColor2 = new Color(0.5f, 1.0f, 0.5f, 1.0f);

        public Color _DefaultTransparentColor = new Color(1, 1, 1, 0);
        public Color _HighlightInteriorColor = new Color(0f, 0f, 0f, 0f);

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref DoArrowDraw, "DoArrowDraw", true);
            Scribe_Values.Look(ref DoHighlightDraw, "DoHighlightDraw", true);
            Scribe_Values.Look(ref DoDayNightBarDraw, "DoDayNightBarDraw", true);
            Scribe_Values.Look(ref DoDayNightIndicatorDraw, "DoDayNightIndicatorDraw", true);
            Scribe_Values.Look(ref DoDynamicTimeTraceLineDraw, "DoDynamicTimeTraceLineDraw", true);
            Scribe_Values.Look(ref DoPawnLineDraw, "DoPawnLineDraw", true);
            Scribe_Values.Look(ref DoHollowHour, "DoHollowHour", false);
            Scribe_Values.Look(ref DoIncidentSpecialDraw, "DoIncidentSpecialDraw", true);
            Scribe_Values.Look(ref DoLoadWarnings, "DoLoadWarnings", true);

            Scribe_Values.Look(ref ArrowColor, "ArrowColor", Color.red);
            Scribe_Values.Look(ref HighlightColor, "HighlightColor", new Color(1f, 1f, 0f, 0.3f));
            Scribe_Values.Look(ref BottomCursorColor, "BottomCursorColor", Color.white);
            Scribe_Values.Look(ref TimeTraceColorDay, "TimeTraceColorDay", Color.black);
            Scribe_Values.Look(ref TimeTraceColorNight, "TimeTraceColorNight", Color.white);
            Scribe_Values.Look(ref NightColor, "NightColor", new Color(0f, 0f, 0.5f)); //Deep blue
            Scribe_Values.Look(ref DawnDuskColor, "DawnDuskColor", new Color(0.5f, 0.5f, 1f)); // Light Blue
            Scribe_Values.Look(ref SunriseSunsetColor, "SunriseSunsetColor", new Color(1f, 0.5f, 0f));   // Orange;
            Scribe_Values.Look(ref DayColor, "DayColor", Color.yellow);
            Scribe_Values.Look(ref VolcanicWinterColor, "VolcanicWinterColor", new Color(0f, 0f, 0f, 0.75f));
            Scribe_Values.Look(ref ToxicFalloutColor, "ToxicFalloutColor", new Color(0.25f, 1.6f, 0.25f, 0.75f));
            Scribe_Values.Look(ref AuroraColor1, "AuroraColor1", new Color(1.0f, 0.5f, 1.0f, 1.0f));
            Scribe_Values.Look(ref AuroraColor2, "AuroraColor2", new Color(0.5f, 1.0f, 0.5f, 1.0f));

            Scribe_Values.Look(ref CursorThickness, "CursorThickness", 2f); // Default thickn
            Scribe_Values.Look(ref DayNightBarCursorThickness, "DayNightBarCursorThickness", 2f); // Def
            Scribe_Values.Look(ref AuroraMinOpacity, "AuroraMinOpacity", 0.1f);
            Scribe_Values.Look(ref AuroraMaxOpacity, "AuroraMaxOpacity", 0.75f);
            Scribe_Values.Look(ref SunlightThreshold_Night, "SunlightThreshold_Night", 0.0f);
            Scribe_Values.Look(ref SunlightThreshold_Any, "SunlightThreshold_Any", 0.05f);
            Scribe_Values.Look(ref SunlightThreshold_DawnDusk, "SunlightThreshold_DawnDusk", 0.35f);
            Scribe_Values.Look(ref SunlightThreshold_SunriseSunset, "SunlightThreshold_SunriseSunset", 0.7f);

            // Validate cursorThickness after loading
            ValidateCursorThickness(ref CursorThickness);
            ValidateCursorThickness(ref DayNightBarCursorThickness);
        }
        public void ResetToDefaults()
        {
            DoArrowDraw = true;
            DoHighlightDraw = true;
            DoDayNightBarDraw = true;
            DoDayNightIndicatorDraw = true;
            DoDynamicTimeTraceLineDraw = true;
            DoPawnLineDraw = true;
            DoHollowHour = false;
            DoIncidentSpecialDraw = true;
            //DoLoadWarnings = true; Don't reset because the user probably doesn't want to see the warnings again after they have been disabled.

            CursorThickness = 2f; // Default thickness
            DayNightBarCursorThickness = 2f; // Default thickness
            AuroraMinOpacity = 0.1f;
            AuroraMaxOpacity = 0.75f;
            SunlightThreshold_Night = 0.0f;
            SunlightThreshold_Any = 0.05f;
            SunlightThreshold_DawnDusk = 0.35f;
            SunlightThreshold_SunriseSunset = 0.7f;

            HighlightBorderThickness = 2; // Default thickness

            ArrowColor = Color.red;
            HighlightColor = new Color(1f, 1f, 0f, 0.3f);
            BottomCursorColor = Color.white;
            TimeTraceColorDay = Color.black;
            TimeTraceColorNight = Color.white;
            NightColor = new Color(0f, 0f, 0.5f); //Deep blue
            DawnDuskColor = new Color(0.5f, 0.5f, 1f); // Light Blue
            SunriseSunsetColor = new Color(1f, 0.5f, 0f);   // Orange;
            DayColor = Color.yellow;
            VolcanicWinterColor = new Color(0f, 0f, 0f, 0.75f);
            ToxicFalloutColor = new Color(0.25f, 1.6f, 0.25f, 0.75f);
            AuroraColor1 = new Color(1.0f, 0.5f, 1.0f, 1.0f);
            AuroraColor2 = new Color(0.5f, 1.0f, 0.5f, 1.0f);

            _DefaultTransparentColor = new Color(1, 1, 1, 0);
            _HighlightInteriorColor = new Color(0f, 0f, 0f, 0f);

            // Validate cursorThickness after resetting
            ValidateCursorThickness(ref CursorThickness);
            ValidateCursorThickness(ref DayNightBarCursorThickness);
        }

        public void OldWindow(Rect inRect)
        {
            float height = 1000f;
            Rect viewRect = new Rect(0f, 40f, inRect.width - 50f, height);

            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(viewRect);
            DoTopSettings(listingStandard);

            listingStandard.Gap(15f);
            // ListingWidth
            listingStandard.ColumnWidth = inRect.width / 2f;
            float LRHeight = listingStandard.CurHeight + 25f;

            listingStandard.Gap(5f);
            DoLeftSettings(inRect, listingStandard);

            listingStandard.End();
            Rect rightRect = new Rect(inRect.x + inRect.width / 2f + 16f, LRHeight, inRect.width / 2f - 50f, height);

            Listing_Standard rightListing = new Listing_Standard();
            // ListingWidth
            rightListing.ColumnWidth = rightRect.width;
            rightListing.Begin(rightRect);
            rightListing.Gap(5f);
            DoRightSettings(rightListing);
            rightListing.End();
            GUI.color = Color.white;
        }


        public void DoWindowContents(Rect inRect)
        {
            var list = new Listing_Standard();
            list.Begin(inRect);
            list.Label("Coming soon! This is a placeholder for the new settings window design.");
            list.End();
        }

        private void DoTopSettings(Listing_Standard listingStandard)
        {
            if (listingStandard.ButtonText("Reset to Default"))
            {
                ResetToDefaults();
            }
            listingStandard.Gap(20f);

            listingStandard.CheckboxLabeled("- Show Arrow", ref DoArrowDraw);
            listingStandard.CheckboxLabeled("- Show Day/Night Bar", ref DoDayNightBarDraw);

            // Gray out unapplicable settings
            if (!DoDayNightBarDraw) GUI.color = Color.gray;
            listingStandard.CheckboxLabeled("    Show Day/Night Bar Time Indicator", ref DoDayNightIndicatorDraw);
            GUI.color = Color.white;

            if (!DoDayNightIndicatorDraw || !DoDayNightBarDraw) GUI.color = Color.gray;
            listingStandard.CheckboxLabeled("    Dynamic Day/Night Bar Cursor Color", ref DoDynamicTimeTraceLineDraw);
            GUI.color = Color.white;

            listingStandard.CheckboxLabeled("- Show Pawn Section Time Indicator", ref DoPawnLineDraw);
            listingStandard.CheckboxLabeled("- Show Current Hour Highlight", ref DoHighlightDraw);
            if (!DoHighlightDraw) GUI.color = Color.gray;
            listingStandard.CheckboxLabeled("    Hollow Current Hour Highlight", ref DoHollowHour);
            GUI.color = Color.white;
            listingStandard.CheckboxLabeled("Do Incident Special Effects", ref DoIncidentSpecialDraw);
            listingStandard.CheckboxLabeled("Show Warnings on Load", ref DoLoadWarnings);
            GUI.color = Color.white;
        }



        private void DoLeftSettings(Rect inRect, Listing_Standard listingStandard)
        {

            if (Current.Game != null)
            {
                GUI.color = Color.green;
                if (listingStandard.ButtonText("Show schedule menu"))
                {
                    var fakeSchedule = Find.MainButtonsRoot.allButtonsInOrder.Where((MainButtonDef button) => { return button.TabWindow is MainTabWindow_Schedule; }).First()?.TabWindow;
                    if (fakeSchedule != null)
                    {
                        fakeSchedule.layer = WindowLayer.SubSuper;
                        Find.WindowStack.Add(fakeSchedule);
                        var confirmWindow = new Dialog_IncidentTesting("", "Done", () =>
                        {
                            Find.WindowStack.TryRemove(fakeSchedule);
                        }, layer: WindowLayer.Super);
                        confirmWindow.absorbInputAroundWindow = true;
                        confirmWindow.doCloseButton = false;
                        confirmWindow.draggable = true;
                        Find.WindowStack.Add(confirmWindow);
                    }
                }
            }
            else
            {
                GrayIfInactive(false);
                listingStandard.ButtonText("Show schedule menu");
                GUI.color = Color.white;
            }
            listingStandard.Gap(10f);
            // Arrow Color
            GrayIfInactive(DoArrowDraw);
            DoColorPickButton(listingStandard, ArrowColor, (newColor, isClosing) => { ArrowColor = newColor; }, "Change Arrow Color");

            listingStandard.Gap(4f);

            // Pawn cursor
            GrayIfInactive(DoPawnLineDraw);
            listingStandard.Label($"Pawn Section Time Indicator Thickness: {CursorThickness:F1}");
            float newThickness = listingStandard.Slider(CursorThickness, 2f, 10f);
            if (DoPawnLineDraw)
            {
                CursorThickness = Mathf.Round(newThickness / 2f) * 2f; // Round to nearest even number
            }
            listingStandard.Gap(4f);

            GrayIfInactive(DoPawnLineDraw);
            DoColorPickButton(listingStandard, BottomCursorColor, (newColor, isClosing) => { BottomCursorColor = newColor; }, "Change Pawn Section Time Indicator Color");

            // Current hour highlight
            GrayIfInactive(DoHighlightDraw);
            GrayIfInactive(DoDayNightIndicatorDraw);
            listingStandard.Label($"Pawn Section Time Indicator Thickness: {DayNightBarCursorThickness:F1}");
            float secondNewThickness = listingStandard.Slider(DayNightBarCursorThickness, 2f, 10f);
            if (DoDayNightIndicatorDraw)
            {
                DayNightBarCursorThickness = Mathf.Round(secondNewThickness / 2f) * 2f; // Round to nearest even number
            }
            DoColorPickButton(listingStandard, HighlightColor, ChangeCurrentHourHighlightAction(), "Change current-hour color");
            GUI.color = Color.white;

        }
        private void DoRightSettings(Listing_Standard listingStandard)
        {


            // Day/Night bar day and night colors
            GrayIfInactive(DoDayNightBarDraw);
            GrayIfInactive(DoDayNightBarDraw && DoDayNightIndicatorDraw);
            // Day/Default
            DoColorPickButton(listingStandard, TimeTraceColorDay, (color, b) => TimeTraceColorDay = color, "Change Time Trace Color" + (DoDynamicTimeTraceLineDraw ? " Day" : ""));
            // Night
            GrayIfInactive(DoDayNightBarDraw && DoDayNightIndicatorDraw && DoDynamicTimeTraceLineDraw);
            DoColorPickButton(listingStandard, TimeTraceColorNight, (color, b) => TimeTraceColorNight = color, "Change Time Trace Color Night");

            GrayIfInactive(DoDayNightBarDraw);
            DoColorPickButton(listingStandard, NightColor, (color, b) => NightColor = color, "Change Night Color");
            DoColorPickButton(listingStandard, DawnDuskColor, (color, b) => DawnDuskColor = color, "Change Dawn/Dusk Color");
            DoColorPickButton(listingStandard, SunriseSunsetColor, (color, b) => SunriseSunsetColor = color, "Change Sunrise/Sunset Color");
            DoColorPickButton(listingStandard, DayColor, (color, b) => DayColor = color, "Change Day Color");


        }
        private Action<Color, bool> ChangeCurrentHourHighlightAction()
        {
            return (color, isClosing) =>
            {
                if (color.a > 0.3f && !DoHollowHour)
                {

                    if (!isClosing)
                    {
                        Find.WindowStack.Add(new Dialog_Confirm("The chosen color has low transparency and may be hard to use with a solid current hour highlight.", "Use anyway", () => HighlightColor = color));
                    }
                    else
                    {
                        Find.WindowStack.Add(new Dialog_ColourPicker(color, ChangeCurrentHourHighlightAction()));
                        Find.WindowStack.Add(new Dialog_Confirm("The chosen color has low transparency and may be hard to use with a solid current hour highlight.", "Use anyway", () =>
                        {
                            Find.WindowStack.TryGetWindow<Dialog_ColourPicker>(out var picker);
                            picker?.Close();
                            HighlightColor = color;
                        }));
                    }
                }
                else
                {
                    HighlightColor = color;
                }

            };
        }

        private static void DoColorPickButton(Listing_Standard listingStandard, Color color, Action<Color, bool> colorChangeOperation, string buttonText)
        {
            Widgets.DrawBoxSolid(listingStandard.GetRect(10), color);
            if (listingStandard.ButtonText(buttonText))
                Find.WindowStack.Add(new Dialog_ColourPicker(color, colorChangeOperation));
            listingStandard.Gap(10f);
        }

        /// <summary>
        /// returns true if the setting is active, false if it is inactive and should be grayed out.
        /// </summary>
        /// <param name="isActive"></param>
        /// <returns></returns>
        private static bool GrayIfInactive(bool isActive)
        {
            GUI.color = Color.white;

            // If the setting isn't being used, gray the color picker out so it's obvious it's not being used.
            if (!isActive)
            {
                GUI.color = Color.gray;
            }

            return isActive;
        }
    }

}

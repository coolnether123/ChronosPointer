using ColourPicker;
using Mono.Security.Protocol.Ntlm;
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
        public static int ValidateCursorThickness(ref float cursor)
        {
            if (cursor % 2 != 0)
            {
                cursor += 1; // Adjust to the next even number
            }
            return (int) cursor;
        }
        // Toggles
        public bool DrawArrow = true;
        //public bool DrawHighlight = true;

        public bool DrawHourBar = true;
        public bool DrawHoursBarCursor = true;
        public bool DoDynamicHoursBarLine = true;
        
        public bool DrawHourBarGetSet
        {
            get => DrawHourBar;
            set
            {
                DrawHourBar = value;
                if (!value)
                {
                    DrawHoursBarCursor = value; // Disable cursor if bar is disabled
                    DoDynamicHoursBarLine = value; // Disable cursor if bar is disabled

                }
            }
        }

        public bool DrawCurrentHourHighlight = true;
        public bool DoFilledHourHighlight = true;

        public bool DrawCurrentHourHighlightGetSet
        {
            get => DrawCurrentHourHighlight;
            set
            {
                DrawCurrentHourHighlight = value;
                if(!value)
                {
                    DoFilledHourHighlight = value; // Disable highlight fill if highlight is disabled
                }
            }
        }

        public bool DrawMainCursor = true;
        public bool DrawIncidentOverlay = true;
        public bool DoLoadWarnings = true;

        // Floats
        public float CursorThickness = 2; // Default thickness
        public float HoursBarCursorThickness = 2; // Default thickness
        public float AuroraMinOpacity = 0.1f;
        public float AuroraMaxOpacity = 0.75f;
        public float SunlightThreshold_Night = 0.0f;
        public float _SunlightThreshold_Any = 0.05f;
        public float SunlightThreshold_DawnDusk = 0.35f;
        public float SunlightThreshold_SunriseSunset = 0.7f;

        // Ints
        public float HighlightBorderThickness = 2; // Default thickness

        // Colors
        public Color Color_Arrow = Color.red;
        public Color Color_HourHighlight = new Color(1f, 1f, 0f, 0.3f);
        public Color Color_MainCursor = Color.white;
        public Color Color_HoursBarCursor_Day = Color.black;
        public Color Color_HoursBarCursor_Night = Color.white;
        public Color Color_Night = new Color(0f, 0f, 0.5f); //Deep blue
        public Color Color_DawnDusk = new Color(0.5f, 0.5f, 1f); // Light Blue
        public Color Color_SunriseSunset = new Color(1f, 0.5f, 0f);   // Orange;
        public Color Color_Day = Color.yellow;
        public Color Color_VolcanicWinter = new Color(0f, 0f, 0f, 0.5f);
        public Color ToxicFalloutColor = new Color(0f, 1f, 0f, 0.5f);
        public Color AuroraColor1 = new Color(1.0f, 0.5f, 1.0f, 1.0f);
        public Color AuroraColor2 = new Color(0.5f, 1.0f, 0.5f, 1.0f);

        public Color _DefaultTransparentColor = new Color(1, 1, 1, 0);
        public Color _HighlightInteriorColor = new Color(0f, 0f, 0f, 0f);

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref DrawArrow, "DrawArrow", true);
            Scribe_Values.Look(ref DrawCurrentHourHighlight, "DrawHighlight", true);
            Scribe_Values.Look(ref DrawHourBar, "DrawHourBar", true);
            Scribe_Values.Look(ref DrawHoursBarCursor, "DrawHoursBarCursor", true);
            Scribe_Values.Look(ref DoDynamicHoursBarLine, "DrawDynamicTimeTraceLine", true);
            Scribe_Values.Look(ref DrawMainCursor, "DrawMainCursor", true);
            Scribe_Values.Look(ref DoFilledHourHighlight, "DoFilledHourHighlight", false);
            Scribe_Values.Look(ref DrawIncidentOverlay, "DrawIncidentOverlay", true);
            Scribe_Values.Look(ref DoLoadWarnings, "DoLoadWarnings", true);

            Scribe_Values.Look(ref Color_Arrow, "Color_Arrow", Color.red);
            Scribe_Values.Look(ref Color_HourHighlight, "Color_Highlight", new Color(1f, 1f, 0f, 0.3f));
            Scribe_Values.Look(ref Color_MainCursor, "Color_MainCursor", Color.white);
            Scribe_Values.Look(ref Color_HoursBarCursor_Day, "Color_HoursBarCursor_Day", Color.black);
            Scribe_Values.Look(ref Color_HoursBarCursor_Night, "Color_HoursBarCursor_Night", Color.white);
            Scribe_Values.Look(ref Color_Night, "Color_Night", new Color(0f, 0f, 0.5f)); //Deep blue
            Scribe_Values.Look(ref Color_DawnDusk, "Color_DawnDusk", new Color(0.5f, 0.5f, 1f)); // Light Blue
            Scribe_Values.Look(ref Color_SunriseSunset, "Color_SunriseSunset", new Color(1f, 0.5f, 0f));   // Orange;
            Scribe_Values.Look(ref Color_Day, "Color_Day", Color.yellow);
            Scribe_Values.Look(ref Color_VolcanicWinter, "Color_VolcanicWinter", new Color(0f, 0f, 0f, 0.5f));
            Scribe_Values.Look(ref ToxicFalloutColor, "ToxicFalloutColor", new Color(0f, 1f, 0f, 0.5f));
            Scribe_Values.Look(ref AuroraColor1, "AuroraColor1", new Color(1.0f, 0.5f, 1.0f, 1.0f));
            Scribe_Values.Look(ref AuroraColor2, "AuroraColor2", new Color(0.5f, 1.0f, 0.5f, 1.0f));

            float writeCursorThickness = CursorThickness;
            float writeHoursBarThickness = HoursBarCursorThickness;
            float writeHighlightBorderThickness = HighlightBorderThickness;

            Scribe_Values.Look(ref writeCursorThickness, "CursorThickness", 2f); // Default thickn
            Scribe_Values.Look(ref writeHoursBarThickness, "HoursBarThickness", 2f); // Def
            Scribe_Values.Look(ref writeHighlightBorderThickness, "HighlightBorderThickness", 0.7f);
            CursorThickness = (int)Mathf.Floor(writeCursorThickness );
            HoursBarCursorThickness = (int)Mathf.Floor(writeHoursBarThickness );
            HighlightBorderThickness = (int)Mathf.Floor(writeHighlightBorderThickness);


            Scribe_Values.Look(ref AuroraMinOpacity, "AuroraMinOpacity", 0.1f);
            Scribe_Values.Look(ref AuroraMaxOpacity, "AuroraMaxOpacity", 0.75f);
            Scribe_Values.Look(ref SunlightThreshold_Night, "SunlightThreshold_Night", 0.0f);
            Scribe_Values.Look(ref _SunlightThreshold_Any, "SunlightThreshold_Any", 0.05f);
            Scribe_Values.Look(ref SunlightThreshold_DawnDusk, "SunlightThreshold_DawnDusk", 0.35f);
            Scribe_Values.Look(ref SunlightThreshold_SunriseSunset, "SunlightThreshold_SunriseSunset", 0.7f);

            // Validate cursorThickness after loading
            ValidateCursorThickness(ref CursorThickness);
            ValidateCursorThickness(ref HoursBarCursorThickness);
        }
        public void ResetToDefaults()
        {
            DrawArrow = true;
            DrawCurrentHourHighlight = true;
            DrawHourBar = true;
            DrawHoursBarCursor = true;
            DoDynamicHoursBarLine = true;
            DrawMainCursor = true;
            DoFilledHourHighlight = true;
            DrawIncidentOverlay = true;
            //DoLoadWarnings = true; Don't reset because the user probably doesn't want to see the warnings again after they have been disabled.

            CursorThickness = 2; // Default thickness
            HoursBarCursorThickness = 2; // Default thickness
            AuroraMinOpacity = 0.1f;
            AuroraMaxOpacity = 0.75f;
            SunlightThreshold_Night = 0.0f;
            _SunlightThreshold_Any = 0.05f;
            SunlightThreshold_DawnDusk = 0.35f;
            SunlightThreshold_SunriseSunset = 0.7f;

            HighlightBorderThickness = 2; // Default thickness

            Color_Arrow = Color.red;
            Color_HourHighlight = new Color(1f, 1f, 0f, 0.3f);
            Color_MainCursor = Color.white;
            Color_HoursBarCursor_Day = Color.black;
            Color_HoursBarCursor_Night = Color.white;
            Color_Night = new Color(0f, 0f, 0.5f); //Deep blue
            Color_DawnDusk = new Color(0.5f, 0.5f, 1f); // Light Blue
            Color_SunriseSunset = new Color(1f, 0.5f, 0f);   // Orange;
            Color_Day = Color.yellow;
            Color_VolcanicWinter = new Color(0f, 0f, 0f, 0.5f);
            ToxicFalloutColor = new Color(0f, 1f, 0f, 0.5f);
            AuroraColor1 = new Color(1.0f, 0.5f, 1.0f, 1.0f);
            AuroraColor2 = new Color(0.5f, 1.0f, 0.5f, 1.0f);

            _DefaultTransparentColor = new Color(1, 1, 1, 0);
            _HighlightInteriorColor = new Color(0f, 0f, 0f, 0f);

            // Validate cursorThickness after resetting
            ValidateCursorThickness(ref CursorThickness);
            ValidateCursorThickness(ref HoursBarCursorThickness);
            Write();
        }

        

        public void DoWindowContents(Rect inRect)
        {
            Rect buttonRect = new Rect(inRect.width - 150f-29f, 0f, 150f, 30f);
            if(Widgets.ButtonText(buttonRect, "Reset Defaults"))
            {
#if V1_5U
                Find.WindowStack.Add(new Dialog_Confirm("Are you sure you want to reset all settings to defaults?", () => 
                {
                    ResetToDefaults();
                }));
#else
                Find.WindowStack.Add(new Dialog_MessageBox("Are you sure you want to reset all settings to defaults?", "Confirm", () => 
                {
                    ResetToDefaults();
                }, "Cancel"));
#endif
            }

            Rect scheduleButtonRect = buttonRect;
            scheduleButtonRect.x -= buttonRect.width + 10f;
            DoShowScheduleWindowButton(scheduleButtonRect);

            Rect left = inRect.LeftHalf();
            left.width -= 20f;
            Rect center = left.LeftHalf(); center.x = left.width + 10f;

            Rect right = center; right.x = (left.width + center.width) + 20f;

            var listL = new Listing_Standard();
            listL.Begin(left);
            listL.CheckboxLabeled("Show Arrow", ref DrawArrow, tooltip: "Whether to draw the arrow above the schedule area.");
            listL.CheckboxLabeled("Show Hours Bar", ref DrawHourBar, tooltip: "Whether to draw the Hours Bar.");
            DrawHourBarGetSet = DrawHourBar;

            GrayIfInactive(DrawHourBar);
            Text.Font = GameFont.Tiny;
            listL.CheckboxLabeled("- Show Hours Bar Cursor", ref DrawHoursBarCursor, tooltip: "Whether to draw the Hours Bar cursor.");
            listL.CheckboxLabeled("- Dynamic Hours Bar Cursor Color", ref DoDynamicHoursBarLine, tooltip: "Whether to dynamically change the Hours Bar cursor color based on daylight level.");
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            listL.CheckboxLabeled("Show Main Cursor", ref DrawMainCursor, tooltip: "Whether to show the Pawn Main Cursor.");
            listL.CheckboxLabeled("Show Current Hour Highlight", ref DrawCurrentHourHighlight, tooltip: "Whether to show the Current Hour highlight.");
            DrawCurrentHourHighlightGetSet = DrawCurrentHourHighlight;
            
            GrayIfInactive(DrawCurrentHourHighlight);
            Text.Font = GameFont.Tiny;
            listL.CheckboxLabeled("- Fill Current Hour Highlight", ref DoFilledHourHighlight, tooltip: "Whether to fill the Current Hour highlight.");
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            listL.CheckboxLabeled("Do Incident Special Effects", ref DrawIncidentOverlay, tooltip: "Whether to display incident effects. The Hours Bar has special effects during certain incidents.)");
            listL.CheckboxLabeled("Show Warnings on Load", ref DoLoadWarnings, tooltip: "Whether to show mod conflict warnins on startup.");
            GUI.color = Color.white;
            
            listL.Gap();
            GrayIfInactive(DrawMainCursor || DrawHoursBarCursor || DrawCurrentHourHighlight);
            listL.Label($"Cursor thicknesses:");
            Text.Font = GameFont.Tiny;
            GrayIfInactive(DrawMainCursor);
#if !V1_3
            CursorThickness                 = (int) listL.SliderLabeled($"- Main Cursor ({CursorThickness:F1})", ValidateCursorThickness(ref CursorThickness), 2f, 10f, tooltip: "Thickness of the Main Cursor.");
            GrayIfInactive(DrawHoursBarCursor);
            HoursBarCursorThickness      = (int) listL.SliderLabeled($"- Hours Bar Cursor ({HoursBarCursorThickness:F1})", ValidateCursorThickness(ref HoursBarCursorThickness), 2f, 10f, tooltip: "Thickness of the Hours Bar cursor.");
            GrayIfInactive(DrawCurrentHourHighlight);
#else
            ;

            CursorThickness = (int) Do1_3LabeledSlider($"- Main Cursor ({CursorThickness:F1})", listL, ref CursorThickness, 2f, 10f, true);
            GrayIfInactive(DrawHoursBarCursor);
            HoursBarCursorThickness = (int) Do1_3LabeledSlider($"- Hours Bar Cursor ({HoursBarCursorThickness:F1})", listL, ref HoursBarCursorThickness, 2f, 10f, true);
            GrayIfInactive(DrawCurrentHourHighlight);
            HighlightBorderThickness = (int) Do1_3LabeledSlider($"- Highlight Border ({HighlightBorderThickness:F1})", listL, ref HighlightBorderThickness, 2f, 10f, true);
#endif
            Text.Font = GameFont.Small;
            listL.Gap();
            GrayIfInactive(DrawHourBar);
            listL.Label("Sunlight thresholds (0.0 = no sunlight, 1.0 = full sunlight):");
            Text.Font = GameFont.Tiny;
#if !V1_3
            SunlightThreshold_Night         = listL.SliderLabeled($"- Night ({SunlightThreshold_Night:F2})", SunlightThreshold_Night, 0.0f, 1.0f, tooltip: "How dark the map has to be to show the night color.");
            //_SunlightThreshold_Any           = listL.SliderLabeled($"Any", _SunlightThreshold_Any, 0.0f, 1.0f, tooltip: "How light the map has to be to show the  color."); //Don't change the any sunlight threshold.
            SunlightThreshold_DawnDusk      = listL.SliderLabeled($"- Dawn/Dusk ({SunlightThreshold_DawnDusk:F2})", SunlightThreshold_DawnDusk, 0.0f, 1.0f, tooltip: "How dark the map has to be to show the dawn/dusk color.");
            SunlightThreshold_SunriseSunset = listL.SliderLabeled($"- Sunrise/Sunset ({SunlightThreshold_SunriseSunset:F2})", SunlightThreshold_SunriseSunset, 0.0f, 1.0f, tooltip:"How dark the map has to be to show the sunrise/sunset color.");
#else
            SunlightThreshold_Night         =  Do1_3LabeledSlider($"- Night ({SunlightThreshold_Night:F2})", listL, ref SunlightThreshold_Night, 0.0f, 1.0f);
            //_SunlightThreshold_Any           = listL.SliderLabeled($"Any", _SunlightThreshold_Any, 0.0f, 1.0f, tooltip: "How light the map has to be to show the  color."); //Don't change the any sunlight threshold.
            SunlightThreshold_DawnDusk      = Do1_3LabeledSlider($"- Dawn/Dusk ({SunlightThreshold_DawnDusk:F2})", listL, ref SunlightThreshold_DawnDusk, 0.0f, 1.0f);
            SunlightThreshold_SunriseSunset = Do1_3LabeledSlider($"- Sunrise/Sunset ({SunlightThreshold_SunriseSunset:F2})", listL, ref SunlightThreshold_SunriseSunset, 0.0f, 1.0f);
#endif
            Text.Font = GameFont.Small;
            listL.Gap();
            listL.Label("Aurora Opacity:");
            Text.Font = GameFont.Tiny;

            GrayIfInactive(DrawIncidentOverlay);
#if !V1_3
            AuroraMinOpacity                = listL.SliderLabeled($"- Aurora Min Opacity ({AuroraMinOpacity:F2})", AuroraMinOpacity, 0.0f, 1.0f, tooltip: "The minimum transparancy the aurora effect gets.");
            GrayIfInactive(DrawIncidentOverlay);
            AuroraMaxOpacity                = listL.SliderLabeled($"- Aurora Max Opacity ({AuroraMaxOpacity:F2})", AuroraMaxOpacity, 0.0f, 1.0f, tooltip: "The maximum transparancy the aurora effect gets.");
#else
            AuroraMinOpacity                = Do1_3LabeledSlider($"- Aurora Min Opacity ({AuroraMinOpacity:F2})", listL, ref AuroraMinOpacity, 0.0f, 1.0f);
            GrayIfInactive(DrawIncidentOverlay);
            AuroraMaxOpacity                = Do1_3LabeledSlider($"- Aurora Max Opacity ({AuroraMaxOpacity:F2})", listL, ref AuroraMaxOpacity, 0.0f, 1.0f);
            //do the 1.3 stuff
#endif
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            listL.End();
            GUI.color = Color.white;

            
            var listC = new Listing_Standard();
            
            listC.Begin(center);
            listC.Label("Main Colors");

            GrayIfInactive(DrawArrow);
            DoColorPickButton(listC, Color_Arrow, (newColor, isClosing) => { Color_Arrow = newColor; }, "Arrow Color");
            
            GrayIfInactive(DrawMainCursor);
            DoColorPickButton(listC, Color_MainCursor, (newColor, isClosing) => { Color_MainCursor = newColor; }, "Cursor Color");
            
            GrayIfInactive(DrawCurrentHourHighlight);
            DoColorPickButton(listC, Color_HourHighlight, ChangeCurrentHourHighlightAction(), "Current-hour color");

            GrayIfInactive(DrawHourBar);
            listC.Label("Hour Bar Cursor Colors");
            DoColorPickButton(listC, Color_HoursBarCursor_Day, (color, b) => Color_HoursBarCursor_Day = color, !DoDynamicHoursBarLine ? "Time Trace Color" : "Time Trace Color Day");
            
            GrayIfInactive(DoDynamicHoursBarLine);
            DoColorPickButton(listC, Color_HoursBarCursor_Night, (color, b) => Color_HoursBarCursor_Night = color, "Time Trace Color Night");

            GrayIfInactive(DrawHourBar);
            listC.Label("Hour Bar Colors");
            DoColorPickButton(listC, Color_Night, (color, b) => Color_Night = color, "Night Color");
            DoColorPickButton(listC, Color_DawnDusk, (color, b) => Color_DawnDusk = color, "Dawn/Dusk Color");
            DoColorPickButton(listC, Color_SunriseSunset, (color, b) => Color_SunriseSunset = color, "Sunrise/Sunset Color");
            DoColorPickButton(listC, Color_Day, (color, b) => Color_Day = color, "Day Color");
            
            GUI.color = Color.white;

            listC.End();
            
            var listR = new Listing_Standard();
            
            listR.Begin(right);

            listR.Label("Incident Overlay Colors");
            GrayIfInactive(DrawIncidentOverlay);
            DoColorPickButton(listR, ToxicFalloutColor,ChangeCurrentToxicColorAction(), "Toxic Fallout Color");
            DoColorPickButton(listR, Color_VolcanicWinter, ChangeCurrentVolacanicColorAction(), "Volcanic Winter Color");
            DoColorPickButton(listR, AuroraColor1, (color, b) => AuroraColor1 = color, "Aurora Color 1");
            DoColorPickButton(listR, AuroraColor2, (color, b) => AuroraColor2 = color, "Aurora Color 2");
            GUI.color = Color.white;

            listR.End();

            
        }

#if V1_3
        public static float Do1_3LabeledSlider(string label, Listing_Standard list, ref float value, float min, float max, bool validateThickness = false)
        {
            return Widgets.HorizontalSlider(list.Label(label).RightHalf(), validateThickness ? (float)ValidateCursorThickness(ref value) : value, min, max);
        }
#endif
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
        private void DoTopSettings(Listing_Standard listingStandard)
        {
            if (listingStandard.ButtonText("Reset to Default"))
            {
                ResetToDefaults();
            }
            listingStandard.Gap(20f);

            listingStandard.CheckboxLabeled("- Show Arrow", ref DrawArrow);
            listingStandard.CheckboxLabeled("- Show Hours Bar", ref DrawHourBar);

            // Gray out unapplicable settings
            if (!DrawHourBar) GUI.color = Color.gray;
            listingStandard.CheckboxLabeled("    Show Hours Bar Time Indicator", ref DrawHoursBarCursor);
            GUI.color = Color.white;

            if (!DrawHoursBarCursor || !DrawHourBar) GUI.color = Color.gray;
            listingStandard.CheckboxLabeled("    Dynamic Hours Bar Cursor Color", ref DoDynamicHoursBarLine);
            GUI.color = Color.white;

            listingStandard.CheckboxLabeled("- Show Pawn Section Time Indicator", ref DrawMainCursor);
            listingStandard.CheckboxLabeled("- Show Current Hour Highlight", ref DrawCurrentHourHighlight);
            if (!DrawCurrentHourHighlight) GUI.color = Color.gray;
            listingStandard.CheckboxLabeled("    Hollow Current Hour Highlight", ref DoFilledHourHighlight);
            GUI.color = Color.white;
            listingStandard.CheckboxLabeled("Do Incident Special Effects", ref DrawIncidentOverlay);
            listingStandard.CheckboxLabeled("Show Warnings on Load", ref DoLoadWarnings);
            GUI.color = Color.white;
        }



        private void DoLeftSettings(Rect inRect, Listing_Standard listingStandard)
        {
            //DoShowScheduleWindow(listingStandard);
            listingStandard.Gap(10f);
            // Arrow Color
            GrayIfInactive(DrawArrow);
            DoColorPickButton(listingStandard, Color_Arrow, (newColor, isClosing) => { Color_Arrow = newColor; }, "Change Arrow Color");

            listingStandard.Gap(4f);

            // Pawn cursor
            GrayIfInactive(DrawMainCursor);
            listingStandard.Label($"Pawn Section Time Indicator Thickness: {CursorThickness:F2}");
            float newThickness = listingStandard.Slider(CursorThickness, 2f, 10f);
            if (DrawMainCursor)
            {
                //CursorThickness = Mathf.Round((float)newThickness / 2f) * 2f; // Round to nearest even number
            }
            listingStandard.Gap(4f);

            GrayIfInactive(DrawMainCursor);
            DoColorPickButton(listingStandard, Color_MainCursor, (newColor, isClosing) => { Color_MainCursor = newColor; }, "Change Pawn Section Time Indicator Color");

            // Current hour highlight
            GrayIfInactive(DrawCurrentHourHighlight);
            GrayIfInactive(DrawHoursBarCursor);
            listingStandard.Label($"Pawn Section Time Indicator Thickness: {HoursBarCursorThickness:F2}");
            float secondNewThickness = listingStandard.Slider(HoursBarCursorThickness, 2f, 10f);
            if (DrawHoursBarCursor)
            {
                //DayNightBarCursorThickness = Mathf.Round(secondNewThickness / 2f) * 2f; // Round to nearest even number
            }
            DoColorPickButton(listingStandard, Color_HourHighlight, ChangeCurrentHourHighlightAction(), "Change current-hour color");
            GUI.color = Color.white;

        }

        private static void DoShowScheduleWindowButton(Rect buttonRect)
        {
            if (Current.Game != null)
            {
                GUI.color = Color.green;
                if (Widgets.ButtonText(buttonRect, "Show schedule menu"))
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
                Widgets.ButtonText(buttonRect, "Show schedule menu");
            }
                GUI.color = Color.white;
        }

        private void DoRightSettings(Listing_Standard listingStandard)
        {


            // Hours Bar day and night colors
            GrayIfInactive(DrawHourBar);
            GrayIfInactive(DrawHourBar && DrawHoursBarCursor);
            // Day/Default
            DoColorPickButton(listingStandard, Color_HoursBarCursor_Day, (color, b) => Color_HoursBarCursor_Day = color, "Change Time Trace Color" + (DoDynamicHoursBarLine ? " Day" : ""));
            // Night
            GrayIfInactive(DrawHourBar && DrawHoursBarCursor && DoDynamicHoursBarLine);
            DoColorPickButton(listingStandard, Color_HoursBarCursor_Night, (color, b) => Color_HoursBarCursor_Night = color, "Change Time Trace Color Night");

            GrayIfInactive(DrawHourBar);
            DoColorPickButton(listingStandard, Color_Night, (color, b) => Color_Night = color, "Change Night Color");
            DoColorPickButton(listingStandard, Color_DawnDusk, (color, b) => Color_DawnDusk = color, "Change Dawn/Dusk Color");
            DoColorPickButton(listingStandard, Color_SunriseSunset, (color, b) => Color_SunriseSunset = color, "Change Sunrise/Sunset Color");
            DoColorPickButton(listingStandard, Color_Day, (color, b) => Color_Day = color, "Change Day Color");


        }

        #region This is completely wet (opposite of D.R.Y.) but I'm too dumb to figure out how to apply ref to a lambda expression (not allowed but there's workarounds) without completely ripping out the code and starting over.
        private Action<Color, bool> ChangeCurrentHourHighlightAction()
        {
            Log.Message("Made it here 1");
            return (color, isClosing) =>
            {
            Log.Message("Made it here 2");
                if (color.a > 0.3f && DoFilledHourHighlight)
                {
            Log.Message("Made it here 3");

                    if (!isClosing)
                    {
            Log.Message("Made it here 4");
#if V1_5U
                        Find.WindowStack.Add(new Dialog_Confirm("The chosen color has hight transparency and may be hard to use with a solid current hour highlight.", "Use anyway", () => Color_HourHighlight = color));
#else
                        Find.WindowStack.Add(new Dialog_MessageBox("The chosen color has hight transparency and may be hard to use with a solid current hour highlight.", "Use anyway", () => Color_HourHighlight = color, "Cancel"));
#endif
                    }
                    else
                    {
            Log.Message("Made it here 4.1");
                        Find.WindowStack.Add(new Dialog_ColourPicker(color, ChangeCurrentHourHighlightAction()));
#if V1_5U
                        Find.WindowStack.Add(new Dialog_Confirm("The chosen color has hight transparency and may be hard to use with a solid current hour highlight.", "Use anyway", () =>
                                 {
                                     Find.WindowStack.TryGetWindow<Dialog_ColourPicker>(out var picker);
                                     picker?.Close();
                                     Color_HourHighlight = color;
                     }));
#else
                        Find.WindowStack.Add(new Dialog_MessageBox("The chosen color has hight transparency and may be hard to use with a solid current hour highlight.", "Use anyway", () =>
                                 {
                                     Find.WindowStack.WindowOfType<Dialog_ColourPicker>()?.Close();
                                     //picker?.Close();
                                     Color_HourHighlight = color;
                                 }, "Cancel"));
#endif
                    }
                }
                else
                {
                    Log.Message("Made it here 2.1");
                    Color_HourHighlight = color;
                }
                
                    Log.Message("Made it here 5");

            };
        }
        private Action<Color, bool> ChangeCurrentVolacanicColorAction()
        {
            return (color, isClosing) =>
            {
                if (color.a > 0.5f && DrawIncidentOverlay)
                {

                    if (!isClosing)
                    {
#if V1_5U
                        Find.WindowStack.Add(new Dialog_Confirm("The chosen color has hight transparency and may be hard to see the current hour's daylight.", "Use anyway", () => Color_VolcanicWinter = color));
#else
                        Find.WindowStack.Add(new Dialog_MessageBox("The chosen color has hight transparency and may be hard to see the current hour's daylight.", "Use anyway", () => Color_VolcanicWinter = color, "Cancel"));
#endif
                    }
                    else
                    {
                        Find.WindowStack.Add(new Dialog_ColourPicker(color, ChangeCurrentVolacanicColorAction()));

#if V1_5U
                        Find.WindowStack.Add(new Dialog_Confirm("The chosen color has hight transparency and may be hard to see the current hour's daylight.", "Use anyway", () =>
                        {
                            Find.WindowStack.TryGetWindow<Dialog_ColourPicker>(out var picker);
                            picker?.Close();
                            Color_VolcanicWinter = color;
                        }));
         
#else

                        Find.WindowStack.Add(new Dialog_MessageBox("The chosen color has hight transparency and may be hard to see the current hour's daylight.", "Use anyway", () =>
                        {
                            Find.WindowStack.WindowOfType<Dialog_ColourPicker>()?.Close();
                            Color_VolcanicWinter = color;
                        }, "Cancel"));
#endif
                    }
                }
                else
                {
                    Color_VolcanicWinter = color;
                }

            };
        }
        private Action<Color, bool> ChangeCurrentToxicColorAction()
        {
            return (color, isClosing) =>
            {
                if (color.a > 0.5f && DrawIncidentOverlay)
                {

                    if (!isClosing)
                    {
#if V1_5U
                        Find.WindowStack.Add(new Dialog_Confirm("The chosen color has hight transparency and may be hard to see the current hour's daylight.", "Use anyway", () => ToxicFalloutColor = color));
#else
                        Find.WindowStack.Add(new Dialog_MessageBox("The chosen color has hight transparency and may be hard to see the current hour's daylight.", "Use anyway", () => ToxicFalloutColor = color, "Cancel"));
#endif
                    }
                    else
                    {
                        Find.WindowStack.Add(new Dialog_ColourPicker(color, ChangeCurrentToxicColorAction()));
#if V1_5U
             
             Find.WindowStack.Add(new Dialog_Confirm("The chosen color has hight transparency and may be hard to see the current hour's daylight.", "Use anyway", () =>
                        {
                            Find.WindowStack.TryGetWindow<Dialog_ColourPicker>(out var picker);
                            picker?.Close();
                            ToxicFalloutColor = color;
                        }));
#else
             Find.WindowStack.Add(new Dialog_MessageBox("The chosen color has hight transparency and may be hard to see the current hour's daylight.", "Use anyway", () =>
                        {
                            Find.WindowStack.WindowOfType<Dialog_ColourPicker>()?.Close();
                            ToxicFalloutColor = color;
                        }, "Cancel"));
#endif
                        
                    }
                }
                else
                {
                    ToxicFalloutColor = color;
                }

            };
        }
#endregion
        private static void DoColorPickButton(Listing_Standard listingStandard, Color color, Action<Color, bool> colorChangeOperation, string buttonText)
        {
            //listingStandard.Gap(10f);
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

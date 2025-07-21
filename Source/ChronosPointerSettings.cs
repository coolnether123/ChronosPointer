// START OF FILE ChronosPointerSettings.cs
using ColourPicker;
using Mono.Security.Protocol.Ntlm;
using RimWorld;
using System;
//using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace ChronosPointer
{
    public static class Defaults
    {
        //bools
        public static bool DrawArrow = true;
        public static bool DrawCurrentHourHighlight = true;
        public static bool DrawHourBar = true;
        public static bool DrawHoursBarCursor = true;
        public static bool DoDynamicHoursBarLine = true;
        public static bool DrawMainCursor = true;
        public static bool DoFilledHourHighlight = false;
        public static bool DrawIncidentOverlay = true;
        public static bool DoLoadWarnings = true;

        // Floats    
        public static float CursorThickness = 2; // Default thickness
        public static float HoursBarCursorThickness = 2; // Default thickness
        public static float AuroraMinOpacity = 0.1f;
        public static float AuroraMaxOpacity = 0.75f;
        public static float SunlightThreshold_Night = 0.0f;
        public static float _SunlightThreshold_Any = 0.05f;
        public static float SunlightThreshold_DawnDusk = 0.35f;
        public static float SunlightThreshold_SunriseSunset = 0.7f;

        // Ints       
        public static float HighlightBorderThickness = 2; // Default thickness

        // Colors    
        public static Color Color_Arrow = Color.red;
        public static Color Color_MainCursor = Color.white;
        public static Color Color_HourHighlight = new Color(0.737f, 0.737f, 0.114f, 0.812f);
        public static Color Color_HoursBarCursor_Day = Color.black;
        public static Color Color_HoursBarCursor_Night = Color.white;
        public static Color Color_Night = new Color(0f, 0f, 0.5f); //Deep blue
        public static Color Color_DawnDusk = new Color(0.5f, 0.5f, 1f); // Light Blue
        public static Color Color_SunriseSunset = new Color(1f, 0.5f, 0f);   // Orange;
        public static Color Color_Day = Color.yellow;
        public static Color Color_VolcanicWinter = new Color(0f, 0f, 0f, 0.5f);
        public static Color Color_ToxicFallout = new Color(0f, 1f, 0f, 0.5f);
        public static Color Color_Aurora1 = new Color(1.0f, 0.5f, 1.0f, 1.0f);
        public static Color Color_Aurora2 = new Color(0.5f, 1.0f, 0.5f, 1.0f);

        public static Color _DefaultTransparentColor = new Color(1, 1, 1, 0);
        public static Color _HighlightInteriorColor = new Color(0f, 0f, 0f, 0f);


    }

    public class ChronosPointerSettings : ModSettings
    {


        // Method to ensure cursorThickness is always an even number
        public static int ValidateCursorThickness(ref float cursor)
        {
            if (cursor % 2 != 0)
            {
                cursor += 1; // Adjust to the next even number
            }
            return (int)cursor;
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
                    DrawIncidentOverlay = value; // Disable incident overlay if bar is disabled
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
                if (!value)
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
        public Color Color_HourHighlight = new Color(0.737f, 0.737f, 0.114f, 0.812f);
        public Color Color_MainCursor = Color.white;
        public Color Color_HoursBarCursor_Day = Color.black;
        public Color Color_HoursBarCursor_Night = Color.white;
        public Color Color_Night = new Color(0f, 0f, 0.5f); //Deep blue
        public Color Color_DawnDusk = new Color(0.5f, 0.5f, 1f); // Light Blue
        public Color Color_SunriseSunset = new Color(1f, 0.5f, 0f);   // Orange;
        public Color Color_Day = Color.yellow;
        public Color Color_VolcanicWinter = new Color(0f, 0f, 0f, 0.5f);
        public Color Color_ToxicFallout = new Color(0f, 1f, 0f, 0.5f);
        public Color Color_Aurora1 = new Color(1.0f, 0.5f, 1.0f, 1.0f);
        public Color Color_Aurora2 = new Color(0.5f, 1.0f, 0.5f, 1.0f);

        public Color _DefaultTransparentColor = new Color(1, 1, 1, 0);
        public Color _HighlightInteriorColor = new Color(0f, 0f, 0f, 0f);

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref DrawArrow, "DrawArrow", Defaults.DrawArrow);
            Scribe_Values.Look(ref DrawCurrentHourHighlight, "DrawHighlight", Defaults.DrawCurrentHourHighlight);
            Scribe_Values.Look(ref DrawHourBar, "DrawHourBar", Defaults.DrawHourBar);
            Scribe_Values.Look(ref DrawHoursBarCursor, "DrawHoursBarCursor", Defaults.DrawHoursBarCursor);
            Scribe_Values.Look(ref DoDynamicHoursBarLine, "DrawDynamicTimeTraceLine", Defaults.DoDynamicHoursBarLine);
            Scribe_Values.Look(ref DrawMainCursor, "DrawMainCursor", Defaults.DrawMainCursor);
            Scribe_Values.Look(ref DoFilledHourHighlight, "DoFilledHourHighlight", Defaults.DoFilledHourHighlight);
            Scribe_Values.Look(ref DrawIncidentOverlay, "DrawIncidentOverlay", Defaults.DrawIncidentOverlay);
            Scribe_Values.Look(ref DoLoadWarnings, "DoLoadWarnings", Defaults.DoLoadWarnings);

            Scribe_Values.Look(ref Color_Arrow, "Color_Arrow", Defaults.Color_Arrow);
            Scribe_Values.Look(ref Color_HourHighlight, "Color_Highlight", Defaults.Color_HourHighlight);
            Scribe_Values.Look(ref Color_MainCursor, "Color_MainCursor", Defaults.Color_MainCursor);
            Scribe_Values.Look(ref Color_HoursBarCursor_Day, "Color_HoursBarCursor_Day", Defaults.Color_HoursBarCursor_Day);
            Scribe_Values.Look(ref Color_HoursBarCursor_Night, "Color_HoursBarCursor_Night", Defaults.Color_HoursBarCursor_Night);
            Scribe_Values.Look(ref Color_Night, "Color_Night", Defaults.Color_Night); //Deep blue
            Scribe_Values.Look(ref Color_DawnDusk, "Color_DawnDusk", Defaults.Color_DawnDusk); // Light Blue
            Scribe_Values.Look(ref Color_SunriseSunset, "Color_SunriseSunset", Defaults.Color_SunriseSunset);   // Orange;
            Scribe_Values.Look(ref Color_Day, "Color_Day", Defaults.Color_Day);
            Scribe_Values.Look(ref Color_VolcanicWinter, "Color_VolcanicWinter", Defaults.Color_VolcanicWinter);
            Scribe_Values.Look(ref Color_ToxicFallout, "ToxicFalloutColor", Defaults.Color_ToxicFallout);
            Scribe_Values.Look(ref Color_Aurora1, "AuroraColor1", Defaults.Color_Aurora1);
            Scribe_Values.Look(ref Color_Aurora2, "AuroraColor2", Defaults.Color_Aurora2);

            float writeCursorThickness = CursorThickness;
            float writeHoursBarThickness = HoursBarCursorThickness;
            float writeHighlightBorderThickness = HighlightBorderThickness;

            Scribe_Values.Look(ref writeCursorThickness, "CursorThickness", 2f); // Default thickn
            Scribe_Values.Look(ref writeHoursBarThickness, "HoursBarThickness", 2f); // Def
            Scribe_Values.Look(ref writeHighlightBorderThickness, "HighlightBorderThickness", 0.7f);
            CursorThickness = (int)Mathf.Floor(writeCursorThickness);
            HoursBarCursorThickness = (int)Mathf.Floor(writeHoursBarThickness);
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
            DrawArrow = Defaults.DrawArrow;
            DrawCurrentHourHighlight = Defaults.DrawCurrentHourHighlight;
            DrawHourBar = Defaults.DrawHourBar;
            DrawHoursBarCursor = Defaults.DrawHoursBarCursor;
            DoDynamicHoursBarLine = Defaults.DoDynamicHoursBarLine;
            DrawMainCursor = Defaults.DrawMainCursor;
            DoFilledHourHighlight = Defaults.DoFilledHourHighlight;
            DrawIncidentOverlay = Defaults.DrawIncidentOverlay;
            //DoLoadWarnings = true; Don't reset because the user probably doesn't want to see the warnings again after they have been disabled.

            CursorThickness = Defaults.CursorThickness; // Default thickness
            HoursBarCursorThickness = Defaults.HoursBarCursorThickness; // Default thickness
            AuroraMinOpacity = Defaults.AuroraMinOpacity;
            AuroraMaxOpacity = Defaults.AuroraMaxOpacity;
            SunlightThreshold_Night = Defaults.SunlightThreshold_Night;
            _SunlightThreshold_Any = Defaults._SunlightThreshold_Any;
            SunlightThreshold_DawnDusk = Defaults.SunlightThreshold_DawnDusk;
            SunlightThreshold_SunriseSunset = Defaults.SunlightThreshold_SunriseSunset;
            HighlightBorderThickness = Defaults.HighlightBorderThickness; // Default thickness


            Color_Arrow = Defaults.Color_Arrow;
            Color_HourHighlight = Defaults.Color_HourHighlight;
            Color_MainCursor = Defaults.Color_MainCursor;
            Color_HoursBarCursor_Day = Defaults.Color_HoursBarCursor_Day;
            Color_HoursBarCursor_Night = Defaults.Color_HoursBarCursor_Night;
            Color_Night = Defaults.Color_Night; //Deep blue
            Color_DawnDusk = Defaults.Color_DawnDusk; // Light Blue
            Color_SunriseSunset = Defaults.Color_SunriseSunset;   // Orange;
            Color_Day = Defaults.Color_Day;
            Color_VolcanicWinter = Defaults.Color_VolcanicWinter;
            Color_ToxicFallout = Defaults.Color_ToxicFallout;
            Color_Aurora1 = Defaults.Color_Aurora1;
            Color_Aurora2 = Defaults.Color_Aurora2;

            _DefaultTransparentColor = Defaults._DefaultTransparentColor;
            _HighlightInteriorColor = Defaults._HighlightInteriorColor;

            // Validate cursorThickness after resetting
            ValidateCursorThickness(ref CursorThickness);
            ValidateCursorThickness(ref HoursBarCursorThickness);
            Write();
        }



        public void DoWindowContents(Rect inRect)
        {
            Rect buttonRect = new Rect(inRect.width - 150f - 29f, 0f, 150f, 30f);
            if (Widgets.ButtonText(buttonRect, "Reset Defaults"))
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
            listL.CheckboxLabeled("- Do Incident Special Effects", ref DrawIncidentOverlay, tooltip: "Whether to display incident effects. The Hours Bar has special effects during certain incidents.)");

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
            listL.CheckboxLabeled("Show Warnings on Load", ref DoLoadWarnings, tooltip: "Whether to show mod conflict warnins on startup.");
            GUI.color = Color.white;

            listL.Gap();
            GrayIfInactive(DrawMainCursor || DrawHoursBarCursor || DrawCurrentHourHighlight);
            listL.Label($"Cursor thicknesses:");
            Text.Font = GameFont.Tiny;
            GrayIfInactive(DrawMainCursor);
#if !V1_3
            CursorThickness = (int)listL.SliderLabeled($"- Main Cursor ({CursorThickness:F1})", ValidateCursorThickness(ref CursorThickness), 2f, 10f, tooltip: "Thickness of the Main Cursor.");
            GrayIfInactive(DrawHoursBarCursor);
            HoursBarCursorThickness = (int)listL.SliderLabeled($"- Hours Bar Cursor ({HoursBarCursorThickness:F1})", ValidateCursorThickness(ref HoursBarCursorThickness), 2f, 10f, tooltip: "Thickness of the Hours Bar cursor.");
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
            SunlightThreshold_Night = listL.SliderLabeled($"- Night ({SunlightThreshold_Night:F2})", SunlightThreshold_Night, 0.0f, 1.0f, tooltip: "How dark the map has to be to show the night color.");
            //_SunlightThreshold_Any           = listL.SliderLabeled($"Any", _SunlightThreshold_Any, 0.0f, 1.0f, tooltip: "How light the map has to be to show the  color."); //Don't change the any sunlight threshold.
            SunlightThreshold_DawnDusk = listL.SliderLabeled($"- Dawn/Dusk ({SunlightThreshold_DawnDusk:F2})", SunlightThreshold_DawnDusk, 0.0f, 1.0f, tooltip: "How dark the map has to be to show the dawn/dusk color.");
            SunlightThreshold_SunriseSunset = listL.SliderLabeled($"- Sunrise/Sunset ({SunlightThreshold_SunriseSunset:F2})", SunlightThreshold_SunriseSunset, 0.0f, 1.0f, tooltip: "How dark the map has to be to show the sunrise/sunset color.");
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
            AuroraMinOpacity = listL.SliderLabeled($"- Aurora Min Opacity ({AuroraMinOpacity:F2})", AuroraMinOpacity, 0.0f, 1.0f, tooltip: "The minimum transparancy the aurora effect gets.");
            GrayIfInactive(DrawIncidentOverlay);
            AuroraMaxOpacity = listL.SliderLabeled($"- Aurora Max Opacity ({AuroraMaxOpacity:F2})", AuroraMaxOpacity, 0.0f, 1.0f, tooltip: "The maximum transparancy the aurora effect gets.");
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
            DoColorPickButton(listC, Color_Arrow, (newColor, isClosing) => { Color_Arrow = newColor; }, "Arrow Color", nameof(Color_Arrow));

            GrayIfInactive(DrawMainCursor);
            DoColorPickButton(listC, Color_MainCursor, (newColor, isClosing) => { Color_MainCursor = newColor; }, "Cursor Color", nameof(Color_MainCursor));

            GrayIfInactive(DrawCurrentHourHighlight);
            DoColorPickButton(listC, Color_HourHighlight, ChangeCurrentHourHighlightAction(), "Current-hour color", nameof(Color_HourHighlight));

            GrayIfInactive(DrawHourBar);
            listC.Label("Hour Bar Cursor Colors");
            DoColorPickButton(listC, Color_HoursBarCursor_Day, (color, b) => Color_HoursBarCursor_Day = color, !DoDynamicHoursBarLine ? "Time Trace Color" : "Time Trace Color Day", nameof(Color_HoursBarCursor_Day));

            GrayIfInactive(DoDynamicHoursBarLine);
            DoColorPickButton(listC, Color_HoursBarCursor_Night, (color, b) => Color_HoursBarCursor_Night = color, "Time Trace Color Night", nameof(Color_HoursBarCursor_Night));

            GrayIfInactive(DrawHourBar);
            listC.Label("Hour Bar Colors");
            DoColorPickButton(listC, Color_Night, (color, b) => Color_Night = color, "Night Color", nameof(Color_Night));
            DoColorPickButton(listC, Color_DawnDusk, (color, b) => Color_DawnDusk = color, "Dawn/Dusk Color", nameof(Color_DawnDusk));
            DoColorPickButton(listC, Color_SunriseSunset, (color, b) => Color_SunriseSunset = color, "Sunrise/Sunset Color", nameof(Color_SunriseSunset));
            DoColorPickButton(listC, Color_Day, (color, b) => Color_Day = color, "Day Color", nameof(Color_Day));

            GUI.color = Color.white;

            listC.End();

            var listR = new Listing_Standard();

            listR.Begin(right);

            listR.Label("Incident Overlay Colors");
            GrayIfInactive(DrawIncidentOverlay);
            DoColorPickButton(listR, Color_ToxicFallout, ChangeCurrentToxicColorAction(), "Toxic Fallout Color", nameof(Color_ToxicFallout));
            DoColorPickButton(listR, Color_VolcanicWinter, ChangeCurrentVolacanicColorAction(), "Volcanic Winter Color", nameof(Color_VolcanicWinter));
            DoColorPickButton(listR, Color_Aurora1, (color, b) => Color_Aurora1 = color, "Aurora Color 1", nameof(Color_Aurora1));
            DoColorPickButton(listR, Color_Aurora2, (color, b) => Color_Aurora2 = color, "Aurora Color 2", nameof(Color_Aurora2));
            GUI.color = Color.white;

            listR.End();


        }

#if V1_3
        public static float Do1_3LabeledSlider(string label, Listing_Standard list, ref float value, float min, float max, bool validateThickness = false)
        {
            return Widgets.HorizontalSlider(list.Label(label).RightHalf(), validateThickness ? (float)ValidateCursorThickness(ref value) : value, min, max);
        }
#endif

        private static void DoShowScheduleWindowButton(Rect buttonRect)
        {
            if (Current.Game != null)
            {
                GUI.color = Color.green;
                if (Widgets.ButtonText(buttonRect, "Incident Preview..."))
                {
                    var fakeSchedule = Find.MainButtonsRoot.allButtonsInOrder.FirstOrDefault((MainButtonDef button) => button.TabWindow is MainTabWindow_Schedule)?.TabWindow;
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
                Widgets.ButtonText(buttonRect, "Incident Preview...");
            }
            GUI.color = Color.white;
        }

        #region This is completely wet (opposite of D.R.Y.) but I'm too dumb to figure out how to apply ref to a lambda expression (not allowed but there's workarounds) without completely ripping out the code and starting over.
        private Action<Color, bool> ChangeCurrentHourHighlightAction()
        {
            return (color, isClosing) =>
            {
                if (isClosing && color.a > 0.3f && DoFilledHourHighlight)
                {
#if V1_5U
                    Find.WindowStack.Add(new Dialog_Confirm("The chosen color has high transparency and may be hard to use with a solid current hour highlight.", "Use anyway", () => Color_HourHighlight = color, "Cancel"));
#else
                    Find.WindowStack.Add(new Dialog_MessageBox("The chosen color has high transparency and may be hard to use with a solid current hour highlight.", "Use anyway", () => Color_HourHighlight = color, "Cancel"));
#endif
                }
                else
                {
                    Color_HourHighlight = color;
                }

            };
        }
        private Action<Color, bool> ChangeCurrentVolacanicColorAction()
        {
            return (color, isClosing) =>
            {
                if (isClosing && color.a > 0.5f && DrawIncidentOverlay)
                {
#if V1_5U
                    Find.WindowStack.Add(new Dialog_Confirm("The chosen color has high transparency and may be hard to see the current hour's daylight.", "Use anyway", () => Color_VolcanicWinter = color, "Cancel"));
#else
                    Find.WindowStack.Add(new Dialog_MessageBox("The chosen color has high transparency and may be hard to see the current hour's daylight.", "Use anyway", () => Color_VolcanicWinter = color, "Cancel"));
#endif
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
                if (isClosing && color.a > 0.5f && DrawIncidentOverlay)
                {
#if V1_5U
                    Find.WindowStack.Add(new Dialog_Confirm("The chosen color has high transparency and may be hard to see the current hour's daylight.", "Use anyway", () => Color_ToxicFallout = color, "Cancel"));
#else
                    Find.WindowStack.Add(new Dialog_MessageBox("The chosen color has high transparency and may be hard to see the current hour's daylight.", "Use anyway", () => Color_ToxicFallout = color, "Cancel"));
#endif
                }
                else
                {
                    Color_ToxicFallout = color;
                }

            };
        }
        #endregion
        private static void DoColorPickButton(Listing_Standard listingStandard, Color color, Action<Color, bool> colorChangeOperation, string buttonText, string settingFieldName)
        {
            Widgets.DrawBoxSolid(listingStandard.GetRect(10), color);
            if (listingStandard.ButtonText(buttonText))
            {
                // If not in a game, or can't find schedule, show normal picker.
                if (Current.Game == null || Find.CurrentMap == null)
                {
                    Find.WindowStack.Add(new Dialog_ColourPicker(color, colorChangeOperation));
                    return;
                }

                var scheduleWindow = Find.MainButtonsRoot.allButtonsInOrder.FirstOrDefault(b => b.TabWindow is MainTabWindow_Schedule)?.TabWindow;
                if (scheduleWindow == null)
                {
                    Find.WindowStack.Add(new Dialog_ColourPicker(color, colorChangeOperation));
                    return;
                }

                // --- Live Preview Logic ---
                var originalColor = color;
                var fieldInfo = typeof(ChronosPointerSettings).GetField(settingFieldName, BindingFlags.Public | BindingFlags.Instance);
                if (fieldInfo == null)
                {
                    Log.Error($"[ChronosPointer] Could not find setting field '{settingFieldName}' for live preview.");
                    Find.WindowStack.Add(new Dialog_ColourPicker(color, colorChangeOperation));
                    return;
                }

                // --- Temporary Incident Simulation Logic ---
                Action incidentCleanupAction = null;
                switch (settingFieldName)
                {
                    case nameof(Color_ToxicFallout):
                        bool originalTF = Patch_ScheduleWindow.overrideIsToxicFallout;
                        Patch_ScheduleWindow.overrideIsToxicFallout = true;
                        incidentCleanupAction = () => { Patch_ScheduleWindow.overrideIsToxicFallout = originalTF; };
                        break;
                    case nameof(Color_VolcanicWinter):
                        bool originalVW = Patch_ScheduleWindow.overrideIsVolcanicWinter;
                        Patch_ScheduleWindow.overrideIsVolcanicWinter = true;
                        incidentCleanupAction = () => { Patch_ScheduleWindow.overrideIsVolcanicWinter = originalVW; };
                        break;
                    case nameof(Color_Aurora1):
                    case nameof(Color_Aurora2):
                        bool originalA = Patch_ScheduleWindow.overrideIsAurora;
                        Patch_ScheduleWindow.overrideIsAurora = true;
                        incidentCleanupAction = () => { Patch_ScheduleWindow.overrideIsAurora = originalA; };
                        break;
                }

                // --- Setup Actions for the Live Preview Window ---
                Action onCancel = () =>
                {
                    fieldInfo.SetValue(ChronosPointerMod.Settings, originalColor);
                };

                Action onPostClose = () =>
                {
                    Find.WindowStack.TryRemove(scheduleWindow);
                    incidentCleanupAction?.Invoke();
                };

                // Open the schedule window as a non-modal sub-window
                scheduleWindow.layer = WindowLayer.SubSuper;
                Find.WindowStack.Add(scheduleWindow);

                // Create and add our new custom preview dialog
                var previewDialog = new Dialog_LivePreview(color, colorChangeOperation, onCancel, onPostClose);
                Find.WindowStack.Add(previewDialog);
            }
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
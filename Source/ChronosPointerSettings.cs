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
        public bool enableArrow = true;
        public bool showHighlight = true;
        public bool showDayNightBar = true;
        public bool showDayNightIndicator = true;
        public bool useDynamicTimeTraceLine = true;
        public bool showPawnLine = true;
        public bool hollowHourHighlight = false;
        public bool doIncidentSpecials = true;
        public bool doLoadWarnings = true;

        // Floats
        public float cursorThickness = 2f; // Default thickness
        public float dayNightBarCursorThickness = 2f; // Default thickness

        // Colors
        public Color arrowColor = Color.red;
        public Color highlightColor = new Color(1f, 1f, 0f, 0.3f);
        public Color bottomCursorColor = Color.white;
        public Color timeTraceColorDay = Color.black;
        public Color timeTraceColorNight = Color.white;
        public Color nightColor = new Color(0f, 0f, 0.5f); //Deep blue
        public Color dawnDuskColor = new Color(0.5f, 0.5f, 1f); // Light Blue
        public Color sunriseSunsetColor = new Color(1f, 0.5f, 0f);   // Orange;
        public Color dayColor = Color.yellow;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref enableArrow, "enableArrow", true);
            Scribe_Values.Look(ref showHighlight, "showHighlight", true);
            Scribe_Values.Look(ref showDayNightBar, "showDayNightBar", true);
            Scribe_Values.Look(ref showDayNightIndicator, "showTimeTraceLine", true);
            Scribe_Values.Look(ref useDynamicTimeTraceLine, "dynamicTimeTraceLine", true);
            Scribe_Values.Look(ref showPawnLine, "showPawnLine", true);
            Scribe_Values.Look(ref hollowHourHighlight, "hollowHourHighlight", false);
            Scribe_Values.Look(ref doIncidentSpecials, "doIncidentSpecials", false);
            Scribe_Values.Look(ref doLoadWarnings, "doLoadWarnings", true);

            Scribe_Values.Look(ref arrowColor, "arrowColor", Color.red);
            Scribe_Values.Look(ref highlightColor, "highlightColor", new Color(1f, 1f, 0f, 0.3f));
            Scribe_Values.Look(ref bottomCursorColor, "bottomCursorColor", Color.white);
            Scribe_Values.Look(ref timeTraceColorDay, "timeTraceColorDay", Color.black);
            Scribe_Values.Look(ref timeTraceColorNight, "timeTraceColorNight", Color.white);

            Scribe_Values.Look(ref nightColor, "nightColor", new Color(0f, 0f, 0.5f));
            Scribe_Values.Look(ref dawnDuskColor, "dawnDuskColor", new Color(0.5f, 0.5f, 1f));
            Scribe_Values.Look(ref sunriseSunsetColor, "sunriseSunsetColor", new Color(1f, 0.5f, 0f));
            Scribe_Values.Look(ref dayColor, "dayColor", Color.yellow);



            Scribe_Values.Look(ref cursorThickness, "cursorThickness", 2f);
            Scribe_Values.Look(ref dayNightBarCursorThickness, "dayNightBarCursorTickness", 2f);

            // Validate cursorThickness after loading
            ValidateCursorThickness(ref cursorThickness);
            ValidateCursorThickness(ref dayNightBarCursorThickness);
        }
        public void ResetToDefaults()
        {
            enableArrow = true;
            showHighlight = true;
            showDayNightBar = true;
            showDayNightIndicator = true;
            useDynamicTimeTraceLine = true;
            showPawnLine = true;
            hollowHourHighlight = false;
            doIncidentSpecials = true;
            doLoadWarnings = true;


            cursorThickness = 2f; // Default thickness
            dayNightBarCursorThickness = 2f; // Default thickness


            arrowColor = Color.red;
            highlightColor = new Color(1f, 1f, 0f, 0.3f);
            bottomCursorColor = Color.white;
            timeTraceColorDay = Color.black;
            timeTraceColorNight = Color.white;
            nightColor = new Color(0f, 0f, 0.5f); //Deep blue
            dawnDuskColor = new Color(0.5f, 0.5f, 1f); // Light Blue
            sunriseSunsetColor = new Color(1f, 0.5f, 0f);   // Orange;
            dayColor = Color.yellow;

            // Validate cursorThickness after resetting
            ValidateCursorThickness(ref cursorThickness);
            ValidateCursorThickness(ref dayNightBarCursorThickness);

        }
    }
}
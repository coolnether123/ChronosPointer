using UnityEngine;
using Verse;

namespace ChronosPointer
{
    public class ChronosPointerSettings : ModSettings
    {

        public float cursorThickness = 2f; // Default thickness

        // Method to ensure cursorThickness is always an even number
        public void ValidateCursorThickness()
        {
            if (cursorThickness % 2 != 0)
            {
                cursorThickness += 1f; // Adjust to the next even number
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


        // Colors
        public Color arrowColor = Color.red;
        public Color highlightColor = new Color(1f, 1f, 0f, 0.3f);
        public Color bottomCursorColor = Color.white;
        public Color timeTraceColorDay = Color.black;
        public Color timeTraceColorNight = Color.white;

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

            Scribe_Values.Look(ref arrowColor, "arrowColor", Color.red);
            Scribe_Values.Look(ref highlightColor, "highlightColor", new Color(1f, 1f, 0f, 0.3f));
            Scribe_Values.Look(ref bottomCursorColor, "bottomCursorColor", Color.white);
            Scribe_Values.Look(ref timeTraceColorDay, "timeTraceColorDay", Color.black);
            Scribe_Values.Look(ref timeTraceColorNight, "timeTraceColorNight", Color.white);
            Scribe_Values.Look(ref cursorThickness, "cursorThickness", 2f);

            // Validate cursorThickness after loading
            ValidateCursorThickness();
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
            cursorThickness = 2f;

            // Colors
            arrowColor = Color.red;
            highlightColor = new Color(1f, 1f, 0f, 0.3f);
            bottomCursorColor = Color.white;
            timeTraceColorDay = Color.black;
            timeTraceColorNight = Color.white;

            // Validate cursorThickness after resetting
            ValidateCursorThickness();
        }
    }
}
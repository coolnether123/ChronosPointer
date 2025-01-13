using UnityEngine;
using Verse;

namespace ChronosPointer
{
    public class ChronosPointerSettings : ModSettings
    {
        // Toggles
        public bool enableArrow = true;
        public bool showHighlight = true;
        public bool showDayNightBar = true;
        public bool showTimeTraceLine = true;

        // Colors
        public Color arrowColor = Color.grey;
        public Color highlightColor = new Color(1f, 1f, 0f, 0.3f);
        public Color topCursorColor = Color.white;
        public Color bottomCursorColor = Color.white;
        public Color pawnBarColor = Color.cyan;
        public Color dayNightBarColor = Color.blue;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref enableArrow, "enableArrow", true);
            Scribe_Values.Look(ref showHighlight, "showHighlight", true);
            Scribe_Values.Look(ref showDayNightBar, "showDayNightBar", true);
            Scribe_Values.Look(ref showTimeTraceLine, "showTimeTraceLine", true);

            Scribe_Values.Look(ref arrowColor, "arrowColor", Color.red);
            Scribe_Values.Look(ref highlightColor, "highlightColor", new Color(1f, 1f, 0f, 0.3f));
            Scribe_Values.Look(ref topCursorColor, "topCursorColor", Color.white);
            Scribe_Values.Look(ref bottomCursorColor, "bottomCursorColor", Color.white);
            Scribe_Values.Look(ref pawnBarColor, "pawnBarColor", Color.cyan);
            Scribe_Values.Look(ref dayNightBarColor, "dayNightBarColor", Color.blue);
        }

        public void ResetToDefaults()
        {
            enableArrow = true;
            showHighlight = true;
            showDayNightBar = true;
            showTimeTraceLine = true;

            arrowColor = Color.red;
            highlightColor = new Color(1f, 1f, 0f, 0.3f);
            topCursorColor = Color.white;
            bottomCursorColor = Color.white;
            pawnBarColor = Color.cyan;
            dayNightBarColor = Color.blue;
        }
    }
}
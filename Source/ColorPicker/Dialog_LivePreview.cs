using ColourPicker;
using RimWorld;
using System;
using UnityEngine;
using Verse;
using System.Linq;

namespace ChronosPointer
{
    public class Dialog_LivePreview : Window
    {
        private Dialog_ColourPicker colourPicker;
        private Action onCancelAction;
        private Action onPostCloseAction;

        public Dialog_LivePreview(Color initialColor, Action<Color, bool> callback, Action onCancel, Action onPostClose)
        {
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = true;
            this.draggable = false;

            this.onCancelAction = onCancel;
            this.onPostCloseAction = onPostClose;

            // Create an instance of the color picker to "embed" its GUI
            colourPicker = new Dialog_ColourPicker(initialColor, callback)
            {
                autoApply = true,
                // Link the picker's cancel/close buttons to our main dialog's Close() method
                onCancel = () => { this.Close(true); },
            };
            // Manually set curColour for the "Old" color swatch in the picker
            colourPicker.curColour = initialColor;
        }

        public override Vector2 InitialSize => new Vector2(800f, 450f);

        protected override void SetInitialSizeAndPosition()
        {
            base.SetInitialSizeAndPosition();
            Patch_ScheduleWindow.IsInTestMode = true;
        }

        private void DrawIncidentToggle(Listing_Standard listing, string label, ref bool overrideFlag)
        {
            Color originalColor = GUI.color;
            bool isOverridden = overrideFlag || (label == "Aurora" && Patch_ScheduleWindow.isAurora) ||
                                (label == "Eclipse" && Patch_ScheduleWindow.isEclipse) ||
                                (label == "Solar Flare" && Patch_ScheduleWindow.isSolarFlare) ||
                                (label == "Toxic Fallout" && Patch_ScheduleWindow.isToxicFallout) ||
                                (label == "Volcanic Winter" && Patch_ScheduleWindow.isVolcanicWinter);

            if (isOverridden)
            {
                GUI.color = Color.green;
            }

            listing.CheckboxLabeled(label, ref overrideFlag, "Toggle simulation for this incident.");
            GUI.color = originalColor;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect leftRect = new Rect(inRect.x, inRect.y, 200, inRect.height);
            Rect rightRect = new Rect(leftRect.xMax + 20, inRect.y, inRect.width - leftRect.width - 20, inRect.height);

            // --- Left Panel: Incident Toggles ---
            var listing = new Listing_Standard();
            listing.Begin(leftRect);

            Text.Font = GameFont.Medium;
            listing.Label("Simulations");
            listing.Gap(12f);
            Text.Font = GameFont.Small;

            DrawIncidentToggle(listing, "Aurora", ref Patch_ScheduleWindow.overrideIsAurora);
            DrawIncidentToggle(listing, "Eclipse", ref Patch_ScheduleWindow.overrideIsEclipse);
            DrawIncidentToggle(listing, "Solar Flare", ref Patch_ScheduleWindow.overrideIsSolarFlare);
            DrawIncidentToggle(listing, "Toxic Fallout", ref Patch_ScheduleWindow.overrideIsToxicFallout);
            DrawIncidentToggle(listing, "Volcanic Winter", ref Patch_ScheduleWindow.overrideIsVolcanicWinter);

            listing.End();

            // --- Right Panel: Color Picker ---
            // We "embed" the color picker's UI here by calling its drawing method directly.
            colourPicker.DoWindowContents(rightRect);

            // Handle the color picker's internal close requests
            if (colourPicker.WantsToClose)
            {
                // If it wants to close because of "OK", don't run the cancel action
                if (!colourPicker.Accepted)
                {
                    onCancelAction?.Invoke();
                }
                this.Close(false);
            }
        }

        // When Escape is pressed or the window is closed via the 'x'
        public override void OnCancelKeyPressed()
        {
            onCancelAction?.Invoke();
            base.OnCancelKeyPressed();
        }

        // When the user clicks outside the window
        public override void Notify_ClickOutsideWindow()
        {
            onCancelAction?.Invoke();
            base.Notify_ClickOutsideWindow();
        }

        // This is called AFTER the window is removed from the stack, for final cleanup.
        public override void PostClose()
        {
            base.PostClose();
            onPostCloseAction?.Invoke();

            var scheduleWindow = Find.MainButtonsRoot.allButtonsInOrder
                             .FirstOrDefault(b => b.TabWindow is MainTabWindow_Schedule)?.TabWindow;
            if (scheduleWindow != null)
            {
                // Reset the layer back to default so it behaves like a normal tab again.
                scheduleWindow.layer = WindowLayer.GameUI;
            }

            Patch_ScheduleWindow.IsInTestMode = false;
        }
    }
}

using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace ChronosPointer
{
    public class Dialog_IncidentTesting : Dialog_MessageBox
    {
        public Dialog_IncidentTesting(TaggedString text, string buttonAText = null, Action buttonAAction = null, WindowLayer layer = WindowLayer.Dialog) : base(text, buttonAText, buttonAAction, layer: layer)
        {
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(300f, 320f);
            }
        }

        protected override void SetInitialSizeAndPosition()
        {
            base.SetInitialSizeAndPosition();
            windowRect.y -= 100f;
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
            var listing = new Listing_Standard();

            Rect contentRect = inRect;
            contentRect.height -= 40f; // Space for the bottom button

            listing.Begin(contentRect);
            Text.Font = GameFont.Medium;
            listing.Label("Incident Simulation");
            listing.Gap(15f);
            Text.Font = GameFont.Small;

            listing.Label("Toggle incidents on/off for preview:");
            listing.Gap(10f);

            // Directly use checkboxes to toggle the override flags
            DrawIncidentToggle(listing, "Aurora", ref Patch_ScheduleWindow.overrideIsAurora);
            DrawIncidentToggle(listing, "Eclipse", ref Patch_ScheduleWindow.overrideIsEclipse);
            DrawIncidentToggle(listing, "Solar Flare", ref Patch_ScheduleWindow.overrideIsSolarFlare);
            DrawIncidentToggle(listing, "Toxic Fallout", ref Patch_ScheduleWindow.overrideIsToxicFallout);
            DrawIncidentToggle(listing, "Volcanic Winter", ref Patch_ScheduleWindow.overrideIsVolcanicWinter);

            listing.End();

            // Bottom "Done" button
            Rect buttonRect = new Rect(inRect.x, inRect.yMax - 30f, inRect.width, 30f);
            if (Widgets.ButtonText(new Rect(buttonRect.center.x - 75f, buttonRect.y, 150f, buttonRect.height), buttonAText))
            {
                if (buttonAAction != null)
                {
                    buttonAAction();
                }
                Close();
            }
            GUI.color = Color.white;

            if (windowRect.x < 0 || windowRect.xMax > UI.screenWidth || windowRect.y < 0 || windowRect.yMax > UI.screenHeight)
            {
                windowRect.x = Mathf.Clamp(windowRect.x, 0, UI.screenWidth - windowRect.width);
                windowRect.y = Mathf.Clamp(windowRect.y, 0, UI.screenHeight - windowRect.height);
            }
        }


        void CloseAction()
        {
            if (buttonAAction != null)
            {
                buttonAAction();
            }
            Patch_ScheduleWindow.IsInTestMode = false; // Reset test mode when this dialog closes
            Event.current.Use();
        }

        public override void OnCancelKeyPressed()
        {
            base.OnCancelKeyPressed();
            CloseAction();
            Close();
        }

        public override void Notify_ClickOutsideWindow()
        {
            base.Notify_ClickOutsideWindow();
            if (!closeOnClickedOutside)
                return;
            CloseAction();
            Close();
        }

        public override void PostClose()
        {
            CloseAction();
            base.PostClose(); // Keep the base call if it was there.
        }
    }
}
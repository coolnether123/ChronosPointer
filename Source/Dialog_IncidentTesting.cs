using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                return new Vector2(300f, 270f);
            }
        }

        protected override void SetInitialSizeAndPosition()
        {
            base.SetInitialSizeAndPosition();
            windowRect.y -= 100f;
            Patch_ScheduleWindow.IsInTestMode = true;
        }

        string EnableString(bool isEnabled)
        {
            return isEnabled ? "Enable" : "Disable";
        }

        void DoGUIColor(bool green)
        {
            GUI.color = green ? Color.green : Color.red;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Vector2 buttonSize = new Vector2(180f, 60f);

            var listing = new Listing_Standard();

            bool aurora = Patch_ScheduleWindow.AuroraActive;
            bool eclipse = Patch_ScheduleWindow.EclipseActive;
            bool solarFlare = Patch_ScheduleWindow.SolarFlareActive;
            bool toxFallout = Patch_ScheduleWindow.ToxicFalloutActive;
            bool volWinter = Patch_ScheduleWindow.VolcanicWinterActive;

            listing.Begin(inRect);
            Text.Font = GameFont.Medium;
            listing.Label("Preview Changes");
            listing.Gap(15f);
            Text.Font = GameFont.Small;

            DoGUIColor(aurora);
            listing.Label("Aurora: " + EnableString(aurora) + "d");
            DoGUIColor(eclipse);
            listing.Label("Eclipse: " + EnableString(eclipse) + "d");
            DoGUIColor(solarFlare);
            listing.Label("Solar Flare: " + EnableString(solarFlare) + "d");
            DoGUIColor(toxFallout);
            listing.Label("Toxic Fallout: " + EnableString(toxFallout) + "d");
            DoGUIColor(volWinter);
            listing.Label("Volcanic Winter: " + EnableString(volWinter) + "d");

            GUI.color = Color.white;
            listing.Gap(5f);

            if (listing.ButtonText("Test incident"))
            {
                closeOnClickedOutside = false;
                var options = new List<FloatMenuOption>() {
                new FloatMenuOption(EnableString(!aurora)+ " Aurora", () => {Patch_ScheduleWindow.overrideIsAurora = !Patch_ScheduleWindow.overrideIsAurora;}),
                new FloatMenuOption(EnableString(!toxFallout)+" Eclipse", () => {Patch_ScheduleWindow.overrideIsEclipse = !Patch_ScheduleWindow.overrideIsEclipse;  }),
                new FloatMenuOption(EnableString(!toxFallout) + " Solar Flare", () => {Patch_ScheduleWindow.overrideIsSolarFlare = !Patch_ScheduleWindow.overrideIsSolarFlare; }),
                new FloatMenuOption(EnableString(!toxFallout)+ " Toxic Fallout", () => {Patch_ScheduleWindow.overrideIsToxicFallout = !Patch_ScheduleWindow.overrideIsToxicFallout; }),
                new FloatMenuOption(EnableString(!volWinter) + " Volcanic Winter", () => {Patch_ScheduleWindow.overrideIsVolcanicWinter = !Patch_ScheduleWindow.overrideIsVolcanicWinter;  }),
                    };
                Find.WindowStack.Add(new FloatMenu(options));
            }
            closeOnClickedOutside = !Find.WindowStack.Windows.Any(w => w is FloatMenu);

            if (listing.ButtonText(buttonAText))
            {
                if (buttonAAction != null)
                {
                    buttonAAction();
                }
                Close();
            }
            GUI.color = Color.white;
            listing.End();

        }


        void CloseAction()
        {
            if (buttonAAction != null)
            {
                buttonAAction();
            }
            Event.current.Use();
            Patch_ScheduleWindow.overrideIsAurora = false;
            Patch_ScheduleWindow.overrideIsEclipse = false;
            Patch_ScheduleWindow.overrideIsSolarFlare = false;
            Patch_ScheduleWindow.overrideIsToxicFallout = false;
            Patch_ScheduleWindow.overrideIsVolcanicWinter = false;
            Patch_ScheduleWindow.IsInTestMode = false;

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
        }

    }
}

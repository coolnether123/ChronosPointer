#if !V1_0
using ColourPicker;
using RimWorld;
using System;
using UnityEngine;
using Verse;
using System.Linq;
#if V1_1
using Harmony;
using System.Reflection; // Required for manual reflection in 1.1
#else
using HarmonyLib;
#endif

#if V1_2 || V1_1
using MainTabWindow_Schedule = RimWorld.MainTabWindow_Restrict;
#endif

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

            colourPicker = new Dialog_ColourPicker(initialColor, callback)
            {
                autoApply = true,
                onCancel = () => { this.Close(true); },
            };
            colourPicker.curColour = initialColor;
        }

#if V1_0
        public override Vector2 InitialSize { get { return new Vector2(800f, 450f); } }
#else
        public override Vector2 InitialSize => new Vector2(800f, 450f);
#endif

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

            colourPicker.DoWindowContents(rightRect);

            if (colourPicker.WantsToClose)
            {
                if (!colourPicker.Accepted)
                {
#if V1_0
                    if (onCancelAction != null) onCancelAction();
#else
                    onCancelAction?.Invoke();
#endif
                }
                this.Close(false);
            }
        }

        public override void OnCancelKeyPressed()
        {
#if V1_0
            if (onCancelAction != null) onCancelAction();
#else
            onCancelAction?.Invoke();
#endif
            base.OnCancelKeyPressed();
        }

#if !(V1_2 || V1_1 || V1_0)
    public override void Notify_ClickOutsideWindow()
    {
        onCancelAction?.Invoke();
        base.Notify_ClickOutsideWindow();
    }
#endif

        public override void PostClose()
        {
            base.PostClose();
#if V1_0
            if (onPostCloseAction != null) onPostCloseAction();
#else
            onPostCloseAction?.Invoke();
#endif

#if V1_1 || V1_0
            var allButtonsField = Harmony.AccessTools.Field(typeof(MainButtonsRoot), "AllButtons");
            var allButtons = (System.Collections.Generic.List<MainButtonDef>)allButtonsField.GetValue(null);
            var scheduleWindow = allButtons.FirstOrDefault(b => b.TabWindow is MainTabWindow_Restrict)?.TabWindow;
#else
            var scheduleWindow = Find.MainButtonsRoot.allButtonsInOrder
                             .FirstOrDefault(b => b.TabWindow is MainTabWindow_Restrict)?.TabWindow;
#endif
            if (scheduleWindow != null)
            {
                scheduleWindow.layer = WindowLayer.GameUI;
            }

            Patch_ScheduleWindow.IsInTestMode = false;
        }
    }
}
#endif
﻿#if V1_1 || V1_0
using Harmony;
using System.Reflection; // Required for manual reflection in 1.1
#else
using HarmonyLib;
#endif
using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;
#if V1_2 || V1_1 || V1_0
using MainTabWindow_Schedule = RimWorld.MainTabWindow_Restrict;
#endif

namespace ChronosPointer
{
    public class Dialog_IncidentTesting : Dialog_MessageBox
    {
        public Dialog_IncidentTesting(
#if V1_0
        string text,
#else
        TaggedString text,
#endif
        string buttonAText = null, Action buttonAAction = null
#if !(V1_2 || V1_1 || V1_0)
        , WindowLayer layer = WindowLayer.Dialog
#endif
        ) : base(text, buttonAText, buttonAAction
#if !(V1_2 || V1_1 || V1_0)
        , layer: layer
#endif
        )
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

#if V1_1 || V1_0
            // For 1.1, AllButtons is a private static field in MainButtonsRoot. We use reflection to access it.
            var allButtonsFieldInfo = Harmony.AccessTools.Field(typeof(MainButtonsRoot), "AllButtons");
            var allButtons = (System.Collections.Generic.List<MainButtonDef>)allButtonsFieldInfo.GetValue(null);
            var scheduleWindow = allButtons?.FirstOrDefault(b => b.TabWindow is MainTabWindow_Schedule)?.TabWindow;
#else
            var scheduleWindow = Find.MainButtonsRoot.allButtonsInOrder
                            .FirstOrDefault(b => b.TabWindow is MainTabWindow_Schedule)?.TabWindow;
#endif

            if (scheduleWindow != null)
            {
                // Reset the layer back to default.
                scheduleWindow.layer = WindowLayer.GameUI;
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

#if !(V1_2 || V1_1 || V1_0)
    public override void Notify_ClickOutsideWindow()
    {
        base.Notify_ClickOutsideWindow();
        if (!closeOnClickedOutside)
            return;
        CloseAction();
        Close();
    }
#endif

        public override void PostClose()
        {
            CloseAction();
            base.PostClose(); // Keep the base call if it was there.
        }
    }
}
using ColourPicker;
using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace ChronosPointer
{
    [HarmonyPatch(typeof(UIRoot_Entry), "Init")]
    public static class Patch_OnGameLoad
    {
        [HarmonyPostfix]
        public static void UIRootEntryInit_Prefix()
        {
            if (!ChronosPointerMod.Settings.doLoadWarnings)
                return;

            if (ModsConfig.IsActive("Mysterius.CustomSchedules"))
                ApplyFixForMysteriusCustomSchedules();
        }
        private static void ApplyFixForMysteriusCustomSchedules()
        {
            // Implement the fix logic for Mysterius.CustomSchedules
            Find.WindowStack?.Add(new Dialog_MessageBox("Custom Schedules (continued) is Active. Chronos Pointer will have overlap", "OK", null, "Don't show again", () =>
            {
                ChronosPointerMod.Settings.doLoadWarnings = false;
                ChronosPointerMod.Settings?.Write();
            }));

        }

    }

    public class ChronosPointerMod : Mod
    {
        public static ChronosPointerSettings Settings;
        public static float cursorThickness = 2f; // Default thickness
        private Vector2 scrollPosition = Vector2.zero;

        public ChronosPointerMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<ChronosPointerSettings>();
            // Harmony patch
            var harmony = new HarmonyLib.Harmony("com.coolnether123.ChronosPointer");
            harmony.PatchAll();
            Log.Message("[ChronosPointer] Harmony patches applied.");
        }

        public override string SettingsCategory()
        {
            return "Chronos Pointer";
        }

        public override void DoSettingsWindowContents(Rect inRect)
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
        public override void WriteSettings()
        {
            base.WriteSettings();
            Patch_ScheduleWindow.dayNightColorsCalculated = false;
        }

        private static void DoTopSettings(Listing_Standard listingStandard)
        {
            if (listingStandard.ButtonText("Reset to Default"))
            {
                Settings.ResetToDefaults();
            }
            listingStandard.Gap(20f);

            listingStandard.CheckboxLabeled("- Show Arrow", ref Settings.enableArrow);
            listingStandard.CheckboxLabeled("- Show Day/Night Bar", ref Settings.showDayNightBar);

            // Gray out unapplicable settings
            if (!Settings.showDayNightBar) GUI.color = Color.gray;
            listingStandard.CheckboxLabeled("    Show Day/Night Bar Time Indicator", ref Settings.showDayNightIndicator);
            GUI.color = Color.white;

            if (!Settings.showDayNightIndicator || !Settings.showDayNightBar) GUI.color = Color.gray;
            listingStandard.CheckboxLabeled("    Dynamic Day/Night Bar Cursor Color", ref Settings.useDynamicTimeTraceLine);
            GUI.color = Color.white;

            listingStandard.CheckboxLabeled("- Show Pawn Section Time Indicator", ref Settings.showPawnLine);
            listingStandard.CheckboxLabeled("- Show Current Hour Highlight", ref Settings.showHighlight);
            if (!Settings.showHighlight) GUI.color = Color.gray;
            listingStandard.CheckboxLabeled("    Hollow Current Hour Highlight", ref Settings.hollowHourHighlight);
            GUI.color = Color.white;
            listingStandard.CheckboxLabeled("Do Incident Special Effects", ref Settings.doIncidentSpecials);
            listingStandard.CheckboxLabeled("Show Warnings on Load", ref Settings.doLoadWarnings);
            GUI.color = Color.white;
        }



        private static void DoLeftSettings(Rect inRect, Listing_Standard listingStandard)
        {

            if (Current.Game != null)
            {
                GUI.color = Color.green;
                if (listingStandard.ButtonText("Show schedule menu"))
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
                listingStandard.ButtonText("Show schedule menu");
                GUI.color = Color.white;
            }
            listingStandard.Gap(10f);
            // Arrow Color
            GrayIfInactive(Settings.enableArrow);
            DoColorPickButton(listingStandard, Settings.arrowColor, (newColor, isClosing) => { Settings.arrowColor = newColor; }, "Change Arrow Color");

            listingStandard.Gap(4f);

            // Pawn cursor
            GrayIfInactive(Settings.showPawnLine);
            listingStandard.Label($"Pawn Section Time Indicator Thickness: {Settings.cursorThickness:F1}");
            float newThickness = listingStandard.Slider(Settings.cursorThickness, 2f, 10f);
            if (Settings.showPawnLine)
            {
                Settings.cursorThickness = Mathf.Round(newThickness / 2f) * 2f; // Round to nearest even number
            }
            listingStandard.Gap(4f);

            GrayIfInactive(Settings.showPawnLine);
            DoColorPickButton(listingStandard, Settings.bottomCursorColor, (newColor, isClosing) => { Settings.bottomCursorColor = newColor; }, "Change Pawn Section Time Indicator Color");

            // Current hour highlight
            GrayIfInactive(Settings.showHighlight);
            GrayIfInactive(Settings.showDayNightIndicator);
            listingStandard.Label($"Pawn Section Time Indicator Thickness: {Settings.dayNightBarCursorThickness:F1}");
            float secondNewThickness = listingStandard.Slider(Settings.dayNightBarCursorThickness, 2f, 10f);
            if (Settings.showDayNightIndicator)
            {
                Settings.dayNightBarCursorThickness = Mathf.Round(secondNewThickness / 2f) * 2f; // Round to nearest even number
            }
            DoColorPickButton(listingStandard, Settings.highlightColor, ChangeCurrentHourHighlightAction(), "Change current-hour color");
            GUI.color = Color.white;

        }
        private static void DoRightSettings(Listing_Standard listingStandard)
        {


            // Day/Night bar day and night colors
            GrayIfInactive(Settings.showDayNightBar);
            GrayIfInactive(Settings.showDayNightBar && Settings.showDayNightIndicator);
            // Day/Default
            DoColorPickButton(listingStandard, Settings.timeTraceColorDay, (color, b) => Settings.timeTraceColorDay = color, "Change Time Trace Color" + (Settings.useDynamicTimeTraceLine ? " Day" : ""));
            // Night
            GrayIfInactive(Settings.showDayNightBar && Settings.showDayNightIndicator && Settings.useDynamicTimeTraceLine);
            DoColorPickButton(listingStandard, Settings.timeTraceColorNight, (color, b) => Settings.timeTraceColorNight = color, "Change Time Trace Color Night");

            GrayIfInactive(Settings.showDayNightBar);
            DoColorPickButton(listingStandard, Settings.nightColor, (color, b) => Settings.nightColor = color, "Change Night Color");
            DoColorPickButton(listingStandard, Settings.dawnDuskColor, (color, b) => Settings.dawnDuskColor = color, "Change Dawn/Dusk Color");
            DoColorPickButton(listingStandard, Settings.sunriseSunsetColor, (color, b) => Settings.sunriseSunsetColor = color, "Change Sunrise/Sunset Color");
            DoColorPickButton(listingStandard, Settings.dayColor, (color, b) => Settings.dayColor = color, "Change Day Color");


        }
        private static Action<Color, bool> ChangeCurrentHourHighlightAction()
        {
            return (color, isClosing) =>
            {
                if (color.a > 0.3f && !Settings.hollowHourHighlight)
                {

                    if (!isClosing)
                    {
                        Find.WindowStack.Add(new Dialog_Confirm("The chosen color has low transparency and may be hard to use with a solid current hour highlight.", "Use anyway", () => Settings.highlightColor = color));
                    }
                    else
                    {
                        Find.WindowStack.Add(new Dialog_ColourPicker(color, ChangeCurrentHourHighlightAction()));
                        Find.WindowStack.Add(new Dialog_Confirm("The chosen color has low transparency and may be hard to use with a solid current hour highlight.", "Use anyway", () =>
                        {
                            Find.WindowStack.TryGetWindow<Dialog_ColourPicker>(out var picker);
                            picker?.Close();
                            Settings.highlightColor = color;
                        }));
                    }
                }
                else
                {
                    Settings.highlightColor = color;
                }

            };
        }

        private static void DoColorPickButton(Listing_Standard listingStandard, Color color, Action<Color, bool> colorChangeOperation, string buttonText)
        {
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
            if(!closeOnClickedOutside)
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
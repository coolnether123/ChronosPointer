using ColourPicker;
using ChronosPointer.Api;
using HarmonyLib;
using RimWorld;
using System;
using UnityEngine;
using Verse;
#if CHRONOS_POINTER_USE_SPINE
using Spine.Api;
using Spine.UI.SettingsFramework;
#endif

namespace ChronosPointer
{

    class Dialog_ModWarning : Dialog_MessageBox
    {
        public Dialog_ModWarning(string title, TaggedString text, Action fixAction = null, string fixActionText = "Disable Overlap", WindowLayer layer = WindowLayer.Dialog) : base(text, fixActionText, fixAction, title: title, layer: layer)
        {
            if (fixAction != null)
            {
                buttonCAction = fixAction;
                buttonCText = fixActionText;
            }
            buttonAText = "OK";
            buttonAAction = null;
            buttonBText = "Disable Warnings";
            buttonBAction = () =>
            {
                ChronosPointerMod.Settings.DoLoadWarnings = false;
                ChronosPointerMod.Settings?.Write();
            };


        }
    }

    [HarmonyPatch(typeof(UIRoot_Entry), "Init")]
    public static class Patch_OnGameLoad
    {
        public static bool playerWarned = false;
        [HarmonyPostfix]
        public static void UIRootEntryInit_Prefix()
        {
            if (!ChronosPointerMod.Settings.DoLoadWarnings || playerWarned)
                return;

            if (ModsConfig.IsActive("Mysterius.CustomSchedules") && (ChronosPointerMod.Settings != null ? ChronosPointerMod.Settings.DrawHourBar : true))
                ApplyFixForMysteriusCustomSchedules();
            if (ModsConfig.IsActive("rswallen.scheduleclock") && (ChronosPointerMod.Settings != null ? ChronosPointerMod.Settings.DrawMainCursor : true))
                ApplyFixForScheduleClock();
            //Sumarbrander to CoolNether123: When you do your lining up, please make this so it only appears if grouped pawns has "Restrict" enabled. Probably check something like CustomSchedulesMod.Settings.Restrict.
            if (ModsConfig.IsActive("name.krypt.rimworld.pawntablegrouped"))
                ApplyFixForGroupedPawnsList();
            playerWarned = true;
        }
        private static void ApplyFixForMysteriusCustomSchedules()
        {
            // Implement the fix logic for Mysterius.CustomSchedules
            var message = new Dialog_ModWarning("CustomSchedules (Continued) is Active", "Chronos Pointer will overlap the additional schedule buttons. You can hide this overlap by disabling the arrow the Day/Night bar.", () =>
            {
                ChronosPointerMod.Settings.DrawHourBar = false;
                ChronosPointerMod.Settings.DrawArrow = false;
                ChronosPointerMod.Settings?.Write();
            });

            Find.WindowStack?.Add(message);

        }

        private static void ApplyFixForScheduleClock()
        {
            // Implement the fix logic for Mysterius.CustomSchedules
            var message = new Dialog_MessageBox("Chronos Pointer will overlap the time indicator. You can hide the overlap by disabling the Pawn Section Time Indicator.", "OK", null, "Don't show again", () =>
            {
                ChronosPointerMod.Settings.DoLoadWarnings = false;
                ChronosPointerMod.Settings?.Write();
            }, title: "ScheduleClock is Active");
            message.buttonCText = "Disable Overlap";
            message.buttonCAction = () =>
            {
                ChronosPointerMod.Settings.DrawMainCursor = false;
                ChronosPointerMod.Settings?.Write();
            };
            Find.WindowStack?.Add(message);

        }
        private static void ApplyFixForGroupedPawnsList()
        {
            // Implement the fix logic for Mysterius.CustomSchedules
            var message = new Dialog_ModWarning("Grouped Pawns Lists is Active", "Chronos Pointer may not be properly lined up in the schedule window. To ensure proper placement, please disable \"Restrict\" in Grouped Pawns List mod settings.");
            Find.WindowStack?.Add(message);

        }
    }

#if CHRONOS_POINTER_USE_SPINE
    public class ChronosPointerMod : SpineMod<ChronosPointerSettings>
#else
    public class ChronosPointerMod : Mod
#endif
    {
#if CHRONOS_POINTER_USE_SPINE
        public new static ChronosPointerSettings Settings;
#else
        public static ChronosPointerSettings Settings;
#endif
        public static float cursorThickness = 2f; // Default thickness
        private Vector2 scrollPosition = Vector2.zero;

#if CHRONOS_POINTER_USE_SPINE
        public ChronosPointerMod(ModContentPack content)
            : base(
                content,
                "CoolNether123.ChronosPointer",
                new SemanticVersion(1, 1, 0),
                ChronosPointerSpineSettings.Schema.Definitions,
                SpineCapability.SettingsSchema)
#else
        public ChronosPointerMod(ModContentPack content) : base(content)
#endif
        {
#if CHRONOS_POINTER_USE_SPINE
            Settings = ManagedSettings;
#else
            Settings = GetSettings<ChronosPointerSettings>();
#endif

            if (ModsConfig.IsActive("brrainz.harmony"))
            {
                // Harmony patch
                var harmony = new HarmonyLib.Harmony("com.coolnether123.ChronosPointer");
                harmony.PatchAll();
                Log.Message("[ChronosPointer] Harmony patches applied.");
            }

            // Subscribe to sunlight threshold changes
            ChronosPointerSettings.OnSunlightThresholdChanged += () =>
            {
                Patch_ScheduleWindow.dayNightColorsCalculated = false;
                Patch_ScheduleWindow._cachedSeason = Season.Undefined;
                Patch_ScheduleWindow._cachedMap = null;
            };

            ChronosPointerApi.NotifyReady();
        }

#if CHRONOS_POINTER_USE_SPINE
        protected override string SettingsCategoryLabel =>
            "Chronos Pointer";
#endif

#if !CHRONOS_POINTER_USE_SPINE
        public override string SettingsCategory()
        {
            return "Chronos Pointer";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoWindowContents(inRect);
        }
#endif
        public override void WriteSettings()
        {
            base.WriteSettings();
            Patch_ScheduleWindow.dayNightColorsCalculated = false;
            Patch_ScheduleWindow.overrideIsAurora = false;
            Patch_ScheduleWindow.overrideIsEclipse = false;
            Patch_ScheduleWindow.overrideIsSolarFlare = false;
            Patch_ScheduleWindow.overrideIsToxicFallout = false;
            Patch_ScheduleWindow.overrideIsVolcanicWinter = false;
            Patch_ScheduleWindow.IsInTestMode = false;
            ChronosPointerApi.NotifySettingsChanged();

        }

    }
    
}

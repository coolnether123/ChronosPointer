#if !V1_0
using ColourPicker;
#endif
#if V1_1 || V1_0
using Harmony;
using System.Reflection; // Required for manual reflection in 1.1
#else
using HarmonyLib;
#endif
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace ChronosPointer
{

    class Dialog_ModWarning : Dialog_MessageBox
    {
#if V1_0
        public Dialog_ModWarning(string title, string text, Action fixAction = null, string fixActionText = "Disable Overlap", WindowLayer layer = WindowLayer.Dialog)
            : base(text, fixActionText, fixAction, null, null, title, false, null, null) // Omit 'layer' parameter
#elif V1_1 || V1_2
    public Dialog_ModWarning(string title, TaggedString text, Action fixAction = null, string fixActionText = "Disable Overlap", WindowLayer layer = WindowLayer.Dialog)
        : base(text, fixActionText, fixAction, null, null, title, false, null, null) // Omit 'layer' parameter
#else
    public Dialog_ModWarning(string title, TaggedString text, Action fixAction = null, string fixActionText = "Disable Overlap", WindowLayer layer = WindowLayer.Dialog)
        : base(text, fixActionText, fixAction, null, null, title, false, null, null, layer) // Keep 'layer' parameter
#endif
        {
            if (fixAction != null)
            {
                buttonCAction = fixAction;
                buttonCText = fixActionText;
            }
            buttonAText = "OK";
            buttonAAction = null;
            buttonBText = "Disable Warnings";
#if V1_0
            buttonBAction = new Action(DisableWarningsAction);
#else
            buttonBAction = () =>
            {
                ChronosPointerMod.Settings.DoLoadWarnings = false;
                ChronosPointerMod.Settings?.Write();
            };
#endif
        }

#if V1_0
        private static void DisableWarningsAction()
        {
            ChronosPointerMod.Settings.DoLoadWarnings = false;
            if (ChronosPointerMod.Settings != null) ChronosPointerMod.Settings.Write();
        }
#endif
    }

    [HarmonyPatch(typeof(UIRoot_Entry), "Init")]
    public static class Patch_OnGameLoad
    {
        public static bool playerWarned = false;
        [HarmonyPostfix]
        public static void UIRootEntryInit_Prefix()
        {
#if V1_0
            Verse.Log.Message("[ChronosPointer] Patch_OnGameLoad.UIRootEntryInit_Prefix() called.");
            // For V1_0, skip all mod compatibility checks as per instructions
            return;  // No further logic here
#else
            if (!ChronosPointerMod.Settings.DoLoadWarnings || playerWarned)
                return;

            if (ModsConfig.IsActive("Mysterius.CustomSchedules") && (ChronosPointerMod.Settings?.DrawHourBar ?? true))
                ApplyFixForMysteriusCustomSchedules();
            if (ModsConfig.IsActive("rswallen.scheduleclock") && (ChronosPointerMod.Settings?.DrawMainCursor ?? true))
                ApplyFixForScheduleClock();
            if (ModsConfig.IsActive("name.krypt.rimworld.pawntablegrouped"))
                ApplyFixForGroupedPawnsList();
            playerWarned = true;
#endif
        }
#if !V1_0
        private static void ApplyFixForMysteriusCustomSchedules()
        {
            var message = new Dialog_ModWarning("CustomSchedules (Continued) is Active", "Chronos Pointer will overlap the additional schedule buttons. You can hide this overlap by disabling the arrow the Day/Night bar.", () =>
            {
                ChronosPointerMod.Settings.DrawHourBar = false;
                ChronosPointerMod.Settings.DrawArrow = false;
                ChronosPointerMod.Settings?.Write();
            });
            Find.WindowStack?.Add(message);
        }
#endif

#if !V1_0
        private static void ApplyFixForScheduleClock()
        {
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
#endif


#if !V1_0
        private static void ApplyFixForGroupedPawnsList()
        {
            var message = new Dialog_ModWarning("Grouped Pawns Lists is Active", "Chronos Pointer may not be properly lined up in the schedule window. To ensure proper placement, please disable \"Restrict\" in Grouped Pawns List mod settings.");
            Find.WindowStack?.Add(message);
        }
#endif
    }

    public class ChronosPointerMod : Mod
    {
        public static ChronosPointerSettings Settings;
        public static float cursorThickness = 2f;
        private Vector2 scrollPosition = Vector2.zero;

        public ChronosPointerMod(ModContentPack content) : base(content)
        {
            Verse.Log.Message("[ChronosPointer] ChronosPointerMod constructor called.");
            Settings = GetSettings<ChronosPointerSettings>();

#if V1_1 || V1_0
            var harmonyType = Type.GetType("Harmony.HarmonyInstance, 0Harmony");
            bool harmonyLoaded = !ReferenceEquals(harmonyType, null);
#else
            bool harmonyLoaded = ModsConfig.IsActive("brrainz.harmony");
#endif

            if (harmonyLoaded)
            {
#if V1_1 || V1_0
                var harmony = HarmonyInstance.Create("com.coolnether123.ChronosPointer");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
#else
                var harmony = new HarmonyLib.Harmony("com.coolnether123.ChronosPointer");
                harmony.PatchAll();
#endif
                Log.Message("[ChronosPointer] Harmony patches applied.");
            }
            else
            {
                Log.Warning("[ChronosPointer] Harmony library not found. The mod will not function.");
            }

#if !V1_0
            ChronosPointerSettings.OnSunlightThresholdChanged += () =>
            {
                Patch_ScheduleWindow.dayNightColorsCalculated = false;
                Patch_ScheduleWindow._cachedSeason = Season.Undefined;
                Patch_ScheduleWindow._cachedMap = null;
            };
#endif
        }

        public override string SettingsCategory()
        {
            return "Chronos Pointer";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
#if V1_0
            Settings.DoSettingsWindowContents(inRect);
#else
            Settings.DoWindowContents(inRect);
#endif
        }
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
        }
    }
}
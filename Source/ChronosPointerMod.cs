using ColourPicker;
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
#if !(V1_0)
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
#endif
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

    public class ChronosPointerMod : Mod
    {
        public static ChronosPointerSettings Settings;
        public static float cursorThickness = 2f; // Default thickness
        private Vector2 scrollPosition = Vector2.zero;

        public ChronosPointerMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<ChronosPointerSettings>();

#if V1_1 || V1_0
            // For older versions, check if the Harmony 1.x library type exists, as it might be bundled, not a formal mod.
            // The assembly name for Harmony 1.x is "0Harmony".
            bool harmonyLoaded = Type.GetType("Harmony.HarmonyInstance, 0Harmony") != null;
#else
            // For modern versions, checking the active mod list is the standard and correct way.
            bool harmonyLoaded = ModsConfig.IsActive("brrainz.harmony");
#endif

            if (harmonyLoaded)
            {
                // Harmony patch
#if V1_1 || V1_0
                // Harmony 1.x initialization
                var harmony = HarmonyInstance.Create("com.coolnether123.ChronosPointer");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
#else
                // Harmony 2.x initialization
                var harmony = new HarmonyLib.Harmony("com.coolnether123.ChronosPointer");
                harmony.PatchAll();
#endif
                Log.Message("[ChronosPointer] Harmony patches applied.");
            }
            else
            {
                Log.Warning("[ChronosPointer] Harmony library not found. The mod will not function.");
            }

            // Subscribe to sunlight threshold changes
            ChronosPointerSettings.OnSunlightThresholdChanged += () =>
            {
                Patch_ScheduleWindow.dayNightColorsCalculated = false;
                Patch_ScheduleWindow._cachedSeason = Season.Undefined;
                Patch_ScheduleWindow._cachedMap = null;
            };
        }

        public override string SettingsCategory()
        {
            return "Chronos Pointer";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoWindowContents(inRect);
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
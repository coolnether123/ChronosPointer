using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using UnityEngine;
using Verse;

namespace ChronosPointer
{
    internal static class ChronosSharedSettingsStore
    {
        private const string SharedFileName = "ChronosPointer.SharedSettings.xml";
        private const string RimWorldSettingsPattern = "Mod_*_ChronosPointerMod.xml";
        private const string RootElementName = "ChronosPointerSharedSettings";

        public static void LoadAndSynchronize(ChronosPointerSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            try
            {
                string configFolder = GetConfigFolderPath();
                Directory.CreateDirectory(configFolder);

                List<FileInfo> candidates = GetSettingsCandidates(configFolder);
                for (int index = 0; index < candidates.Count; index++)
                {
                    FileInfo candidate = candidates[index];
                    if (!TryLoad(candidate.FullName, settings))
                    {
                        continue;
                    }

                    Save(settings);
                    if (!string.Equals(candidate.Name, SharedFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Message("[ChronosPointer] Migrated settings from " + candidate.Name + " into the shared settings store.");
                    }

                    return;
                }

                Save(settings);
            }
            catch (Exception exception)
            {
                Log.Error("[ChronosPointer] Failed to synchronize shared settings: " + exception);
            }
        }

        public static void Save(ChronosPointerSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            string temporaryPath = null;
            try
            {
                string configFolder = GetConfigFolderPath();
                Directory.CreateDirectory(configFolder);

                string destinationPath = Path.Combine(configFolder, SharedFileName);
                temporaryPath = destinationPath + ".tmp";

                XmlDocument document = CreateDocument(settings);
                XmlWriterSettings writerSettings = new XmlWriterSettings();
                writerSettings.Indent = true;
                writerSettings.Encoding = new System.Text.UTF8Encoding(false);

                using (XmlWriter writer = XmlWriter.Create(temporaryPath, writerSettings))
                {
                    document.Save(writer);
                }

                File.Copy(temporaryPath, destinationPath, true);
                File.Delete(temporaryPath);
                temporaryPath = null;
            }
            catch (Exception exception)
            {
                Log.Error("[ChronosPointer] Failed to save shared settings: " + exception);
            }
            finally
            {
                if (temporaryPath != null && File.Exists(temporaryPath))
                {
                    try
                    {
                        File.Delete(temporaryPath);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static List<FileInfo> GetSettingsCandidates(string configFolder)
        {
            List<FileInfo> candidates = new List<FileInfo>();
            string sharedPath = Path.Combine(configFolder, SharedFileName);
            if (File.Exists(sharedPath))
            {
                candidates.Add(new FileInfo(sharedPath));
            }

            string[] rimWorldSettingsFiles = Directory.GetFiles(configFolder, RimWorldSettingsPattern);
            for (int index = 0; index < rimWorldSettingsFiles.Length; index++)
            {
                candidates.Add(new FileInfo(rimWorldSettingsFiles[index]));
            }

            candidates.Sort(delegate(FileInfo first, FileInfo second)
            {
                return second.LastWriteTimeUtc.CompareTo(first.LastWriteTimeUtc);
            });
            return candidates;
        }

        private static bool TryLoad(string path, ChronosPointerSettings settings)
        {
            try
            {
                XmlDocument document = new XmlDocument();
                document.Load(path);
                XmlNode root = GetSettingsRoot(document);
                return root != null && ApplySettings(root, settings) > 0;
            }
            catch (Exception exception)
            {
                Log.Error("[ChronosPointer] Could not import settings from " + Path.GetFileName(path) + ": " + exception.Message);
                return false;
            }
        }

        private static XmlNode GetSettingsRoot(XmlDocument document)
        {
            XmlNode sharedRoot = document.SelectSingleNode("/" + RootElementName);
            if (sharedRoot != null)
            {
                return sharedRoot;
            }

            XmlNode modSettings = document.SelectSingleNode("/SettingsBlock/ModSettings");
            return modSettings ?? document.DocumentElement;
        }

        private static int ApplySettings(XmlNode root, ChronosPointerSettings settings)
        {
            int applied = 0;
            bool boolValue;
            float floatValue;
            Color colorValue;

            if (TryReadBool(root, out boolValue, "DrawArrow")) { settings.DrawArrow = boolValue; applied++; }
            if (TryReadBool(root, out boolValue, "DrawHighlight", "DrawCurrentHourHighlight")) { settings.DrawCurrentHourHighlight = boolValue; applied++; }
            if (TryReadBool(root, out boolValue, "DrawHourBar")) { settings.DrawHourBar = boolValue; applied++; }
            if (TryReadBool(root, out boolValue, "DrawHoursBarCursor")) { settings.DrawHoursBarCursor = boolValue; applied++; }
            if (TryReadBool(root, out boolValue, "DrawDynamicTimeTraceLine", "DoDynamicHoursBarLine")) { settings.DoDynamicHoursBarLine = boolValue; applied++; }
            if (TryReadBool(root, out boolValue, "DrawMainCursor")) { settings.DrawMainCursor = boolValue; applied++; }
            if (TryReadBool(root, out boolValue, "DoFilledHourHighlight")) { settings.DoFilledHourHighlight = boolValue; applied++; }
            if (TryReadBool(root, out boolValue, "DrawIncidentOverlay")) { settings.DrawIncidentOverlay = boolValue; applied++; }
            if (TryReadBool(root, out boolValue, "DoLoadWarnings")) { settings.DoLoadWarnings = boolValue; applied++; }

            if (TryReadFloat(root, out floatValue, "CursorThickness")) { settings.CursorThickness = ChronosPointerSettings.ValidateCursorThickness(floatValue); applied++; }
            if (TryReadFloat(root, out floatValue, "HoursBarThickness", "HoursBarCursorThickness")) { settings.HoursBarCursorThickness = ChronosPointerSettings.ValidateCursorThickness(floatValue); applied++; }
            if (TryReadFloat(root, out floatValue, "HighlightBorderThickness")) { settings.HighlightBorderThickness = floatValue; applied++; }
            if (TryReadFloat(root, out floatValue, "AuroraMinOpacity")) { settings.AuroraMinOpacity = floatValue; applied++; }
            if (TryReadFloat(root, out floatValue, "AuroraMaxOpacity")) { settings.AuroraMaxOpacity = floatValue; applied++; }
            if (TryReadFloat(root, out floatValue, "SunlightThreshold_Night")) { settings.SunlightThreshold_Night = floatValue; applied++; }
            if (TryReadFloat(root, out floatValue, "SunlightThreshold_Any", "_SunlightThreshold_Any")) { settings._SunlightThreshold_Any = floatValue; applied++; }
            if (TryReadFloat(root, out floatValue, "SunlightThreshold_DawnDusk")) { settings.SunlightThreshold_DawnDusk = floatValue; applied++; }
            if (TryReadFloat(root, out floatValue, "SunlightThreshold_SunriseSunset")) { settings.SunlightThreshold_SunriseSunset = floatValue; applied++; }

            if (TryReadColor(root, out colorValue, "Color_Arrow")) { settings.Color_Arrow = colorValue; applied++; }
            if (TryReadColor(root, out colorValue, "Color_Highlight", "Color_HourHighlight")) { settings.Color_HourHighlight = colorValue; applied++; }
            if (TryReadColor(root, out colorValue, "Color_MainCursor")) { settings.Color_MainCursor = colorValue; applied++; }
            if (TryReadColor(root, out colorValue, "Color_HoursBarCursor_Day")) { settings.Color_HoursBarCursor_Day = colorValue; applied++; }
            if (TryReadColor(root, out colorValue, "Color_HoursBarCursor_Night")) { settings.Color_HoursBarCursor_Night = colorValue; applied++; }
            if (TryReadColor(root, out colorValue, "Color_Night")) { settings.Color_Night = colorValue; applied++; }
            if (TryReadColor(root, out colorValue, "Color_DawnDusk")) { settings.Color_DawnDusk = colorValue; applied++; }
            if (TryReadColor(root, out colorValue, "Color_SunriseSunset")) { settings.Color_SunriseSunset = colorValue; applied++; }
            if (TryReadColor(root, out colorValue, "Color_Day")) { settings.Color_Day = colorValue; applied++; }
            if (TryReadColor(root, out colorValue, "Color_VolcanicWinter")) { settings.Color_VolcanicWinter = colorValue; applied++; }
            if (TryReadColor(root, out colorValue, "ToxicFalloutColor", "Color_ToxicFallout")) { settings.Color_ToxicFallout = colorValue; applied++; }
            if (TryReadColor(root, out colorValue, "AuroraColor1", "Color_Aurora1")) { settings.Color_Aurora1 = colorValue; applied++; }
            if (TryReadColor(root, out colorValue, "AuroraColor2", "Color_Aurora2")) { settings.Color_Aurora2 = colorValue; applied++; }

            return applied;
        }

        private static XmlDocument CreateDocument(ChronosPointerSettings settings)
        {
            XmlDocument document = new XmlDocument();
            document.AppendChild(document.CreateXmlDeclaration("1.0", "utf-8", null));
            XmlElement root = document.CreateElement(RootElementName);
            root.SetAttribute("version", "1");
            document.AppendChild(root);

            AppendValue(document, root, "DrawArrow", settings.DrawArrow);
            AppendValue(document, root, "DrawHighlight", settings.DrawCurrentHourHighlight);
            AppendValue(document, root, "DrawHourBar", settings.DrawHourBar);
            AppendValue(document, root, "DrawHoursBarCursor", settings.DrawHoursBarCursor);
            AppendValue(document, root, "DrawDynamicTimeTraceLine", settings.DoDynamicHoursBarLine);
            AppendValue(document, root, "DrawMainCursor", settings.DrawMainCursor);
            AppendValue(document, root, "DoFilledHourHighlight", settings.DoFilledHourHighlight);
            AppendValue(document, root, "DrawIncidentOverlay", settings.DrawIncidentOverlay);
            AppendValue(document, root, "DoLoadWarnings", settings.DoLoadWarnings);

            AppendValue(document, root, "CursorThickness", settings.CursorThickness);
            AppendValue(document, root, "HoursBarThickness", settings.HoursBarCursorThickness);
            AppendValue(document, root, "HighlightBorderThickness", settings.HighlightBorderThickness);
            AppendValue(document, root, "AuroraMinOpacity", settings.AuroraMinOpacity);
            AppendValue(document, root, "AuroraMaxOpacity", settings.AuroraMaxOpacity);
            AppendValue(document, root, "SunlightThreshold_Night", settings.SunlightThreshold_Night);
            AppendValue(document, root, "SunlightThreshold_Any", settings._SunlightThreshold_Any);
            AppendValue(document, root, "SunlightThreshold_DawnDusk", settings.SunlightThreshold_DawnDusk);
            AppendValue(document, root, "SunlightThreshold_SunriseSunset", settings.SunlightThreshold_SunriseSunset);

            AppendColor(document, root, "Color_Arrow", settings.Color_Arrow);
            AppendColor(document, root, "Color_Highlight", settings.Color_HourHighlight);
            AppendColor(document, root, "Color_MainCursor", settings.Color_MainCursor);
            AppendColor(document, root, "Color_HoursBarCursor_Day", settings.Color_HoursBarCursor_Day);
            AppendColor(document, root, "Color_HoursBarCursor_Night", settings.Color_HoursBarCursor_Night);
            AppendColor(document, root, "Color_Night", settings.Color_Night);
            AppendColor(document, root, "Color_DawnDusk", settings.Color_DawnDusk);
            AppendColor(document, root, "Color_SunriseSunset", settings.Color_SunriseSunset);
            AppendColor(document, root, "Color_Day", settings.Color_Day);
            AppendColor(document, root, "Color_VolcanicWinter", settings.Color_VolcanicWinter);
            AppendColor(document, root, "ToxicFalloutColor", settings.Color_ToxicFallout);
            AppendColor(document, root, "AuroraColor1", settings.Color_Aurora1);
            AppendColor(document, root, "AuroraColor2", settings.Color_Aurora2);

            return document;
        }

        private static bool TryReadBool(XmlNode root, out bool value, params string[] names)
        {
            value = false;
            XmlNode node = FindNode(root, names);
            return node != null && bool.TryParse(node.InnerText, out value);
        }

        private static bool TryReadFloat(XmlNode root, out float value, params string[] names)
        {
            value = 0f;
            XmlNode node = FindNode(root, names);
            if (node == null)
            {
                return false;
            }

            return float.TryParse(node.InnerText, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                   float.TryParse(node.InnerText, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        private static bool TryReadColor(XmlNode root, out Color value, params string[] names)
        {
            value = default(Color);
            XmlNode node = FindNode(root, names);
            if (node == null)
            {
                return false;
            }

            float red;
            float green;
            float blue;
            float alpha;
            if (TryReadAttribute(node, "r", out red) &&
                TryReadAttribute(node, "g", out green) &&
                TryReadAttribute(node, "b", out blue) &&
                TryReadAttribute(node, "a", out alpha))
            {
                value = new Color(red, green, blue, alpha);
                return true;
            }

            string serialized = node.InnerText.Trim();
            int openParenthesis = serialized.IndexOf('(');
            int closeParenthesis = serialized.LastIndexOf(')');
            if (openParenthesis >= 0 && closeParenthesis > openParenthesis)
            {
                serialized = serialized.Substring(openParenthesis + 1, closeParenthesis - openParenthesis - 1);
            }

            string[] components = serialized.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (components.Length != 4 ||
                !TryParseFloat(components[0], out red) ||
                !TryParseFloat(components[1], out green) ||
                !TryParseFloat(components[2], out blue) ||
                !TryParseFloat(components[3], out alpha))
            {
                return false;
            }

            value = new Color(red, green, blue, alpha);
            return true;
        }

        private static XmlNode FindNode(XmlNode root, string[] names)
        {
            for (int index = 0; index < names.Length; index++)
            {
                XmlNode node = root.SelectSingleNode(".//" + names[index]);
                if (node != null)
                {
                    return node;
                }
            }

            return null;
        }

        private static bool TryReadAttribute(XmlNode node, string name, out float value)
        {
            value = 0f;
            if (node.Attributes == null || node.Attributes[name] == null)
            {
                return false;
            }

            return TryParseFloat(node.Attributes[name].Value, out value);
        }

        private static bool TryParseFloat(string serialized, out float value)
        {
            return float.TryParse(serialized.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                   float.TryParse(serialized.Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        private static void AppendValue(XmlDocument document, XmlElement root, string name, bool value)
        {
            XmlElement element = document.CreateElement(name);
            element.InnerText = XmlConvert.ToString(value);
            root.AppendChild(element);
        }

        private static void AppendValue(XmlDocument document, XmlElement root, string name, float value)
        {
            XmlElement element = document.CreateElement(name);
            element.InnerText = XmlConvert.ToString(value);
            root.AppendChild(element);
        }

        private static void AppendColor(XmlDocument document, XmlElement root, string name, Color color)
        {
            XmlElement element = document.CreateElement(name);
            element.SetAttribute("r", XmlConvert.ToString(color.r));
            element.SetAttribute("g", XmlConvert.ToString(color.g));
            element.SetAttribute("b", XmlConvert.ToString(color.b));
            element.SetAttribute("a", XmlConvert.ToString(color.a));
            root.AppendChild(element);
        }

        private static string GetConfigFolderPath()
        {
#if V0_16 || V0_15 || V0_14 || V0_13 || VALPHA4
            return Path.Combine(GenFilePaths.SaveDataFolderPath, "Config");
#else
            return GenFilePaths.ConfigFolderPath;
#endif
        }
    }
}

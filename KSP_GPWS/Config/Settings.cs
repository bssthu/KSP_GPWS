// GPWS mod for KSP
// License: CC-BY-NC-SA
// Author: bss, 2015

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSP_GPWS
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    class SettingsLoader : MonoBehaviour
    {
        public void Awake()
        {
            // load settings when game start
            Settings.LoadSettings();
            // check toolbar
            if (Settings.useBlizzy78Toolbar && !ToolbarManager.ToolbarAvailable)
            {
                Util.Log("Blizzy78 Toolbar not available");
                Settings.useBlizzy78Toolbar = false;
            }
        }
    }

    static class Settings
    {
        #region from_file

        public static bool enableSystem = true;
        public static float volume = 0.5f;
        public static bool enableDescentRate = true;
        public static bool enableClosureToTerrain = true;
        public static bool enableAltitudeLoss = true;
        public static bool enableTerrainClearance = true;
        public static bool enableAltitudeCallouts = true;
        public static bool enableBankAngle = false;
        public static bool enableTraffic = true;

        public static float descentRateFactor = 1.0f;
        public static float tooLowGearAltitude = 500.0f;
        public static int[] altitudeArray = { 2500, 1000, 500, 400, 300, 200, 100, 50, 40, 30, 20, 10 };
        public enum UnitOfAltitude
        {
            FOOT = 0,
            METER = 1,
        };
        public static UnitOfAltitude unitOfAltitude = UnitOfAltitude.FOOT;    // use meters or feet, feet is recommanded.

        public static bool useBlizzy78Toolbar = false;

        #endregion

        #region in_xml_file

        public static Rect guiwindowPosition = new Rect(100, 100, 100, 50);
        public static bool showConfigs = true;  // show lower part of the setting GUI
        public static bool guiIsActive = false;

        #endregion


        public static void LoadSettings()
        {
            loadFromCFG();
            loadFromXML();
        }

        private static void loadFromCFG()
        {
            foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("GPWS_SETTINGS"))
            {
                if (ConvertValue(node, "name", "") == "gpwsSettings")
                {
                    ConvertValue(node, "enableSystem", ref enableSystem);
                    ConvertValue(node, "volume", ref volume);
                    ConvertValue(node, "enableDescentRate", ref enableDescentRate);
                    ConvertValue(node, "enableClosureToTerrain", ref enableClosureToTerrain);
                    ConvertValue(node, "enableAltitudeLoss", ref enableAltitudeLoss);
                    ConvertValue(node, "enableTerrainClearance", ref enableTerrainClearance);
                    ConvertValue(node, "enableAltitudeCallouts", ref enableAltitudeCallouts);
                    ConvertValue(node, "enableBankAngle", ref enableBankAngle);
                    ConvertValue(node, "enableTraffic", ref enableTraffic);

                    ConvertValue(node, "descentRateFactor", ref descentRateFactor);
                    ConvertValue(node, "tooLowGearAltitude", ref tooLowGearAltitude);
                    if (node.HasValue("altitudeArray"))
                    {
                        String[] intstrings = node.GetValue("altitudeArray").Split(',');
                        if (intstrings.Length > 0)
                        {
                            int id = 0;
                            int[] tempAlt = new int[intstrings.Length];
                            for (int j = 0; j < intstrings.Length; j++)
                            {
                                if (int.TryParse(intstrings[j], out tempAlt[id]))
                                {
                                    id++;
                                }
                            }
                            altitudeArray = new int[id];
                            for (int j = 0; j < id; j++)
                            {
                                altitudeArray[j] = tempAlt[j];
                            }
                        }
                    }
                    ConvertValue(node, "unitOfAltitude", ref unitOfAltitude);

                    ConvertValue(node, "useBlizzy78Toolbar", ref useBlizzy78Toolbar);
                }   // End of has value "name"
            }
            // check legality
            descentRateFactor = Math.Max(descentRateFactor, 0.1f);
            descentRateFactor = Math.Min(descentRateFactor, 10.0f);
            volume = Math.Max(volume, 0.0f);
            volume = Math.Min(volume, 1.0f);
        }

        private static T ConvertValue<T>(ConfigNode node, String key, T def = default(T))
        {
            T value;
            return TryConvertValue(node, key, out value) ? value : def;
        }

        private static void ConvertValue<T>(ConfigNode node, String key, ref T value)
        {
            value = ConvertValue(node, key, value);
        }

        private static bool TryConvertValue<T>(ConfigNode node, String key, out T value)
        {
            value = default(T);

            if (!node.HasValue(key))
            {
                return false;
            }

            String str = node.GetValue(key);
            var typeConverter = TypeDescriptor.GetConverter(typeof(T));

            try
            {
                value = (T)typeConverter.ConvertFromInvariantString(str);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void loadFromXML()
        {
            KSP.IO.PluginConfiguration config = KSP.IO.PluginConfiguration.CreateForType<SettingGUI>(); // why use template T?
            config.load();
            guiwindowPosition = config.GetValue<Rect>("guiwindowPosition", guiwindowPosition);
            showConfigs = config.GetValue<bool>("showConfig", showConfigs);
            guiIsActive = config.GetValue<bool>("guiIsActive", guiIsActive);
        }

        public static void SaveSettings()
        {
            saveToCFG();
            saveToXML();
        }

        private static void saveToCFG()
        {
            ConfigNode config = new ConfigNode();
            ConfigNode gpwsNode = new ConfigNode();

            gpwsNode.name = "GPWS_SETTINGS";

            gpwsNode.AddValue("name", "gpwsSettings");

            gpwsNode.AddValue("enableSystem", enableSystem);
            gpwsNode.AddValue("volume", volume);
            gpwsNode.AddValue("enableDescentRate", enableDescentRate);
            gpwsNode.AddValue("enableClosureToTerrain", enableClosureToTerrain);
            gpwsNode.AddValue("enableAltitudeLoss", enableAltitudeLoss);
            gpwsNode.AddValue("enableTerrainClearance", enableTerrainClearance);
            gpwsNode.AddValue("enableAltitudeCallouts", enableAltitudeCallouts);
            gpwsNode.AddValue("enableBankAngle", enableBankAngle);
            gpwsNode.AddValue("enableTraffic", enableTraffic);

            gpwsNode.AddValue("descentRateFactor", descentRateFactor);
            gpwsNode.AddValue("tooLowGearAltitude", tooLowGearAltitude);
            gpwsNode.AddValue("altitudeArray", String.Join(",", Array.ConvertAll(altitudeArray, x => x.ToString())));
            gpwsNode.AddValue("unitOfAltitude", unitOfAltitude);

            gpwsNode.AddValue("useBlizzy78Toolbar", useBlizzy78Toolbar);

            config.AddNode(gpwsNode);
            config.Save(KSPUtil.ApplicationRootPath + "GameData/GPWS/settings.cfg", "GPWS");
        }

        public static void saveToXML()
        {
            KSP.IO.PluginConfiguration config = KSP.IO.PluginConfiguration.CreateForType<SettingGUI>();
            config.SetValue("guiwindowPosition", guiwindowPosition);
            config.SetValue("showConfig", showConfigs);
            config.SetValue("guiIsActive", guiIsActive);
            config.save();
        }
    }
}

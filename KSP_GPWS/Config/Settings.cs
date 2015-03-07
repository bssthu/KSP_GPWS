// GPWS mod for KSP
// License: CC-BY-NC-SA
// Author: bss, 2015

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP_GPWS.Interfaces;
using KSP_GPWS.SimpleTypes;

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
        public static bool useBlizzy78Toolbar = false;
        public static IPlaneConfig PlaneConfig { get; private set; }

        public static Rect guiwindowPosition = new Rect(100, 100, 100, 50);
        public static bool showConfigs = true;  // show lower part of the setting GUI
        public static bool guiIsActive = false;

        public static void InitializePlaneConfig(IPlaneConfig planeConfig)
        {
            PlaneConfig = planeConfig;
            PlaneConfig.EnableSystem = true;
            PlaneConfig.Volume = 0.5f;
            PlaneConfig.EnableDescentRate = true;
            PlaneConfig.EnableClosureToTerrain = true;
            PlaneConfig.EnableAltitudeLoss = true;
            PlaneConfig.EnableTerrainClearance = true;
            PlaneConfig.EnableAltitudeCallouts = true;
            PlaneConfig.EnableBankAngle = false;
            PlaneConfig.EnableTraffic = true;

            PlaneConfig.DescentRateFactor = 1.0f;
            PlaneConfig.TooLowGearAltitude = 500.0f;
            PlaneConfig.AltitudeArray = new int[] { 2500, 1000, 500, 400, 300, 200, 100, 50, 40, 30, 20, 10 };
            PlaneConfig.UnitOfAltitude = UnitOfAltitude.FOOT;
        }

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
                    PlaneConfig.EnableSystem = ConvertValue<bool>(node, "enableSystem");
                    PlaneConfig.Volume = ConvertValue<float>(node, "volume");
                    PlaneConfig.EnableDescentRate = ConvertValue<bool>(node, "enableDescentRate");
                    PlaneConfig.EnableClosureToTerrain = ConvertValue<bool>(node, "enableClosureToTerrain");
                    PlaneConfig.EnableAltitudeLoss = ConvertValue<bool>(node, "enableAltitudeLoss");
                    PlaneConfig.EnableTerrainClearance = ConvertValue<bool>(node, "enableTerrainClearance");
                    PlaneConfig.EnableAltitudeCallouts = ConvertValue<bool>(node, "enableAltitudeCallouts");
                    PlaneConfig.EnableBankAngle = ConvertValue<bool>(node, "enableBankAngle");
                    PlaneConfig.EnableTraffic = ConvertValue<bool>(node, "enableTraffic");

                    PlaneConfig.DescentRateFactor = ConvertValue<float>(node, "descentRateFactor");
                    PlaneConfig.TooLowGearAltitude = ConvertValue<float>(node, "tooLowGearAltitude");
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
                            PlaneConfig.AltitudeArray = new int[id];
                            for (int j = 0; j < id; j++)
                            {
                                PlaneConfig.AltitudeArray[j] = tempAlt[j];
                            }
                        }
                    }
                    PlaneConfig.UnitOfAltitude = ConvertValue<UnitOfAltitude>(node, "unitOfAltitude");

                    ConvertValue(node, "useBlizzy78Toolbar", ref useBlizzy78Toolbar);
                }   // End of has value "name"
            }
            // check legality
            PlaneConfig.DescentRateFactor = Math.Max(PlaneConfig.DescentRateFactor, 0.1f);
            PlaneConfig.DescentRateFactor = Math.Min(PlaneConfig.DescentRateFactor, 10.0f);
            PlaneConfig.Volume = Math.Max(PlaneConfig.Volume, 0.0f);
            PlaneConfig.Volume = Math.Min(PlaneConfig.Volume, 1.0f);
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

            gpwsNode.AddValue("enableSystem", PlaneConfig.EnableSystem);
            gpwsNode.AddValue("volume", PlaneConfig.Volume);
            gpwsNode.AddValue("enableDescentRate", PlaneConfig.EnableDescentRate);
            gpwsNode.AddValue("enableClosureToTerrain", PlaneConfig.EnableClosureToTerrain);
            gpwsNode.AddValue("enableAltitudeLoss", PlaneConfig.EnableAltitudeLoss);
            gpwsNode.AddValue("enableTerrainClearance", PlaneConfig.EnableTerrainClearance);
            gpwsNode.AddValue("enableAltitudeCallouts", PlaneConfig.EnableAltitudeCallouts);
            gpwsNode.AddValue("enableBankAngle", PlaneConfig.EnableBankAngle);
            gpwsNode.AddValue("enableTraffic", PlaneConfig.EnableTraffic);

            gpwsNode.AddValue("descentRateFactor", PlaneConfig.DescentRateFactor);
            gpwsNode.AddValue("tooLowGearAltitude", PlaneConfig.TooLowGearAltitude);
            gpwsNode.AddValue("altitudeArray", String.Join(",", Array.ConvertAll(PlaneConfig.AltitudeArray, x => x.ToString())));
            gpwsNode.AddValue("unitOfAltitude", PlaneConfig.UnitOfAltitude);

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

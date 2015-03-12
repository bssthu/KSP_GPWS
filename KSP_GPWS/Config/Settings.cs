// GPWS mod for KSP
// License: CC-BY-NC-SA
// Author: bss, 2015

using System;
using System.Collections.Generic;
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
            if (Settings.UseBlizzy78Toolbar && !ToolbarManager.ToolbarAvailable)
            {
                Util.Log("Blizzy78 Toolbar not available");
            }
            GPWS.InitializeGPWSFunctions();
        }
    }

    static class Settings
    {
        public static float Volume = 0.5f;
        public static bool UseBlizzy78Toolbar = false;

        public static IPlaneConfig PlaneConfig
        {
            get
            {
                return _planeConfig;
            }
            set
            {
                if (_planeConfig == null)
                {
                    _planeConfig = value;
                    if (planeConfigNode != null)
                    {
                        (_planeConfig as IConfigNode).Load(planeConfigNode);
                    }
                }
            }
        }
        private static IPlaneConfig _planeConfig = null;

        public static ILanderConfig LanderConfig
        {
            get
            {
                return _landerConfig;
            }
            set
            {
                if (_landerConfig == null)
                {
                    _landerConfig = value;
                    if (landerConfigNode != null)
                    {
                        (_landerConfig as IConfigNode).Load(landerConfigNode);
                    }
                }
            }
        }
        private static ILanderConfig _landerConfig = null;

        private static ConfigNode planeConfigNode = null;
        private static ConfigNode landerConfigNode = null;

        public static Rect guiwindowPosition = new Rect(100, 100, 100, 50);
        public static bool showConfigs = true;  // show lower part of the setting GUI
        public static bool guiIsActive = false;

        public static void LoadSettings()
        {
            loadFromCFG();
            loadFromXML();
        }

        private static void loadFromCFG()
        {
            ConfigNode node = ConfigNode.Load(KSPUtil.ApplicationRootPath + "GameData/GPWS/settings.cfg");
            if (node.HasNode("GPWS_SETTINGS"))
            {
                node = node.GetNode("GPWS_SETTINGS");
                if (Util.ConvertValue(node, "name", "") == "gpwsSettings")
                {
                    if (node.HasNode("Plane"))
                    {
                        planeConfigNode = node.GetNode("Plane");
                    }
                    if (node.HasNode("Lander"))
                    {
                        landerConfigNode = node.GetNode("Lander");
                    }

                    Util.ConvertValue<float>(node, "Volume", ref Volume);
                    Util.ConvertValue(node, "UseBlizzy78Toolbar", ref UseBlizzy78Toolbar);
                }   // End of has value "name"
            }
            // check legality
            Volume = Math.Max(Volume, 0.0f);
            Volume = Math.Min(Volume, 1.0f);
        }

        private static void loadFromXML()
        {
            KSP.IO.PluginConfiguration config = KSP.IO.PluginConfiguration.CreateForType<SettingsLoader>(); // why use template T?
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

            ConfigNode planeNode = new ConfigNode();
            PlaneConfig.Save(planeNode);
            gpwsNode.AddNode(planeNode);

            ConfigNode landerNode = new ConfigNode();
            LanderConfig.Save(landerNode);
            gpwsNode.AddNode(landerNode);

            gpwsNode.AddValue("Volume", Settings.Volume);
            gpwsNode.AddValue("UseBlizzy78Toolbar", UseBlizzy78Toolbar);

            config.AddNode(gpwsNode);
            config.Save(KSPUtil.ApplicationRootPath + "GameData/GPWS/settings.cfg", "GPWS");
        }

        public static void saveToXML()
        {
            KSP.IO.PluginConfiguration config = KSP.IO.PluginConfiguration.CreateForType<SettingsLoader>();
            config.SetValue("guiwindowPosition", guiwindowPosition);
            config.SetValue("showConfig", showConfigs);
            config.SetValue("guiIsActive", guiIsActive);
            config.save();
        }
    }
}

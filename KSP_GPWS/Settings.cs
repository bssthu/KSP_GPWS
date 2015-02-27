// GPWS mod for KSP
// License: CC-BY-NC-SA
// Author: bss, 2015

using System;
using System.Collections.Generic;
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
                Tools.Log("Blizzy78 Toolbar not available");
                Settings.useBlizzy78Toolbar = false;
            }
        }
    }

    static class Settings
    {
        #region from_file

        public static bool enableSystem = true;
        public static bool enableDescentRate = true;
        public static bool enableTerrainClearance = true;
        public static bool enableAltitudeCallouts = true;
        public static bool enableBankAngle = false;

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

        #region in_memory

        public static Rect guiwindowPosition = new Rect(100, 100, 800, 50);
        public static bool showConfig = true;

        #endregion


        public static void LoadSettings()
        {
            foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("GPWS_SETTINGS"))
            {
                if (node.HasValue("name") && node.GetValue("name") == "gpwsSettings")
                {
                    if (node.HasValue("enableSystem"))
                    {
                        bool.TryParse(node.GetValue("enableSystem"), out enableSystem);
                    }
                    if (node.HasValue("enableDescentRate"))
                    {
                        bool.TryParse(node.GetValue("enableDescentRate"), out enableDescentRate);
                    }
                    if (node.HasValue("enableTerrainClearance"))
                    {
                        bool.TryParse(node.GetValue("enableTerrainClearance"), out enableTerrainClearance);
                    }
                    if (node.HasValue("enableAltitudeCallouts"))
                    {
                        bool.TryParse(node.GetValue("enableAltitudeCallouts"), out enableAltitudeCallouts);
                    }
                    if (node.HasValue("enableBankAngle"))
                    {
                        bool.TryParse(node.GetValue("enableBankAngle"), out enableBankAngle);
                    }

                    if (node.HasValue("descentRateFactor"))
                    {
                        float.TryParse(node.GetValue("descentRateFactor"), out descentRateFactor);
                    }
                    if (node.HasValue("tooLowGearAltitude"))
                    {
                        float.TryParse(node.GetValue("tooLowGearAltitude"), out tooLowGearAltitude);
                    }
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
                    if (node.HasValue("unitOfAltitude"))
                    {
                        try
                        {
                            unitOfAltitude = (UnitOfAltitude)Enum.Parse(typeof(UnitOfAltitude),
                                node.GetValue("unitOfAltitude"), true);
                        }
                        catch (Exception ex)
                        {
                            Tools.Log("Error: " + ex.Message);
                        }
                    }

                    if (node.HasValue("useBlizzy78Toolbar"))
                    {
                        bool.TryParse(node.GetValue("useBlizzy78Toolbar"), out useBlizzy78Toolbar);
                    }
                }   // End of has value "name"
            }
        }

        public static void SaveSettings()
        {
            ConfigNode config = new ConfigNode();
            ConfigNode gpwsNode = new ConfigNode();

            gpwsNode.name = "GPWS_SETTINGS";

            gpwsNode.AddValue("name", "gpwsSettings");

            gpwsNode.AddValue("enableSystem", enableSystem);
            gpwsNode.AddValue("enableDescentRate", enableDescentRate);
            gpwsNode.AddValue("enableTerrainClearance", enableTerrainClearance);
            gpwsNode.AddValue("enableAltitudeCallouts", enableAltitudeCallouts);
            gpwsNode.AddValue("enableBankAngle", enableBankAngle);

            gpwsNode.AddValue("descentRateFactor", descentRateFactor);
            gpwsNode.AddValue("tooLowGearAltitude", tooLowGearAltitude);
            gpwsNode.AddValue("altitudeArray", String.Join(",", Array.ConvertAll(altitudeArray, x => x.ToString())));
            gpwsNode.AddValue("unitOfAltitude", unitOfAltitude);

            gpwsNode.AddValue("useBlizzy78Toolbar", useBlizzy78Toolbar);

            config.AddNode(gpwsNode);
            config.Save(KSPUtil.ApplicationRootPath + "GameData/GPWS/settings.cfg", "GPWS");
        }
    }
}

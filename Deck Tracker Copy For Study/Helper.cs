﻿using System;
using System.Net;
using System.Windows;

namespace Deck_Tracker_Copy_For_Study
{
    public class Helper
    {
        private static XmlManager<SerializableVersion> _xmlManager;

        public static void CheckForUpdates()
        {
            var versionXmlUrl =
                @"https://raw.githubusercontent.com/Epix37/Hearthstone-Deck-Tracker/master/Hearthstone%20Deck%20Tracker/Version.xml";

            var xml = new WebClient().DownloadString(versionXmlUrl);

            _xmlManager = new XmlManager<SerializableVersion>() { Type = typeof(SerializableVersion) };

            var currentVersion = new Version(_xmlManager.Load("Version.xml").ToString());
            var newVersion = new Version(_xmlManager.LoadFromString(xml).ToString());

            if (newVersion > currentVersion)
            {
                var releaseDownloadUrl = @"https://github.com/Epix37/Hearthstone-Deck-Tracker/releases";
                if (
                    MessageBox.Show("New version available at: \n" + releaseDownloadUrl, "New version available!",
                                    MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    System.Diagnostics.Process.Start(releaseDownloadUrl);
                }
            }
        }

        public static bool IsNumeric(char c)
        {
            int output;
            return Int32.TryParse(c.ToString(), out output);
        }
    }
}

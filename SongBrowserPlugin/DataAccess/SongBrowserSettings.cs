﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;


namespace SongBrowserPlugin.DataAccess
{
    [Serializable]
    public enum SongSortMode
    {
        Default,
        Author,
        Original,
        Newest,        
        PlayCount,
        Difficulty,
        Random,

        // Deprecated
        Favorites,
        Playlist,
        Search
    }

    [Serializable]
    public enum SongFilterMode
    {
        None,
        Favorites,
        Playlist,
        Search
    }

    [Serializable]
    public class SongBrowserSettings
    {
        public SongSortMode sortMode = default(SongSortMode);
        public SongFilterMode filterMode = default(SongFilterMode);
       
        private HashSet<String> _favorites = default(HashSet<String>);

        public List<String> searchTerms = default(List<String>);

        public String currentLevelId = default(String);
        public String currentDirectory = default(String);
        public String currentPlaylistFile = default(String);

        public bool folderSupportEnabled = false;
        public bool randomInstantQueue = false;

        [NonSerialized]
        private static Logger Log = new Logger("SongBrowserSettings");

        [XmlArray(@"favorites")]
        public HashSet<String> Favorites
        {
            get
            {
                return _favorites;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public SongBrowserSettings()
        {
            searchTerms = new List<string>();
            _favorites = new HashSet<String>();
        }

        /// <summary>
        /// Helper to acquire settings path at runtime.
        /// </summary>
        /// <returns></returns>
        public static String SettingsPath()
        {
            return Path.Combine(Environment.CurrentDirectory, "song_browser_settings.xml");
        }

        /// <summary>
        /// Path to the common favorites file location.
        /// </summary>
        /// <returns></returns>
        public static String DownloaderFavoritesFilePath()
        {
            return Path.Combine(Environment.CurrentDirectory, "favoriteSongs.cfg");
        }

        /// <summary>
        /// Load the settings file for this plugin.
        /// If we fail to load return Default settings.
        /// </summary>
        /// <returns>SongBrowserSettings</returns>
        public static SongBrowserSettings Load()
        {
            Log.Trace("Load()");
            SongBrowserSettings retVal = null;

            // No Settings file.
            String settingsFilePath = SongBrowserSettings.SettingsPath();
            if (File.Exists(settingsFilePath))
            {
                // Deserialization from JSON
                FileStream fs = null;
                try
                {
                    fs = File.OpenRead(settingsFilePath);
                    XmlSerializer serializer = new XmlSerializer(typeof(SongBrowserSettings));
                    retVal = (SongBrowserSettings)serializer.Deserialize(fs);
                }
                catch (Exception e)
                {
                    Log.Exception("Unable to deserialize song browser settings file: ", e);
                    throw e;
                }
                finally
                {
                    if (fs != null) { fs.Close(); }
                }
            }
            else
            {
                Log.Debug("Settings file does not exist, returning defaults: " + settingsFilePath);
                retVal = new SongBrowserSettings();
            }

            // Load favorites
            if (File.Exists(SongBrowserSettings.DownloaderFavoritesFilePath()))
            {
                String[] downloaderFavorites = File.ReadAllLines(SongBrowserSettings.DownloaderFavoritesFilePath());
                retVal.Favorites.UnionWith(downloaderFavorites);
            }

            return retVal;
        }

        /// <summary>
        /// Save this Settings insance to file.
        /// </summary>
        public void Save()
        {            
            // TODO - not here
            if (searchTerms.Count > 10)
            {
                searchTerms.RemoveRange(10, searchTerms.Count - 10);
            }

            FileStream fs = new FileStream(SongBrowserSettings.SettingsPath(), FileMode.Create, FileAccess.Write);            
            XmlSerializer serializer = new XmlSerializer(typeof(SongBrowserSettings));           
            serializer.Serialize(fs, this);            
            fs.Close();
        }
    }
}

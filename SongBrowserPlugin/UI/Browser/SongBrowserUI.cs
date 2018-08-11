﻿using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using HMUI;
using VRUI;
using SongBrowserPlugin.DataAccess;
using System.IO;
using SongLoaderPlugin;
using System.Security.Cryptography;
using System.Text;

namespace SongBrowserPlugin.UI
{
    /// <summary>
    /// Hijack the flow coordinator.  Have access to all StandardLevel easily.
    /// </summary>
    public class SongBrowserUI : MonoBehaviour
    {
        // Logging
        public const String Name = "SongBrowserUI";

        private const float SEGMENT_PERCENT = 0.1f;

        private Logger _log = new Logger(Name);

        // Beat Saber UI Elements
        private StandardLevelSelectionFlowCoordinator _levelSelectionFlowCoordinator;
        private StandardLevelListViewController _levelListViewController;
        private StandardLevelDetailViewController _levelDetailViewController;
        private StandardLevelDifficultyViewController _levelDifficultyViewController;
        private StandardLevelSelectionNavigationController _levelSelectionNavigationController;
        private StandardLevelListTableView _levelListTableView;
        private RectTransform _tableViewRectTransform;
        private Button _tableViewPageUpButton;
        private Button _tableViewPageDownButton;
        private Button _playButton;

        // New UI Elements
        private List<SongSortButton> _sortButtonGroup;
        private List<SongFilterButton> _filterButtonGroup;
        private Button _addFavoriteButton;
        private SimpleDialogPromptViewController _simpleDialogPromptViewControllerPrefab;
        private SimpleDialogPromptViewController _deleteDialog;
        private Button _deleteButton;        
        private Button _pageUpTenPercent;
        private Button _pageDownTenPercent;
        private Button _enterFolderButton;
        private Button _upFolderButton;
        private SearchKeyboardViewController _searchViewController;
        private PlaylistFlowCoordinator _playListFlowCoordinator;

        // Cached items
        private Sprite _addFavoriteSprite;
        private Sprite _removeFavoriteSprite;
        private Sprite _currentAddFavoriteButtonSprite;

        // Debug
        private int _sortButtonLastPushedIndex = 0;
        private int _lastRow = 0;

        // Model
        private SongBrowserModel _model;

        /// <summary>
        /// Constructor
        /// </summary>
        public SongBrowserUI() : base()
        {
            if (_model == null)
            {
                _model = new SongBrowserModel();
            }
            _model.Init();
            _sortButtonLastPushedIndex = (int)(_model.Settings.sortMode);
        }

        /// <summary>
        /// Builds the UI for this plugin.
        /// </summary>
        public void CreateUI()
        {
            _log.Trace("CreateUI()");
            try
            {
                if (_levelSelectionFlowCoordinator == null)
                {
                    _levelSelectionFlowCoordinator = Resources.FindObjectsOfTypeAll<StandardLevelSelectionFlowCoordinator>().First();
                }

                if (_levelListViewController == null)
                {
                    _levelListViewController = _levelSelectionFlowCoordinator.GetPrivateField<StandardLevelListViewController>("_levelListViewController");
                }

                if (_levelDetailViewController == null)
                {
                    _levelDetailViewController = _levelSelectionFlowCoordinator.GetPrivateField<StandardLevelDetailViewController>("_levelDetailViewController");
                }

                if (_levelSelectionNavigationController == null)
                {
                    _levelSelectionNavigationController = _levelSelectionFlowCoordinator.GetPrivateField<StandardLevelSelectionNavigationController>("_levelSelectionNavigationController");
                }

                if (_levelDifficultyViewController == null)
                {
                    _levelDifficultyViewController = _levelSelectionFlowCoordinator.GetPrivateField<StandardLevelDifficultyViewController>("_levelDifficultyViewController");
                }

                if (_levelListTableView == null)
                {
                    _levelListTableView = this._levelListViewController.GetComponentInChildren<StandardLevelListTableView>();
                }

                _playButton = _levelDetailViewController.GetComponentsInChildren<Button>().FirstOrDefault(x => x.name == "PlayButton");

                _simpleDialogPromptViewControllerPrefab = Resources.FindObjectsOfTypeAll<SimpleDialogPromptViewController>().First();

                this._deleteDialog = UnityEngine.Object.Instantiate<SimpleDialogPromptViewController>(this._simpleDialogPromptViewControllerPrefab);
                this._deleteDialog.gameObject.SetActive(false);

                this._addFavoriteSprite = Base64Sprites.Base64ToSprite(Base64Sprites.AddToFavoritesIcon);
                this._removeFavoriteSprite = Base64Sprites.Base64ToSprite(Base64Sprites.RemoveFromFavoritesIcon);

                this.CreateUIElements();

                _levelListViewController.didSelectLevelEvent += OnDidSelectLevelEvent;
            }
            catch (Exception e)
            {
                _log.Exception("Exception during CreateUI: ", e);
            }
        }

        /// <summary>
        /// Builds the SongBrowser UI
        /// </summary>
        private void CreateUIElements()
        {
            _log.Trace("CreateUIElements");

            try
            {
                // Gather some transforms and templates to use.
                RectTransform sortButtonTransform = this._levelSelectionNavigationController.transform as RectTransform;
                RectTransform otherButtonTransform = this._levelDetailViewController.transform as RectTransform;
                Button sortButtonTemplate = _playButton;
                Button otherButtonTemplate = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "QuitButton"));
                Sprite arrowIcon = SongBrowserApplication.Instance.CachedIcons["ArrowIcon"];

                // Resize some of the UI
                _tableViewRectTransform = _levelListViewController.GetComponentsInChildren<RectTransform>().First(x => x.name == "TableViewContainer");
                _tableViewRectTransform.sizeDelta = new Vector2(0f, -20f);
                _tableViewRectTransform.anchoredPosition = new Vector2(0f, -2.5f);

                _tableViewPageUpButton = _tableViewRectTransform.GetComponentsInChildren<Button>().First(x => x.name == "PageUpButton");
                (_tableViewPageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -1f);

                _tableViewPageDownButton = _tableViewRectTransform.GetComponentsInChildren<Button>().First(x => x.name == "PageDownButton");
                (_tableViewPageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 1f);

                // Create Sorting Songs By-Buttons
                _log.Debug("Creating sort by buttons...");
                                
                float fontSize = 2.35f;
                float buttonWidth = 13.50f;
                float buttonHeight = 5.5f;
                float buttonX = -61;
                float buttonY = 74.0f;

                string[] sortButtonNames = new string[]
                {
                    "Song", "Author", "Original", "Newest", "Plays", "Difficult", "Random"
                };

                SongSortMode[] sortModes = new SongSortMode[]
                {
                    SongSortMode.Default, SongSortMode.Author, SongSortMode.Original, SongSortMode.Newest, SongSortMode.PlayCount, SongSortMode.Difficulty, SongSortMode.Random
                };

                _sortButtonGroup = new List<SongSortButton>();
                for (int i = 0; i < sortButtonNames.Length; i++)
                {
                    _sortButtonGroup.Add(UIBuilder.CreateSortButton(sortButtonTransform, sortButtonTemplate, arrowIcon,
                        sortButtonNames[i], 
                        fontSize, 
                        buttonX + (buttonWidth * i), 
                        buttonY, 
                        buttonWidth, 
                        buttonHeight, 
                        sortModes[i],
                        onSortButtonClickEvent));
                }

                // Create filter buttons
                float filterButtonX = buttonX + (buttonWidth * (sortButtonNames.Length - 1)) + (buttonWidth / 2.0f) + 2.5f;
                Vector2 iconButtonSize = new Vector2(5.5f, buttonHeight);
                Sprite playlistSprite = Base64Sprites.Base64ToSprite(Base64Sprites.PlaylistIcon);
                Sprite searchSprite = Base64Sprites.Base64ToSprite(Base64Sprites.SearchIcon);

                List<Tuple<SongFilterMode, UnityEngine.Events.UnityAction, Sprite>> filterButtonSetup = new List<Tuple<SongFilterMode, UnityEngine.Events.UnityAction, Sprite>>()
                {
                    Tuple.Create<SongFilterMode, UnityEngine.Events.UnityAction, Sprite>(SongFilterMode.Favorites, onFavoriteFilterButtonClickEvent, _addFavoriteSprite),
                    Tuple.Create<SongFilterMode, UnityEngine.Events.UnityAction, Sprite>(SongFilterMode.Playlist, onPlaylistButtonClickEvent, playlistSprite),
                    Tuple.Create<SongFilterMode, UnityEngine.Events.UnityAction, Sprite>(SongFilterMode.Search, onSearchButtonClickEvent, searchSprite),
                };

                _filterButtonGroup = new List<SongFilterButton>();
                for (int i = 0; i < filterButtonSetup.Count; i++)
                {
                    Tuple<SongFilterMode, UnityEngine.Events.UnityAction, Sprite> t = filterButtonSetup[i];
                    Button b = UIBuilder.CreateIconButton(sortButtonTransform, sortButtonTemplate,
                        t.Item3,
                        new Vector2(filterButtonX + (iconButtonSize.x * i), buttonY),
                        new Vector2(iconButtonSize.x, iconButtonSize.y),
                        new Vector2(0, 0),
                        new Vector2(3.5f, 3.5f),
                        new Vector2(1.0f, 1.0f),
                        0);
                    SongFilterButton filterButton = new SongFilterButton();
                    filterButton.Button = b;
                    filterButton.FilterMode = t.Item1;
                    b.onClick.AddListener(t.Item2);
                    _filterButtonGroup.Add(filterButton);
                }
               
                // Create Add to Favorites Button
                Vector2 addFavoritePos = new Vector2(40f, (sortButtonTemplate.transform as RectTransform).anchoredPosition.y);
                _addFavoriteButton = UIBuilder.CreateIconButton(otherButtonTransform, otherButtonTemplate, null, 
                    new Vector2(addFavoritePos.x, addFavoritePos.y), 
                    new Vector2(10.0f, 10.0f), 
                    new Vector2(2f, -1.5f),
                    new Vector2(7.0f, 7.0f), 
                    new Vector2(1.0f, 1.0f),
                    0.0f);
                _addFavoriteButton.onClick.AddListener(delegate () {
                    ToggleSongInFavorites();
                });

                if (_currentAddFavoriteButtonSprite == null)
                {
                    IStandardLevel level = this._levelListViewController.selectedLevel;
                    if (level != null)
                    {
                        RefreshAddFavoriteButton(level.levelID);
                    }                    
                }

                // Create delete button
                _deleteButton = UIBuilder.CreateButton(otherButtonTransform, otherButtonTemplate, "Delete", fontSize, 46f, 0f, 15f, 5f);                
                _deleteButton.onClick.AddListener(delegate () {
                    HandleDeleteSelectedLevel();
                });

                // Create fast scroll buttons
                _pageUpTenPercent = UIBuilder.CreateIconButton(sortButtonTransform, otherButtonTemplate, arrowIcon,
                    new Vector2(15, 67.5f),
                    new Vector2(6.0f, 5.5f),
                    new Vector2(0f, 0f),
                    new Vector2(1.5f, 1.5f),
                    new Vector2(2.0f, 2.0f), 
                    180);
                _pageUpTenPercent.onClick.AddListener(delegate () {
                    this.JumpSongList(-1, SEGMENT_PERCENT);
                });

                _pageDownTenPercent = UIBuilder.CreateIconButton(sortButtonTransform, otherButtonTemplate, arrowIcon,
                    new Vector2(15, 0.5f),
                    new Vector2(6.0f, 5.5f),
                    new Vector2(0f, 0f),
                    new Vector2(1.5f, 1.5f),
                    new Vector2(2.0f, 2.0f), 
                    0);
                _pageDownTenPercent.onClick.AddListener(delegate () {
                    this.JumpSongList(1, SEGMENT_PERCENT);
                });

                // Create enter folder button
                if (_model.Settings.folderSupportEnabled)
                {
                    _enterFolderButton = UIBuilder.CreateUIButton(otherButtonTransform, _playButton);
                    _enterFolderButton.onClick.AddListener(delegate ()
                    {
                        _model.PushDirectory(_levelListViewController.selectedLevel);
                        this.RefreshSongList();
                        this.RefreshDirectoryButtons();
                    });
                    UIBuilder.SetButtonText(ref _enterFolderButton, "Enter");

                    // Create up folder button
                    _upFolderButton = UIBuilder.CreateIconButton(sortButtonTransform, sortButtonTemplate, arrowIcon,
                        new Vector2(filterButtonX + (iconButtonSize.x* filterButtonSetup.Count), buttonY),
                        new Vector2(iconButtonSize.x, iconButtonSize.y),
                        new Vector2(0f, 0f),
                        new Vector2(0.85f, 0.85f),
                        new Vector2(2.0f, 2.0f),
                        180);
                    _upFolderButton.onClick.RemoveAllListeners();
                    _upFolderButton.onClick.AddListener(delegate ()
                    {
                        _model.PopDirectory();
                        this.RefreshSongList();
                        this.RefreshDirectoryButtons();
                    });
                }

                RefreshSortButtonUI();
                RefreshDirectoryButtons();
            }
            catch (Exception e)
            {
                _log.Exception("Exception CreateUIElements:", e);
            }
        }

        /// <summary>
        /// Sort button clicked.
        /// </summary>
        private void onSortButtonClickEvent(SongSortMode sortMode)
        {
            _log.Debug("Sort button - {0} - pressed.", sortMode.ToString());
            _model.LastSelectedLevelId = null;

            if (_model.Settings.sortMode == sortMode)
            {
                _model.ToggleInverting();
            }

            _model.Settings.sortMode = sortMode;
            _model.Settings.Save();

            UpdateSongList();
            RefreshSongList();

            // Handle instant queue logic, avoid picking a folder.
            if (_model.Settings.sortMode == SongSortMode.Random && _model.Settings.randomInstantQueue)
            {
                for (int i = 0; i < _model.SortedSongList.Count; i++)
                {
                    if (!_model.SortedSongList[i].levelID.StartsWith("Folder_"))
                    {
                        this.SelectAndScrollToLevel(_levelListTableView, _model.SortedSongList[i].levelID);
                        this._levelDifficultyViewController.HandleDifficultyTableViewDidSelectRow(null, _model.SortedSongList[i].difficultyBeatmaps.Length-1);
                        _playButton.onClick.Invoke();
                        break;
                    }
                }                                                    
            }
        }

        /// <summary>
        /// Filter by favorites.
        /// </summary>
        private void onFavoriteFilterButtonClickEvent()
        {
            _log.Debug("Filter button - {0} - pressed.", SongFilterMode.Favorites.ToString());

            if (_model.Settings.filterMode != SongFilterMode.Favorites)
            {
                _model.Settings.filterMode = SongFilterMode.Favorites;
            }
            else
            {
                _model.Settings.filterMode = SongFilterMode.None;
            }
            _model.Settings.Save();

            UpdateSongList();
            RefreshSongList();
        }

        /// <summary>
        /// Filter button clicked.  
        /// </summary>
        /// <param name="sortMode"></param>
        private void onSearchButtonClickEvent()
        {
            _log.Debug("Filter button - {0} - pressed.", SongFilterMode.Search.ToString());
            if (_model.Settings.filterMode != SongFilterMode.Search)
            {
                _model.Settings.filterMode = SongFilterMode.Search;
                this.ShowSearchKeyboard();
            }
            else
            {
                _model.Settings.filterMode = SongFilterMode.None;
                UpdateSongList();
                RefreshSongList();
            }
            _model.Settings.Save();            
        }

        /// <summary>
        /// Display the playlist selector.
        /// </summary>
        /// <param name="sortMode"></param>
        private void onPlaylistButtonClickEvent()
        {
            _log.Debug("Filter button - {0} - pressed.", SongFilterMode.Playlist.ToString());
            _model.LastSelectedLevelId = null;

            if (_model.Settings.filterMode != SongFilterMode.Playlist)
            {
                if (_playListFlowCoordinator == null || !_playListFlowCoordinator.isActiveAndEnabled)
                {
                    _playListFlowCoordinator = UIBuilder.CreateFlowCoordinator<PlaylistFlowCoordinator>("PlaylistFlowCoordinator");
                    _playListFlowCoordinator.didSelectPlaylist += HandleDidSelectPlaylist;
                    _playListFlowCoordinator.Present(_levelSelectionNavigationController);
                }                
            }
            else
            {
                _model.Settings.filterMode = SongFilterMode.None;
                _model.Settings.Save();
                UpdateSongList();
                RefreshSongList();
            }
        }

        /// <summary>
        /// Adjust UI based on level selected.
        /// Various ways of detecting if a level is not properly selected.  Seems most hit the first one.
        /// </summary>
        private void OnDidSelectLevelEvent(StandardLevelListViewController view, IStandardLevel level)
        {            
            try
            {
                _log.Trace("OnDidSelectLevelEvent()");
                if (level == null)
                {
                    _log.Debug("No level selected?");
                    return;
                }

                if (_model.Settings == null)
                {
                    _log.Debug("Settings not instantiated yet?");
                    return;
                }

                _model.LastSelectedLevelId = level.levelID;

                RefreshAddFavoriteButton(level.levelID);
                RefreshQuickScrollButtons();

                if (level.levelID.StartsWith("Folder_"))
                {
                    _log.Debug("Folder selected!  Adjust PlayButton logic...");
                    HandleDidSelectFolderRow(level);
                }
                else
                {
                    HandleDidSelectLevelRow(level);
                }
            }
            catch (Exception e)
            {
                _log.Exception("Exception selecting song:", e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void HandleDidSelectFolderRow(IStandardLevel level)
        {
            _enterFolderButton.gameObject.SetActive(true);
            _playButton.gameObject.SetActive(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="level"></param>
        private void HandleDidSelectLevelRow(IStandardLevel level)
        {
            if (_enterFolderButton != null)
            {
                _enterFolderButton.gameObject.SetActive(false);
            }
            _playButton.gameObject.SetActive(true);
        }

        /// <summary>
        /// Pop up a delete dialog.
        /// </summary>
        private void HandleDeleteSelectedLevel()
        {
            IStandardLevel level = this._levelListViewController.selectedLevel;
            if (level == null)
            {
                _log.Info("No level selected, cannot delete nothing...");
                return;
            }

            if (level.levelID.StartsWith("Level"))
            {
                _log.Debug("Cannot delete non-custom levels.");
                return;
            }

            if (level.levelID.StartsWith("Folder"))
            {
                _log.Debug("Cannot delete folders.");
                return;
            }

            SongLoaderPlugin.OverrideClasses.CustomLevel customLevel = _model.LevelIdToCustomSongInfos[level.levelID];

            this._deleteDialog.Init("Delete level warning!", String.Format("<color=#00AAFF>Permanently delete level: {0}</color>\n  Do you want to continue?", customLevel.songName), "YES", "NO");
            this._deleteDialog.didFinishEvent += this.HandleDeleteDialogPromptViewControllerDidFinish;

            this._levelSelectionNavigationController.PresentModalViewController(this._deleteDialog, null, false);
        }

        /// <summary>
        /// Handle delete dialog resolution.
        /// </summary>
        /// <param name="viewController"></param>
        /// <param name="ok"></param>
        public void HandleDeleteDialogPromptViewControllerDidFinish(SimpleDialogPromptViewController viewController, bool ok)
        {
            viewController.didFinishEvent -= this.HandleDeleteDialogPromptViewControllerDidFinish;
            if (!ok)
            {
                viewController.DismissModalViewController(null, false);
            }
            else
            {
                string customSongsPath = Path.Combine(Environment.CurrentDirectory, "CustomSongs");
                IStandardLevel level = this._levelListViewController.selectedLevel;                
                SongLoaderPlugin.OverrideClasses.CustomLevel customLevel = _model.LevelIdToCustomSongInfos[level.levelID];
                string songPath = customLevel.customSongInfo.path;
                bool isZippedSong = false;

                viewController.DismissModalViewController(null, false);
                _log.Debug("Deleting: {0}", songPath);

                if (!string.IsNullOrEmpty(songPath) && songPath.Contains("/.cache/"))
                {
                    isZippedSong = true;
                }

                if (isZippedSong)
                {
                    DirectoryInfo songHashDir = Directory.GetParent(songPath);
                    _log.Debug("Deleting zipped song cache: {0}", songHashDir.FullName);
                    Directory.Delete(songHashDir.FullName, true);

                    foreach (string file in Directory.GetFiles(customSongsPath, "*.zip"))
                    {
                        string hash = CreateMD5FromFile(file);
                        if (hash != null)
                        {
                            if (hash == songHashDir.Name)
                            {
                                _log.Debug("Deleting zipped song: {0}", file);
                                File.Delete(file);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    FileAttributes attr = File.GetAttributes(songPath);
                    if (attr.HasFlag(FileAttributes.Directory))
                    {
                        _log.Debug("Deleting song: {0}", songPath);
                        Directory.Delete(songPath, true);
                    }
                }

                _model.LastSelectedLevelId = null;
                SongLoaderPlugin.SongLoader.Instance.RemoveSongWithPath(songPath);
                this.UpdateSongList();
                this.RefreshSongList();
            }
        }

        /// <summary>
        /// Create MD5 of a file.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string CreateMD5FromFile(string path)
        {
            string hash = "";
            if (!File.Exists(path)) return null;
            using (MD5 md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(path))
                {
                    byte[] hashBytes = md5.ComputeHash(stream);

                    StringBuilder sb = new StringBuilder();
                    foreach (byte hashByte in hashBytes)
                    {
                        sb.Append(hashByte.ToString("X2"));
                    }

                    hash = sb.ToString();
                    return hash;
                }
            }
        }

        /// <summary>
        /// Handle selection of a playlist.  Show just the songs in the playlist.
        /// </summary>
        /// <param name="p"></param>
        private void HandleDidSelectPlaylist(Playlist p)
        {
            if (_playListFlowCoordinator != null)
            {
                _levelSelectionNavigationController.PopViewControllerImmediately();
                _playListFlowCoordinator.gameObject.SetActive(false);
                UnityEngine.Object.DestroyImmediate(_playListFlowCoordinator);
            }

            if (p != null)
            {
                _log.Debug("Showing songs for playlist: {0}", p.playlistTitle);
                _model.Settings.filterMode = SongFilterMode.Playlist;
                _model.CurrentPlaylist = p;
                _model.Settings.Save();
            }
            else
            {
                _log.Debug("No playlist selected");
            }

            this.UpdateSongList();
            this.RefreshSongList();
        }

        /// <summary>
        /// Display the search keyboard
        /// </summary>
        void ShowSearchKeyboard()
        {
            if (_searchViewController == null)
            {
                _searchViewController = UIBuilder.CreateViewController<SearchKeyboardViewController>("SearchKeyboardViewController");
                _searchViewController.searchButtonPressed += SearchViewControllerSearchButtonPressed;
                _searchViewController.backButtonPressed += SearchViewControllerbackButtonPressed;
            }

            _log.Debug("Presenting keyboard");
            _levelListViewController.navigationController.PresentModalViewController(_searchViewController, null, false);
        }

        /// <summary>
        /// Handle back button event from search keyboard.
        /// </summary>
        private void SearchViewControllerbackButtonPressed()
        {
 
        }

        /// <summary>
        /// Handle search.
        /// </summary>
        /// <param name="searchFor"></param>
        private void SearchViewControllerSearchButtonPressed(string searchFor)
        {
            _log.Debug("Searching for \"{0}\"...", searchFor);

            _model.Settings.searchTerms.Insert(0, searchFor);
            _model.Settings.Save();
            _model.LastSelectedLevelId = null;
            this.UpdateSongList();
            this.RefreshSongList();
        }

        /// <summary>
        /// Make big jumps in the song list.
        /// </summary>
        /// <param name="numJumps"></param>
        private void JumpSongList(int numJumps, float segmentPercent)
        {
            int totalSize = _model.SortedSongList.Count;
            int segmentSize = (int)(totalSize * segmentPercent);

            TableView tableView = ReflectionUtil.GetPrivateField<TableView>(_levelListTableView, "_tableView");
            HashSet<int> rows = tableView.GetPrivateField<HashSet<int>>("_selectedRows");
            int listSegment = (rows.First() / segmentSize);
            int newSegment = listSegment + numJumps;
            int newRow = 0;
            if (newSegment > 0)
            {
                newRow = Math.Min(newSegment * segmentSize, totalSize - 1);
            }                       

            _log.Debug("ListSegment: {0}, newRow: {1}", listSegment, newRow);

            this.SelectAndScrollToLevel(_levelListTableView, _model.SortedSongList[newRow].levelID);
        }

        /// <summary>
        /// Add/Remove song from favorites depending on if it already exists.
        /// </summary>
        private void ToggleSongInFavorites()
        {
            IStandardLevel songInfo = this._levelListViewController.selectedLevel;
            if (_model.Settings.Favorites.Contains(songInfo.levelID))
            {
                _log.Info("Remove {0} from favorites", songInfo.songName);
                _model.Settings.Favorites.Remove(songInfo.levelID);
            }
            else
            {
                _log.Info("Add {0} to favorites", songInfo.songName);
                _model.Settings.Favorites.Add(songInfo.levelID);
            }

            RefreshAddFavoriteButton(songInfo.levelID);

            _model.Settings.Save();
        }

        /// <summary>
        /// Update interactive state of the quick scroll buttons.
        /// </summary>
        private void RefreshQuickScrollButtons()
        {
            // Refresh the fast scroll buttons
            _pageUpTenPercent.interactable = _tableViewPageUpButton.interactable;
            _pageUpTenPercent.gameObject.SetActive(_tableViewPageUpButton.IsActive());
            _pageDownTenPercent.interactable = _tableViewPageDownButton.interactable;
            _pageDownTenPercent.gameObject.SetActive(_tableViewPageDownButton.IsActive());
        }

        /// <summary>
        /// Helper to quickly refresh add to favorites button
        /// </summary>
        /// <param name="levelId"></param>
        private void RefreshAddFavoriteButton(String levelId)
        {
            if (levelId == null)
            {
                _currentAddFavoriteButtonSprite = null;
            }
            else
            {
                if (_model.Settings.Favorites.Contains(levelId))
                {
                    _currentAddFavoriteButtonSprite = _removeFavoriteSprite;
                }
                else
                {
                    _currentAddFavoriteButtonSprite = _addFavoriteSprite;
                }
            }

            UIBuilder.SetButtonIcon(ref _addFavoriteButton, _currentAddFavoriteButtonSprite);
        }

        /// <summary>
        /// Adjust the UI colors.
        /// </summary>
        public void RefreshSortButtonUI()
        {
            // So far all we need to refresh is the sort buttons.
            foreach (SongSortButton sortButton in _sortButtonGroup)
            {
                UIBuilder.SetButtonBorder(ref sortButton.Button, Color.black);
                if (sortButton.SortMode == _model.Settings.sortMode)
                {
                    if (_model.InvertingResults)
                    {
                        UIBuilder.SetButtonBorder(ref sortButton.Button, Color.red);
                    }
                    else
                    {
                        UIBuilder.SetButtonBorder(ref sortButton.Button, Color.green);
                    }
                }
            }    
            
            // refresh filter buttons
            foreach (SongFilterButton filterButton in _filterButtonGroup)
            {
                UIBuilder.SetButtonBorder(ref filterButton.Button, Color.clear);
                if (filterButton.FilterMode == _model.Settings.filterMode)
                {
                    UIBuilder.SetButtonBorder(ref filterButton.Button, Color.green);
                }
            }           
        }

        /// <summary>
        /// Refresh the UI state of any directory buttons.
        /// </summary>
        public void RefreshDirectoryButtons()
        {
            // bail if no button, likely folder support not enabled.
            if (_upFolderButton == null)
            {
                return;
            }

            if (_model.DirStackSize > 1)
            {
                _upFolderButton.interactable = true;
            }
            else
            {
                _upFolderButton.interactable = false;
            }
        }

        /// <summary>
        /// Try to refresh the song list.  Broken for now.
        /// </summary>
        public void RefreshSongList()
        {
            _log.Info("Refreshing the song list view.");
            try
            {
                if (_model.SortedSongList == null)
                {
                    _log.Debug("Songs are not sorted yet, nothing to refresh.");
                    return;
                }

                StandardLevelSO[] levels = _model.SortedSongList.ToArray();
                StandardLevelListViewController songListViewController = this._levelSelectionFlowCoordinator.GetPrivateField<StandardLevelListViewController>("_levelListViewController");
                ReflectionUtil.SetPrivateField(_levelListTableView, "_levels", levels);
                ReflectionUtil.SetPrivateField(songListViewController, "_levels", levels);            
                TableView tableView = ReflectionUtil.GetPrivateField<TableView>(_levelListTableView, "_tableView");
                tableView.ReloadData();                

                String selectedLevelID = null;
                if (_model.LastSelectedLevelId != null)
                {
                    selectedLevelID = _model.LastSelectedLevelId;
                    _log.Debug("Scrolling to row for level ID: {0}", selectedLevelID);                    
                }
                else
                {
                    if (levels.Length > 0)
                    {
                        selectedLevelID = levels.FirstOrDefault().levelID;
                    }
                }

                // HACK, seems like if 6 or less items scrolling to row causes the song list to disappear.
                if (levels.Length > 6 && !String.IsNullOrEmpty(selectedLevelID) && levels.Any(x => x.levelID == selectedLevelID))
                {
                    SelectAndScrollToLevel(_levelListTableView, selectedLevelID);
                }

                RefreshSortButtonUI();
                RefreshQuickScrollButtons();
            }
            catch (Exception e)
            {
                _log.Exception("Exception refreshing song list:", e);
            }
        }

        /// <summary>
        /// Scroll TableView to proper row, fire events.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="levelID"></param>
        private void SelectAndScrollToLevel(StandardLevelListTableView table, string levelID)
        {
            int row = table.RowNumberForLevelID(levelID);
            TableView tableView = table.GetComponentInChildren<TableView>();
            tableView.SelectRow(row, true);
            tableView.ScrollToRow(row, true);
            _lastRow = row;
        }

        /// <summary>
        /// Helper for updating the model (which updates the song list)c
        /// </summary>
        public void UpdateSongList()
        {
            _log.Trace("UpdateSongList()");

            GameplayMode gameplayMode = _levelSelectionFlowCoordinator.GetPrivateField<GameplayMode>("_gameplayMode");
            _model.UpdateSongLists(gameplayMode);
            this.RefreshDirectoryButtons();
        }

        /// <summary>
        /// Not normally called by the game-engine.  Dependent on SongBrowserApplication to call it.
        /// </summary>
        public void LateUpdate()
        {
            CheckDebugUserInput();
        }

        /// <summary>
        /// Map some key presses directly to UI interactions to make testing easier.
        /// </summary>
        private void CheckDebugUserInput()
        {
            try
            {
                if (this._levelListViewController != null && this._levelListViewController.isActiveAndEnabled)
                {
                    bool isShiftKeyDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                    // back
                    if (Input.GetKeyDown(KeyCode.Escape))
                    {
                        this._levelSelectionNavigationController.DismissButtonWasPressed();
                    }

                    // cycle sort modes
                    if (Input.GetKeyDown(KeyCode.T))
                    {
                        _sortButtonLastPushedIndex = (_sortButtonLastPushedIndex + 1) % _sortButtonGroup.Count;
                        _sortButtonGroup[_sortButtonLastPushedIndex].Button.onClick.Invoke();
                    }

                    // select current sort mode again (toggle inverting)
                    if (Input.GetKeyDown(KeyCode.Y))
                    {
                        _sortButtonGroup[_sortButtonLastPushedIndex].Button.onClick.Invoke();
                    }

                    // filter playlists
                    if (Input.GetKeyDown(KeyCode.P))
                    {
                        onPlaylistButtonClickEvent();
                    }

                    // filter search
                    if (Input.GetKeyDown(KeyCode.S))
                    {
                        onSearchButtonClickEvent();
                    }

                    // filter favorites
                    if (Input.GetKeyDown(KeyCode.F))
                    {
                        onFavoriteFilterButtonClickEvent();
                    }

                    // delete
                    if (Input.GetKeyDown(KeyCode.D))
                    {
                        _deleteButton.onClick.Invoke();
                    }

                    // c - select difficulty for top song
                    if (Input.GetKeyDown(KeyCode.C))
                    {
                        this.SelectAndScrollToLevel(_levelListTableView, _model.SortedSongList[0].levelID);
                        this._levelDifficultyViewController.HandleDifficultyTableViewDidSelectRow(null, 0);
                        this._levelSelectionFlowCoordinator.HandleDifficultyViewControllerDidSelectDifficulty(_levelDifficultyViewController, _model.SortedSongList[0].GetDifficultyLevel(LevelDifficulty.Easy));
                    }

                    // v start a song or enter a folder
                    if (Input.GetKeyDown(KeyCode.Return))
                    {
                        if (_playButton.isActiveAndEnabled)
                        {
                            _playButton.onClick.Invoke();
                        }
                        else if (_enterFolderButton.isActiveAndEnabled)
                        {
                            _enterFolderButton.onClick.Invoke();
                        }
                    }

                    // backspace - up a folder
                    if (Input.GetKeyDown(KeyCode.Backspace))
                    {
                        _upFolderButton.onClick.Invoke();
                    }

                    // change song index
                    if (isShiftKeyDown && Input.GetKeyDown(KeyCode.N))
                    {
                        _pageUpTenPercent.onClick.Invoke();
                    }
                    else if (Input.GetKeyDown(KeyCode.N))
                    {
                        _lastRow = (_lastRow - 1) != -1 ? (_lastRow - 1) % this._model.SortedSongList.Count : 0;
                        this.SelectAndScrollToLevel(_levelListTableView, _model.SortedSongList[_lastRow].levelID);
                    }

                    if (isShiftKeyDown && Input.GetKeyDown(KeyCode.M))
                    {
                        _pageDownTenPercent.onClick.Invoke();
                    }
                    else if (Input.GetKeyDown(KeyCode.M))
                    {
                        _lastRow = (_lastRow + 1) % this._model.SortedSongList.Count;
                        this.SelectAndScrollToLevel(_levelListTableView, _model.SortedSongList[_lastRow].levelID);
                    }

                    // add to favorites
                    if (Input.GetKeyDown(KeyCode.KeypadPlus))
                    {
                        ToggleSongInFavorites();
                    }
                }
                else if (_deleteDialog != null && _deleteDialog.isInViewControllerHierarchy)
                {
                    // accept delete
                    if (Input.GetKeyDown(KeyCode.Return))
                    {
                        _deleteDialog.GetPrivateField<TextMeshProButton>("_okButton").button.onClick.Invoke();
                    }

                    if (Input.GetKeyDown(KeyCode.Escape))
                    {
                        _deleteDialog.GetPrivateField<TextMeshProButton>("_cancelButton").button.onClick.Invoke();
                    }
                }
            }
            catch (Exception e)
            {
                _log.Exception("Debug Input caused Exception: ", e);
            }
        }
    }
}
 
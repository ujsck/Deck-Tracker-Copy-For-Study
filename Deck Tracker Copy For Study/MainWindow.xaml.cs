﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using MahApps.Metro.Controls;
using Microsoft.Win32;

namespace Deck_Tracker_Copy_For_Study
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private readonly Config _config;
        private readonly XmlManager<Config> _xmlManagerConfig;
        private readonly Decks _decks;
        private readonly XmlManager<Decks> _xmlManagerDecks;
        private readonly Hearthstone _hearthstone;
        private readonly LogReader _logReader;
        private readonly bool _initialized;
        private readonly Thread _updateThread;
        private readonly OverlayWindow _overlay;
        private readonly PlayerWindow _playerWindow;
        private readonly OpponentWindow _opponentWindow;
        private Deck _newDeck;
        private bool _editingDeck;
        private bool _newContainsDeck;
        private readonly Version _newVersion;
        private readonly string _logConfigPath =
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Blizzard\Hearthstone\log.config";

        public MainWindow()
        {
            InitializeComponent();
            //var version = Helper.CheckForUpdates(out _newVersion);
            //if (version != null)
            //{
            //    TxtblockVersion.Text = string.Format("Version: {0}.{1}.{2}", version.Major, version.Minor,
            //                                         version.Build);
            //}

            try
            {
                if (!File.Exists(_logConfigPath))
                {
                    File.Copy("log.config", _logConfigPath);
                }
                else
                {
                    //update log.config if newer
                    var localFile = new FileInfo(_logConfigPath);
                    var file = new FileInfo("log.config");
                    if (file.LastWriteTime > localFile.LastWriteTime)
                        File.Copy("log.config", _logConfigPath, true);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine(@"Not authorized write " + _logConfigPath + @". Start as admin(?)");
                Console.WriteLine(ex.Message);
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
            }

            //load config
            _config = new Config();
            _xmlManagerConfig = new XmlManager<Config> { Type = typeof(Config) };
            _config = _xmlManagerConfig.Load("config.xml");

            //load saved decks
            if (!File.Exists("PlayerDecks.xml"))
            {
                //avoid overwriting decks file with new releases.
                using (var sr = new StreamWriter("PlayerDecks.xml", false))
                {
                    sr.WriteLine("<Decks></Decks>");
                }
            }
            _xmlManagerDecks = new XmlManager<Decks> { Type = typeof(Decks) };
            _decks = _xmlManagerDecks.Load("PlayerDecks.xml");

            //add saved decks to gui
            //foreach (var deck in _decks.DecksList)
            //{
            //    ComboBoxDecks.Items.Add(deck.Name);
            //}
            //ComboBoxDecks.SelectedItem = _config.LastDeck;
            ListboxDecks.ItemsSource = _decks.DecksList;

            //hearthstone, loads db etc
            string languageTag = _config.SelectedLanguage;
            _hearthstone = Helper.LanguageDict.ContainsValue(languageTag) ? new Hearthstone(languageTag) : new Hearthstone("enUS");
            _newDeck = new Deck();
            ListViewNewDeck.ItemsSource = _newDeck.Cards;

            //create overlay
            _overlay = new OverlayWindow(_config, _hearthstone) { Topmost = true };
            //_overlay.Show();

            _playerWindow = new PlayerWindow(_config, _hearthstone.PlayerDeck);
            _opponentWindow = new OpponentWindow(_config, _hearthstone.EnemyCards);

            //load config
            _xmlManagerConfig = new XmlManager<Config> { Type = typeof(Config) };
            _config = _xmlManagerConfig.Load("config.xml");
            this.LoadConfig();

            //find hs directory
            if (!File.Exists(_config.HearthstoneDirectory + @"\Hearthstone.exe"))
            {
                MessageBox.Show("Please specify your Hearthstone directory", "Hearthstone directory not found",
                                MessageBoxButton.OK);
                var dialog = new OpenFileDialog
                {
                    Title = "Select Hearthstone.exe",
                    DefaultExt = "Hearthstone.exe",
                    Filter = "Hearthstone.exe|Hearthstone.exe"
                };
                var result = dialog.ShowDialog();
                if (result != true)
                    return;
                _config.HearthstoneDirectory = Path.GetDirectoryName(dialog.FileName);
                _xmlManagerConfig.Save("config.xml", _config);
            }

            //log reader
            _logReader = new LogReader(_config.HearthstoneDirectory);
            _logReader.CardMovement += LogReaderOnCardMovement;
            _logReader.GameStateChange += LogReaderOnGameStateChange;

            //update ..
            UpdateDbListView(); // Show Cards In Db ListView

            _updateThread = new Thread(Update);
            _updateThread.Start();
            ListboxDecks.SelectedItem = _decks.DecksList.FirstOrDefault(d => d.Name != null && d.Name == _config.LastDeck);

            _initialized = true;

            UpdateDeckList(ListboxDecks.SelectedItem as Deck);
            UseDeck(ListboxDecks.SelectedItem as Deck);

            _logReader.Start();
        }

        private void LogReaderOnCardMovement(LogReader sender, CardMovementArgs args)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                switch (args.MovementType)
                {
                    case CardMovementType.PlayerGet:
                        HandlePlayerGet(args.CardId);
                        break;
                    case CardMovementType.PlayerDraw:
                        HandlePlayerDraw(args.CardId);
                        break;
                    case CardMovementType.PlayerMulligan:
                        HandlePlayerMulligan(args.CardId);
                        break;
                    case CardMovementType.PlayerHandDiscard:
                        HandlePlayerHandDiscard(args.CardId);
                        break;
                    case CardMovementType.PlayerPlay:
                        HandlePlayerPlay(args.CardId);
                        break;
                    case CardMovementType.PlayerDeckDiscard:
                        HandlePlayerDeckDiscard(args.CardId);
                        break;
                    case CardMovementType.OpponentSecretTrigger:
                        HandleOpponentSecretTrigger(args.CardId);
                        break;
                    case CardMovementType.OpponentPlay:
                        HandleOpponentPlay(args.CardId);
                        break;
                    case CardMovementType.OpponentMulligan:
                        HandleOpponentMulligan();
                        break;
                    case CardMovementType.OpponentHandDiscard:
                        HandleOpponentHandDiscard();
                        break;
                    case CardMovementType.OpponentDraw:
                        HandleOpponentDraw();
                        break;
                    case CardMovementType.OpponentDeckDiscard:
                        HandleOpponentDeckDiscard();
                        break;
                    default:
                        Console.WriteLine("Invalid card movement");
                        break;
                }
            }));
            //_overlay.Dispatcher.BeginInvoke(new Action(_overlay.Update));
        }

        private void LogReaderOnGameStateChange(LogReader sender, GameStateArgs args)
        {
            switch (args.State)
            {
                case GameState.GameBegin:
                    HandleGameStart();
                    break;
                case GameState.GameEnd:
                    HandleGameEnd();
                    break;
            }
        }

        #region Handle Events
        private void HandleGameStart()
        {

        }

        private void HandleGameEnd()
        {

        }

        private void HandlePlayerGet(string cardId)
        {
            _hearthstone.PlayerGet(cardId);
        }

        private void HandlePlayerDraw(string cardId)
        {
            _hearthstone.PlayerDraw(cardId);
        }

        private void HandlePlayerMulligan(string cardId)
        {
            _hearthstone.Mulligan(cardId);
        }

        private void HandlePlayerHandDiscard(string cardId)
        {
            _hearthstone.PlayerHandDiscard(cardId);
        }

        private void HandlePlayerPlay(string cardId)
        {
            _hearthstone.PlayerPlayed(cardId);
        }

        private void HandlePlayerDeckDiscard(string cardId)
        {
            _hearthstone.PlayerDeckDiscard(cardId);
        }

        private void HandleOpponentSecretTrigger(string cardId)
        {
            _hearthstone.EnemySecretTriggered(cardId);
        }

        private void HandleOpponentPlay(string cardId)
        {
            _hearthstone.EnemyPlayed(cardId);
        }

        private void HandleOpponentMulligan()
        {
            _hearthstone.EnemyMulligan();
        }

        private void HandleOpponentHandDiscard()
        {
            _hearthstone.EnemyHandDiscard();
        }

        private void HandleOpponentDraw()
        {
            _hearthstone.EnemyDraw();
        }

        private void HandleOpponentDeckDiscard()
        {
            _hearthstone.EnemyDeckDiscard();
        }
        #endregion

        private void SortCardCollection(ItemCollection collection)
        {
            var view1 = (CollectionView)CollectionViewSource.GetDefaultView(collection);
            view1.SortDescriptions.Add(new SortDescription("Cost", ListSortDirection.Ascending));
            view1.SortDescriptions.Add(new SortDescription("Type", ListSortDirection.Descending));
            view1.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }

        private void AddCardToDeck(Card card)
        {
            if (_newDeck.Cards.Contains(card))
            {
                var cardInDeck = _newDeck.Cards.First(c => c.Name == card.Name);
                if (cardInDeck.Count > 1 || cardInDeck.Rarity == "Legendary")
                {
                    if (
                        MessageBox.Show(
                            "Are you sure you want to add " + cardInDeck.Count + " of this card to the deck?\n(will not be displayed correctly)",
                            "More than  " + cardInDeck.Count + " cards", MessageBoxButton.YesNo, MessageBoxImage.Asterisk) !=
                        MessageBoxResult.Yes)
                    {
                        return;
                    }
                }
                _newDeck.Cards.Remove(cardInDeck);
                cardInDeck.Count++;
                _newDeck.Cards.Add(cardInDeck);
            }
            else
            {
                _newDeck.Cards.Add(card);
            }

            SortCardCollection(ListViewNewDeck.Items);
            BtnSaveDeck.Content = "Save*";
        }

        private void RemoveCardFromDeck(Card card)
        {
            if (card.Count > 1)
            {
                _newDeck.Cards.Remove(card);
                card.Count--;
                _newDeck.Cards.Add(card);
            }
            else
                _newDeck.Cards.Remove(card);

            SortCardCollection(ListViewNewDeck.Items);
            BtnSaveDeck.Content = "Save*";
        }

        private void ClearNewDeckSection()
        {
            ComboBoxSelectClass.SelectedIndex = 0;
            TextBoxDeckName.Text = string.Empty;
            TextBoxDBFilter.Text = string.Empty;
            ComboBoxFilterMana.SelectedIndex = 0;
            _newDeck.Cards.Clear();
            _newDeck.Class = string.Empty;
            _newDeck.Name = string.Empty;
            _newContainsDeck = false;
            //_newDeck.Cards.Clear();
        }

        private void SaveDeck()
        {
            if (_newDeck.Cards.Sum(c => c.Count) != 30)
            {
                var result = MessageBox.Show(string.Format("Deck contains {0} cards. Is this what you want to save?", _newDeck.Cards.Sum(c => c.Count)),
                                             "Deck does not contain 30 cards.", MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes)
                    return;
            }
            var deckName = TextBoxDeckName.Text;
            if (string.IsNullOrEmpty(deckName))
            {
                MessageBox.Show("Please set a name for the deck.");
                return;
            }
            else if (_decks.DecksList.Any(d => d.Name == deckName) && !_editingDeck)
            {
                MessageBox.Show("You already have a deck with that name!");
                return;
            }
            if (_editingDeck)
            {
                _decks.DecksList.Remove(_newDeck);
            }
            _newDeck.Name = deckName;
            _newDeck.Class = ComboBoxSelectClass.SelectedValue.ToString();
            _decks.DecksList.Add((Deck)_newDeck.Clone());
            _xmlManagerDecks.Save("PlayerDecks.xml", _decks);
            BtnSaveDeck.Content = "Save";

            //move to decks
            TabControlTracker.SelectedIndex = 0;
            _editingDeck = false;

            ListboxDecks.SelectedItem = _decks.DecksList.First(d => d.Equals(_newDeck));

            ClearNewDeckSection();
        }

        private void LoadConfig()
        {
            var deck = _decks.DecksList.FirstOrDefault(d => d.Name == _config.LastDeck);
            if (deck != null && ListboxDecks.Items.Contains(deck))
            {
                ListboxDecks.SelectedItem = deck;
            }

            // Height = _config.WindowHeight;
            Hearthstone.HighlightCardsInHand = _config.HighlightCardsInHand;
            CheckboxHideOverlayInBackground.IsChecked = _config.HideInBackground;
            CheckboxHideDrawChances.IsChecked = _config.HideDrawChances;
            CheckboxHideEnemyCards.IsChecked = _config.HideEnemyCards;
            CheckboxHideEnemyCardCounter.IsChecked = _config.HideEnemyCardCount;
            CheckboxHidePlayerCardCounter.IsChecked = _config.HidePlayerCardCount;
            CheckboxHideOverlayInMenu.IsChecked = _config.HideInMenu;
            CheckboxHighlightCardsInHand.IsChecked = _config.HighlightCardsInHand;
            //CheckboxHideOverlay.IsChecked = _config.HideOverlay;
        }

        private void SaveConfigUpdateOverlay()
        {
            _xmlManagerConfig.Save("config.xml", _config);
            //_overlay.Dispatcher.BeginInvoke(new Action(_overlay.Update));
        }

        private void UseDeck(Deck selected)
        {
            if (selected == null)
                return;
            _hearthstone.SetPremadeDeck(selected.Cards);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _hearthstone.PlayerHandCount = 0;
                _hearthstone.EnemyCards.Clear();
                _hearthstone.EnemyHandCount = 0;
            }));
            _logReader.Reset();

            //_overlay.Dispatcher.BeginInvoke(new Action(_overlay.Update));
        }

        private void Update()
        {
            while (true)
            {
                _overlay.Dispatcher.BeginInvoke(new Action(() =>
                {
                    _overlay.UpdatePosition();
                }));
                Thread.Sleep(100);
            }
        }

        private void UpdateDbListView()
        {
            var selectedClass = ComboBoxSelectClass.SelectedValue.ToString();
            var selectedNeutral = ComboboxNeutral.SelectedValue.ToString();
            if (selectedClass == "Select a Class")
            {
                ListViewDB.Items.Clear();
            }
            else
            {
                ListViewDB.Items.Clear();

                foreach (var card in _hearthstone.GetActualCards())
                {
                    if (!card.Name.ToLower().Contains(TextBoxDBFilter.Text.ToLower()))
                        continue;
                    // mana filter
                    if (ComboBoxFilterMana.SelectedItem.ToString() == "All"
                        || ((ComboBoxFilterMana.SelectedItem.ToString() == "9+" && card.Cost >= 9)
                            || (ComboBoxFilterMana.SelectedItem.ToString() == card.Cost.ToString())))
                    {
                        switch (selectedNeutral)
                        {
                            case "Class + Neutral":
                                if (card.GetPlayerClass == selectedClass || card.GetPlayerClass == "Neutral")
                                    ListViewDB.Items.Add(card);
                                break;
                            case "Class Only":
                                if (card.GetPlayerClass == selectedClass)
                                {
                                    ListViewDB.Items.Add(card);
                                }
                                break;
                            case "Neutral Only":
                                if (card.GetPlayerClass == "Neutral")
                                {
                                    ListViewDB.Items.Add(card);
                                }
                                break;
                        }
                    }
                }

                var view1 = (CollectionView)CollectionViewSource.GetDefaultView(ListViewDB.Items);
                view1.SortDescriptions.Add(new SortDescription("Cost", ListSortDirection.Ascending));
                view1.SortDescriptions.Add(new SortDescription("Type", ListSortDirection.Descending));
                view1.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            }
        }

        private void UpdateDeckList(Deck selected)
        {
            if (selected == null) return;

            ListViewDeck.Items.Clear();
            foreach (var card in selected.Cards)
            {
                ListViewDeck.Items.Add(card);
            }
            var cardsInDeck = selected.Cards.Sum(c => c.Count);

            SortCardCollection(ListViewDeck.Items);
            _config.LastDeck = selected.Name;
            _xmlManagerConfig.Save("config.xml", _config);
        }

        #region GUI Handle
        private void TextBoxDBFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateDbListView();
        }

        private void TextBoxDBFilter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (ListViewDB.Items.Count == 1)
                {
                    var card = (Card)ListViewDB.Items[0];
                    AddCardToDeck(card);
                }
            }
        }

        private void ListViewDB_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var originalSource = (DependencyObject)e.OriginalSource;
            while ((originalSource != null) && !(originalSource is ListViewItem))
            {
                originalSource = VisualTreeHelper.GetParent(originalSource);
            }

            if (originalSource != null)
            {
                var card = (Card)ListViewDB.SelectedItem;
                AddCardToDeck(card);
                _newContainsDeck = true;
            }
        }

        private void ListViewDeck_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var originalSource = (DependencyObject)e.OriginalSource;
            while ((originalSource != null) && !(originalSource is ListViewItem))
            {
                originalSource = VisualTreeHelper.GetParent(originalSource);
            }

            if (originalSource != null)
            {
                var card = (Card)ListViewDeck.SelectedItem;
                RemoveCardFromDeck(card);
            }
        }

        private void BtnSaveDeck_Click(object sender, RoutedEventArgs e)
        {
            SaveDeck();
        }

        private void BtnDeleteDeck_Click(object sender, RoutedEventArgs e)
        {
            var deck = ListboxDecks.SelectedItem as Deck;
            if (deck != null)
            {
                if (
                    MessageBox.Show("Are you Sure?", "Delete " + deck.Name, MessageBoxButton.YesNo,
                                    MessageBoxImage.Asterisk) ==
                    MessageBoxResult.Yes)
                {
                    try
                    {
                        _decks.DecksList.Remove(deck);
                        _xmlManagerDecks.Save("PlayerDecks.xml", _decks);
                        ListboxDecks.SelectedIndex = -1;
                        ListViewDeck.Items.Clear();
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Error deleting deck");
                    }
                }
            }
        }

        private void ComboBoxFilterMana_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_initialized) return;
            UpdateDbListView();
        }

        private void ComboBoxFilterClass_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_initialized) return;
            UpdateDbListView();
        }

        private void ButtonNoDeck_Click(object sender, RoutedEventArgs e)
        {
            ListboxDecks.SelectedIndex = -1;
            UpdateDeckList(new Deck());
            UseDeck(new Deck());
            Hearthstone.IsUsingPremade = false;
        }

        private void ComboboxNeutral_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_initialized) return;
            UpdateDbListView();
        }

        private void ListboxDecks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_initialized) return;
            var deck = ListboxDecks.SelectedItem as Deck;
            if (deck != null)
            {
                Hearthstone.IsUsingPremade = true;
                UpdateDeckList(deck);
                UseDeck(deck);
            }
        }

        private void BtnEditDeck_Click(object sender, RoutedEventArgs e)
        {
            if (ListboxDecks.SelectedIndex == -1) return;
            var selectedDeck = ListboxDecks.SelectedItem as Deck;
            if (selectedDeck == null) return;
            //move to new deck section with stuff preloaded
            if (_newContainsDeck)
            {
                //still contains deck, discard?
                var result = MessageBox.Show("New Deck Section still contains an unfinished deck. Discard?",
                                             "Found unfinished deck.", MessageBoxButton.YesNo,
                                             MessageBoxImage.Exclamation);
                if (result == MessageBoxResult.No)
                {
                    TabControlTracker.SelectedIndex = 1;
                    return;
                }
            }

            ClearNewDeckSection();
            _editingDeck = true;
            _newContainsDeck = true;
            _newDeck = (Deck)selectedDeck.Clone();
            ListViewNewDeck.ItemsSource = _newDeck.Cards;

            if (ComboBoxSelectClass.Items.Contains(_newDeck.Class))
                ComboBoxSelectClass.SelectedValue = _newDeck.Class;

            TextBoxDeckName.Text = _newDeck.Name;

            UpdateDbListView();
            TabControlTracker.SelectedIndex = 1;
        }

        private void BtnClearNewDeck_Click(object sender, RoutedEventArgs e)
        {
            ClearNewDeckSection();
        }

        private void ListViewDB_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var card = (Card)ListViewDB.SelectedItem;
                if (string.IsNullOrEmpty(card.Name)) return;
                AddCardToDeck(card);
            }
        }

        private void ListViewNewDeck_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var originalSource = (DependencyObject)e.OriginalSource;
            while ((originalSource != null) && !(originalSource is ListViewItem))
            {
                originalSource = VisualTreeHelper.GetParent(originalSource);
            }

            if (originalSource != null)
            {
                var card = (Card)ListViewNewDeck.SelectedItem;
                RemoveCardFromDeck(card);
            }
        }

        private void Window_Closing_1(object sender, CancelEventArgs e)
        {
            try
            {
                _overlay.Close();
                _logReader.Stop();
                _updateThread.Abort();
                //_options.Shutdown();
                _playerWindow.Shutdown();
                _opponentWindow.Shutdown();
                _xmlManagerConfig.Save("config.xml", _config);
            }
            catch (Exception)
            {
                //doesnt matter
            }
        }
        #endregion

        #region OPTIONS

        private void CheckboxHighlightCardsInHand_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            _config.HighlightCardsInHand = true;
            Hearthstone.HighlightCardsInHand = true;
            SaveConfigUpdateOverlay();
        }

        private void CheckboxHighlightCardsInHand_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            _config.HighlightCardsInHand = false;
            Hearthstone.HighlightCardsInHand = false;
            SaveConfigUpdateOverlay();
        }

        private void CheckboxHideOverlay_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            _config.HideOverlay = true;
            SaveConfigUpdateOverlay();
        }

        private void CheckboxHideOverlay_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            _config.HideOverlay = false;
            SaveConfigUpdateOverlay();
        }

        private void CheckboxHideOverlayInMenu_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            _config.HideInMenu = true;
            SaveConfigUpdateOverlay();
        }

        private void CheckboxHideOverlayInMenu_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            _config.HideInMenu = false;
            SaveConfigUpdateOverlay();
        }

        private void CheckboxHideDrawChances_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            _config.HideDrawChances = true;
            SaveConfigUpdateOverlay();
        }

        private void CheckboxHideDrawChances_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            _config.HideDrawChances = false;
            SaveConfigUpdateOverlay();
        }

        private void CheckboxHidePlayerCardCounter_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            _config.HidePlayerCardCount = true;
            SaveConfigUpdateOverlay();
        }

        private void CheckboxHidePlayerCardCounter_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            _config.HidePlayerCardCount = false;
            SaveConfigUpdateOverlay();
        }

        private void CheckboxHideEnemyCardCounter_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            _config.HideEnemyCardCount = true;
            SaveConfigUpdateOverlay();
        }

        private void CheckboxHideEnemyCardCounter_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            _config.HideEnemyCardCount = false;
            SaveConfigUpdateOverlay();
        }

        private void CheckboxHideEnemyCards_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            _config.HideEnemyCards = true;
            SaveConfigUpdateOverlay();
        }

        private void CheckboxHideEnemyCards_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            _config.HideEnemyCards = false;
            SaveConfigUpdateOverlay();
        }


        private void CheckboxHideOverlayInBackground_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            _config.HideInBackground = true;
            SaveConfigUpdateOverlay();
        }

        private void CheckboxHideOverlayInBackground_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            _config.HideInBackground = false;
            SaveConfigUpdateOverlay();
        }
        private void CheckboxWindowsTopmost_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            _config.WindowsTopmost = true;
            _playerWindow.Topmost = true;
            _opponentWindow.Topmost = true;
            SaveConfigUpdateOverlay();
        }

        private void CheckboxWindowsTopmost_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            _config.WindowsTopmost = false;
            _playerWindow.Topmost = false;
            _opponentWindow.Topmost = false;
            SaveConfigUpdateOverlay();
        }

        private void BtnShowWindows_Click(object sender, RoutedEventArgs e)
        {
            //show playeroverlay and enemy overlay
            _playerWindow.Show();
            _playerWindow.Activate();
            _opponentWindow.Show();
            _opponentWindow.Activate();
        }
        #endregion

    }
}

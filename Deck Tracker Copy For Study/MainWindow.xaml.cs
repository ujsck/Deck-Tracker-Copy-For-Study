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
using Microsoft.Win32;

namespace Deck_Tracker_Copy_For_Study
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Config _config;
        private readonly XmlManager<Config> _xmlManagerConfig;
        private readonly Decks _decks;
        private readonly XmlManager<Decks> _xmlManagerDecks;
        private readonly Hearthstone _hearthstone;
        private readonly LogReader _logReader;
        private readonly bool _initialized;
        private readonly Thread _otherThread;
        private int _cardsInDeck;
        private readonly string _logConfigPath =
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Blizzard\Hearthstone\log.config";

        public MainWindow()
        {
            InitializeComponent();
            //Helper.CheckForUpdates();
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
            foreach (var deck in _decks.DecksList)
            {
                ComboBoxDecks.Items.Add(deck.Name);
            }
            ComboBoxDecks.SelectedItem = _config.LastDeck;

            //hearthstone, loads db etc
            _hearthstone = new Hearthstone();

            //create overlay
            //_overlay = new OverlayWindow(_config, _hearthstone) { Topmost = true };
            //_overlay.Show();

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

            _otherThread = new Thread(Update);
            _otherThread.Start();

            _initialized = true;

            UpdateDeckList();
            UseSelectedDeck();

            _initialized = true;
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

        private void AddCardToDeck(Card card)
        {
            if (ListViewDeck.Items.Contains(card))
            {
                var cardInDeck = (Card)ListViewDeck.Items.GetItemAt(ListViewDeck.Items.IndexOf(card));
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
                ListViewDeck.Items.Remove(cardInDeck);
                cardInDeck.Count++;
                ListViewDeck.Items.Add(cardInDeck);
            }
            else
            {
                ListViewDeck.Items.Add(card);
            }

            ButtonUseDeck.Content = "Use Deck (" + (++_cardsInDeck) + ")";
            var view1 = (CollectionView)CollectionViewSource.GetDefaultView(ListViewDeck.Items);
            view1.SortDescriptions.Add(new SortDescription("Cost", ListSortDirection.Ascending));
            view1.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            BtnSaveDeck.Content = "Save*";

        }

        private void RemoveCardFromDeck(Card card)
        {
            if (card.Count > 1)
            {
                ListViewDeck.Items.Remove(card);
                card.Count--;
                ListViewDeck.Items.Add(card);
            }
            else
                ListViewDeck.Items.Remove(card);

            ButtonUseDeck.Content = "Use Deck (" + (--_cardsInDeck) + ")";
            var view1 = (CollectionView)CollectionViewSource.GetDefaultView(ListViewDeck.Items);
            view1.SortDescriptions.Add(new SortDescription("Cost", ListSortDirection.Ascending));
            view1.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            BtnSaveDeck.Content = "Save*";
        }

        private void SaveDeck()
        {
            var deck = new Deck();
            deck.Cards = new List<Card>();
            if (ComboBoxDecks.SelectedItem.ToString() == "Create New")
            {
                deck.Name = TextBoxDeckName.Text;
                ComboBoxDecks.Items.Add(deck.Name);
            }
            else
            {
                deck.Name = ComboBoxDecks.SelectedItem.ToString();
                _decks.DecksList.Remove(
                    _decks.DecksList.First(x => ComboBoxDecks.SelectedItem.ToString().Equals(x.Name)));
            }
            foreach (var card in ListViewDeck.Items)
            {
                deck.Cards.Add((Card)card);
            }
            _decks.DecksList.Add(deck);

            ComboBoxDecks.SelectedItem = deck.Name;
            _xmlManagerDecks.Save("PlayerDecks.xml", _decks);
            BtnSaveDeck.Content = "Save";
        }

        private void DeleteDeck()
        {
            var deck = new Deck();
            deck.Cards = new List<Card>();
            if (ComboBoxDecks.SelectedItem.ToString() == "Create New")
            {
                deck.Name = TextBoxDeckName.Text;
                ComboBoxDecks.Items.Add(deck.Name);
                _cardsInDeck = 0;
                ButtonUseDeck.Content = "Use Deck (" + (_cardsInDeck) + ")";
            }
            else
            {
                deck.Name = ComboBoxDecks.SelectedItem.ToString();
            }


            if (
                MessageBox.Show("Are you Sure?", "Delete " + deck.Name, MessageBoxButton.YesNo, MessageBoxImage.Asterisk) ==
                MessageBoxResult.Yes)
            {
                try
                {

                    _decks.DecksList.Remove(
                        _decks.DecksList.First(x => ComboBoxDecks.SelectedItem.ToString().Equals(x.Name)));
                    _xmlManagerDecks.Save("PlayerDecks.xml", _decks);
                    ComboBoxDecks.SelectedIndex = 0;
                    ComboBoxDecks.Items.Remove(deck.Name);
                    _cardsInDeck = 0;
                    ButtonUseDeck.Content = "Use Deck (" + (_cardsInDeck) + ")";
                }
                catch (Exception)
                {
                    Console.WriteLine("Error deleting deck");
                }
            }
        }

        private void LoadConfig()
        {
            if (ComboBoxDecks.Items.Contains(_config.LastDeck))
                ComboBoxDecks.SelectedItem = _config.LastDeck;

            CheckboxHideOverlayInBackground.IsChecked = _config.HideInBackground;
            CheckboxHideDrawChances.IsChecked = _config.HideDrawChances;
            CheckboxHideEnemyCards.IsChecked = _config.HideEnemyCards;
            CheckboxHideEnemyCardCounter.IsChecked = _config.HideEnemyCardCount;
            CheckboxHidePlayerCardCounter.IsChecked = _config.HidePlayerCardCount;
            CheckboxHideOverlayInMenu.IsChecked = _config.HideInMenu;
            CheckboxHighlightCardsInHand.IsChecked = _config.HighlightCardsInHand;
            this.Height = _config.WindowHeight;
            //_overlay.Dispatcher.BeginInvoke(new Action(() => _overlay.ShowInTaskbar = _config.ShowInTaskbar));
            Hearthstone.HighlightCardsInHand = _config.HighlightCardsInHand;
        }

        private void SaveConfigUpdateOverlay()
        {
            _xmlManagerConfig.Save("config.xml", _config);
            //_overlay.Dispatcher.BeginInvoke(new Action(_overlay.Update));
        }

        private void UseSelectedDeck()
        {
            if (ComboBoxDecks.SelectedItem.ToString() == "Create New")
                return;
            var firstOrDefault = _decks.DecksList.FirstOrDefault(x => x.Name != null && x.Name.Equals(ComboBoxDecks.SelectedItem.ToString()));
            if (firstOrDefault != null)
            {
                var deck =
                    firstOrDefault.Cards;
                if (deck != null)
                {
                    _hearthstone.SetPremadeDeck(deck);
                    //_overlay.Dispatcher.BeginInvoke(new Action(_overlay.Update));
                }
            }
        }

        private void Update()
        {
            while (true)
            {
                //_overlay.Dispatcher.BeginInvoke(new Action(() =>
                //{
                //    bool hide1 = false;
                //    bool hide2 = false;
                //    //hide in background depenting on option

                //    if (CheckboxHideOverlayInBackground.IsChecked.Value)
                //    {
                //        hide1 = !User32.IsForegroundWindow("炉石传说");
                //    }

                //    if (CheckboxHideOverlayInMenu.IsChecked.Value)
                //    {
                //        hide2 = _hearthstone.IsInMenu;
                //    }

                //    _overlay.EnableCanvas(!(hide1 || hide2));
                //    _overlay.UpdatePosition();
                //}));
                Thread.Sleep(100);
            }
        }

        private void UpdateDbListView()
        {
            ListViewDB.Items.Clear();

            // filter
            foreach (var card in _hearthstone.GetActualCards())
            {
                if (!card.Name.ToLower().Contains(TextBoxDBFilter.Text.ToLower())) continue;
                if (ComboBoxFilterClass.SelectedItem.ToString() == "All" ||
                    ComboBoxFilterClass.SelectedItem.ToString() == card.GetPlayerClass ||
                    (CheckboxIncludeNeutral.IsChecked.Value && card.GetPlayerClass == "Neutral"))
                {
                    if (ComboBoxFilterMana.SelectedItem.ToString() == "All")
                        ListViewDB.Items.Add(card);
                    else if (ComboBoxFilterMana.SelectedItem.ToString() == "9+" && card.Cost >= 9)
                        ListViewDB.Items.Add(card);
                    else if (ComboBoxFilterMana.SelectedItem.ToString() == card.Cost.ToString())
                        ListViewDB.Items.Add(card);
                }
            }

            // sort
            var view1 = (CollectionView)CollectionViewSource.GetDefaultView(ListViewDB.Items);
            view1.SortDescriptions.Add(new SortDescription("Cost", ListSortDirection.Ascending));
            view1.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }

        private void UpdateDeckList()
        {
            if (ComboBoxDecks.SelectedItem.ToString() == "Create New")
            {
                ListViewDeck.Items.Clear();
                _cardsInDeck = 0;
                ButtonUseDeck.Content = "Use Deck (" + (_cardsInDeck) + ")";
                TextBoxDeckName.IsEnabled = true;
            }
            else
            {
                TextBoxDeckName.IsEnabled = false;
                ListViewDeck.Items.Clear();
                var deck = _decks.DecksList.FirstOrDefault(x => x.Name != null && x.Name.Equals(ComboBoxDecks.SelectedItem));
                if (deck == null) return;

                foreach (var card in deck.Cards)
                {
                    ListViewDeck.Items.Add(card);
                }
                _cardsInDeck = deck.Cards.Sum(c => c.Count);
                ButtonUseDeck.Content = "Use Deck (" + (_cardsInDeck) + ")";
                var view1 = (CollectionView)CollectionViewSource.GetDefaultView(ListViewDeck.Items);
                view1.SortDescriptions.Add(new SortDescription("Cost", ListSortDirection.Ascending));
                view1.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

                _config.LastDeck = deck.Name;
                _xmlManagerConfig.Save("config.xml", _config);
            }
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
            DeleteDeck();
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

        private void ButtonUseDeck_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBoxDecks.SelectedItem.ToString() == "Create New")
            {
                MessageBox.Show("Save and/or select a deck first");
                return;
            }
            if (BtnSaveDeck.Content.ToString().Contains("*"))
            {
                if (
                    MessageBox.Show("Save deck first?", "Deck not saved", MessageBoxButton.YesNo,
                                    MessageBoxImage.Asterisk) == MessageBoxResult.Yes)
                {
                    SaveDeck();
                }
            }
            var firstOrDefault =
                _decks.DecksList.FirstOrDefault(x => x.Name != null && x.Name.Equals(ComboBoxDecks.SelectedItem.ToString()));
            if (firstOrDefault != null)
            {
                var deck =
                    firstOrDefault.Cards;
                if (deck != null)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _hearthstone.PlayerHandCount = 0;
                        _hearthstone.EnemyCards.Clear();
                        _hearthstone.EnemyHandCount = 0;
                    }));
                    _hearthstone.SetPremadeDeck(deck);
                    _logReader.Reset(); // _previousSize = 0;
                }
            }
        }

        private void ButtonNoDeck_Click(object sender, RoutedEventArgs e)
        {
            Hearthstone.IsUsingPremade = false;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _hearthstone.PlayerDeck.Clear();
                _hearthstone.PlayerHandCount = 0;
                _hearthstone.EnemyCards.Clear();
                _hearthstone.EnemyHandCount = 0;
            }));
            _config.LastDeck = "";
            _logReader.Reset();
        }

        private void ComboBoxDecks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_initialized) return;
            BtnSaveDeck.Content = "Save";
            UpdateDeckList();
        }

        private void Window_Closing_1(object sender, CancelEventArgs e)
        {
            try
            {
                //save config
                _config.HideInBackground = CheckboxHideOverlayInBackground.IsChecked.Value;
                _xmlManagerConfig.Save("config.xml", _config);

                //_overlay.Close();
                _logReader.Stop();
                _otherThread.Abort();
            }
            catch (Exception)
            {
                //doesnt matter
            }
        }
        #endregion

        #region CheckBox
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

        private void CheckboxHideOverlayInMenu_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            _config.HideInMenu = true;
            SaveConfigUpdateOverlay();
        }

        private void CheckboxHideOverlayInMenu_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            _config.HideInMenu = true;
            SaveConfigUpdateOverlay();
        }

        private void CheckboxIncludeNeutral_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            UpdateDbListView();
        }

        private void CheckboxIncludeNeutral_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            UpdateDbListView();
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
        #endregion

    }
}

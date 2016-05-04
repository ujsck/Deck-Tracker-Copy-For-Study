﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Deck_Tracker_Copy_For_Study
{
    /// <summary>
    /// DeckSelectionDialog.xaml 的交互逻辑
    /// </summary>
    public partial class DeckSelectionDialog
    {
        public DeckSelectionDialog(IEnumerable<Deck> decks)
        {
            InitializeComponent();

            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            
            DeckPickerList.Items.Clear();
            foreach (var deck in decks)
            {
                DeckPickerList.Items.Add(deck);
            }
            
        }

        public Deck SelectedDeck;
        private void DeckPickerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedDeck = DeckPickerList.SelectedItem as Deck;
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(SelectedDeck == null)
                MessageBox.Show("Deck detection disabled for now. You can reenable it in the options.");
        }
    }
}

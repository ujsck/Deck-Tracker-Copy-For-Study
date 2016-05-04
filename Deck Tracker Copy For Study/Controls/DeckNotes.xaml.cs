using System.Windows.Controls;

namespace Deck_Tracker_Copy_For_Study
{
    /// <summary>
    /// DeckNotes.xaml 的交互逻辑
    /// </summary>
    public partial class DeckNotes : UserControl
    {
        private Deck currentDeck;
        public DeckNotes()
        {
            InitializeComponent();
        }

        public void SetDeck(Deck deck)
        {
            currentDeck = deck;
            Textbox.Text = deck.Note;
        }

        private void Textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            currentDeck.Note = Textbox.Text;
        }
    }
}

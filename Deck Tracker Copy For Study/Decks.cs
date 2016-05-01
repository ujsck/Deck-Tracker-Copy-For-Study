using System.Collections.Generic;
using System.Xml.Serialization;

namespace Deck_Tracker_Copy_For_Study
{
    public class Decks
    {
        [XmlElement(ElementName = "Deck")]
        public List<Deck> DecksList;
    }

    public class Deck
    {
        public string Name;

        [XmlArray(ElementName = "Cards")]
        [XmlArrayItem(ElementName = "Card")]
        public List<Card> Cards;
    }
}

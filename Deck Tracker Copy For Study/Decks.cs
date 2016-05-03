using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace Deck_Tracker_Copy_For_Study
{
    public class Decks
    {
        [XmlElement(ElementName = "Deck")]
        public ObservableCollection<Deck> DecksList;

        [XmlArray(ElementName = "Tags")]
        [XmlArrayItem(ElementName = "Tags")]
        public List<string> AllTags;

        public List<DeckInfo> LastDeckClass;
    }

    public class DeckInfo
    {
        public string Name;
        public string Class;
    }
}

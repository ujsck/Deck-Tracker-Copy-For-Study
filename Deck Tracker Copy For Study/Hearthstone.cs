﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Deck_Tracker_Copy_For_Study
{
    public class Hearthstone
    {
        public static bool IsUsingPremade;

        //dont like this solution, cant think of better atm
        public static bool HighlightCardsInHand;


        private readonly Dictionary<string, Card> _cardDb;
        //https://msdn.microsoft.com/zh-cn/library/ms668604.aspx
        public ObservableCollection<Card> EnemyCards;
        public ObservableCollection<Card> PlayerDeck;
        public int EnemyHandCount;
        public bool IsInMenu;
        public int PlayerHandCount;
        public string PlayingAgainst;
        public string PlayingAs;

        private readonly List<string> _invalidCardIds = new List<string>
        {
                "EX1_tk34",
                "EX1_tk29",
                "EX1_tk28",
                "EX1_tk11",
                "EX1_598",
                "NEW1_032",
                "NEW1_033",
                "NEW1_034",
                "NEW1_009",
                "CS2_052",
                "CS2_082",
                "CS2_051",
                "CS2_050",
                "CS2_152",
                "skele11",
                "skele21",
                "GAME",
                "DREAM",
                "NEW1_006",
        };

        public Hearthstone()
        {
            IsInMenu = true;
            PlayerDeck = new ObservableCollection<Card>();
            EnemyCards = new ObservableCollection<Card>();
            _cardDb = new Dictionary<string, Card>();
            LoadCardDb();
        }

        private void LoadCardDb()
        {
            var obj = JObject.Parse(File.ReadAllText("cardsDB.json"));
            foreach (var cardType in obj)
            {
                if (cardType.Key != "Basic" && cardType.Key != "Expert" && cardType.Key != "Promotion" &&
                    cardType.Key != "Reward") continue;
                foreach (var card in cardType.Value)
                {
                    var tmp = JsonConvert.DeserializeObject<Card>(card.ToString());
                    _cardDb.Add(tmp.Id, tmp);
                }
            }
        }

        public Card GetCardFromDb(string cardId)
        {
            if (cardId == "") return new Card();
            return _cardDb[cardId];
        }


        public List<Card> GetActualCards()
        {
            return (from card in _cardDb.Values
                    where card.Type == "Minion" || card.Type == "Spell" || card.Type == "Weapon"
                    where Helper.IsNumeric(card.Id.ElementAt(card.Id.Length - 1))
                    where Helper.IsNumeric(card.Id.ElementAt(card.Id.Length - 2))
                    where !_invalidCardIds.Any(id => card.Id.Contains(id))
                    select card).ToList();
        }

        public void SetPremadeDeck(List<Card> cards)
        {
            PlayerDeck.Clear();
            foreach (var card in cards)
            {
                PlayerDeck.Add(card);
            }
            IsUsingPremade = true;
        }

        public void PlayerDraw(string cardId)
        {
            if (cardId == "GAME_005")
            {
                PlayerHandCount++;
                return;
            }
            var card = GetCardFromDb(cardId);

            if (!IsUsingPremade)
            {
                if (PlayerDeck.Contains(card))
                {
                    PlayerDeck.Remove(card);
                    card.Count++;
                }
                PlayerDeck.Add(card);
            }
            else
            {
                var deckCard = PlayerDeck.FirstOrDefault(x => x.Name != null && x.Name.Equals(card.Name));

                PlayerDeck.Remove(deckCard);
                deckCard.Count--;
                deckCard.InHandCount++;
                PlayerDeck.Add(deckCard);
            }
            PlayerHandCount++;
        }

        //cards from board(?), thoughtsteal etc
        public void PlayerGet(string cardId)
        {
            PlayerHandCount++;
            if (IsUsingPremade)
            {
                if (PlayerDeck.Any(c => c.Id == cardId))
                {
                    var card = PlayerDeck.First(c => c.Id == cardId);
                    PlayerDeck.Remove(card);
                    card.InHandCount++;
                    PlayerDeck.Add(card);
                }
            }
        }

        public void PlayerPlayed(string cardId)
        {
            PlayerHandCount--;
            if (IsUsingPremade)
            {
                if (PlayerDeck.Any(c => c.Id == cardId))
                {
                    var card = PlayerDeck.First(c => c.Id == cardId);
                    PlayerDeck.Remove(card);
                    card.InHandCount--;
                    PlayerDeck.Add(card);
                }
            }
        }

        public void EnemyDraw()
        {
            EnemyHandCount++;
        }

        public void EnemyPlayed(string cardId)
        {
            if (cardId == "")
            {
                EnemyHandCount--;
                return;
            }
            Card card = GetCardFromDb(cardId);
            if (EnemyCards.Any(x => x.Equals(card)))
            {
                EnemyCards.Remove(card);
                card.Count++;
            }
            EnemyCards.Add(card);
            EnemyHandCount--;
        }

        public void Mulligan(string cardId)
        {
            Card card = GetCardFromDb(cardId);
            if (!IsUsingPremade)
            {
                Card deckCard = PlayerDeck.FirstOrDefault(c => c.Equals(card));
                if (deckCard.Count > 1)
                    deckCard.Count--;
                else
                {
                    PlayerDeck.Remove(deckCard);
                }
            }
            else //PREMADE
            {
                Card deckCard = PlayerDeck.FirstOrDefault(c => c.Name != null && c.Name == card.Name);
                PlayerDeck.Remove(deckCard);
                deckCard.Count++;
                deckCard.InHandCount--;
                PlayerDeck.Add(deckCard);
            }

            PlayerHandCount--;
        }

        public void EnemyMulligan()
        {
            EnemyHandCount--;
        }

        internal void PlayerHandDiscard(string cardId)
        {
            PlayerHandCount--;
            if (IsUsingPremade)
            {
                if (PlayerDeck.Any(c => c.Id == cardId))
                {
                    var card = PlayerDeck.First(c => c.Id == cardId);
                    PlayerDeck.Remove(card);
                    card.InHandCount--;
                    PlayerDeck.Add(card);
                }
            }
        }

        internal void PlayerDeckDiscard(string cardId)
        {
            Card card = GetCardFromDb(cardId);

            if (!IsUsingPremade)
            {
                if (PlayerDeck.Contains(card))
                {
                    PlayerDeck.Remove(card);
                    card.Count++;
                }
                PlayerDeck.Add(card);
            }
            else
            {
                Card deckCard = PlayerDeck.FirstOrDefault(x => x.Name != null && x.Name.Equals(card.Name));
                PlayerDeck.Remove(deckCard);
                deckCard.Count--;
                PlayerDeck.Add(deckCard);
            }
        }

        internal void EnemyHandDiscard()
        {
            EnemyHandCount--;
        }

        internal void EnemyDeckDiscard()
        {
            //nothing for now
        }

        internal void EnemySecretTriggered(string cardId)
        {
            if (cardId == "")
            {
                return;
            }
            Card card = GetCardFromDb(cardId);
            if (EnemyCards.Any(x => x.Equals(card)))
            {
                EnemyCards.Remove(card);
                card.Count++;
            }
            EnemyCards.Add(card);
        }
    }
}

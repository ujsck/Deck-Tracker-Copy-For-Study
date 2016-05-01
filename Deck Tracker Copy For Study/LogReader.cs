using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Deck_Tracker_Copy_For_Study
{
    public enum GameState
    {
        GameBegin,
        GameEnd,
    }

    public enum CardMovementType
    {
        PlayerDraw,
        PlayerMulligan,
        PlayerPlay,
        PlayerDeckDiscard,
        PlayerHandDiscard,
        OpponentDraw,
        OpponentMulligan,
        OpponentPlay,
        OpponentSecretTrigger,
        OpponentDeckDiscard,
        OpponentHandDiscard,
        PlayerGet,
    }

    public class GameStateArgs : EventArgs
    {
        public GameStateArgs(GameState state)
        {
            State = state;
        }
        public GameState State { get; private set; }
    }

    public class CardMovementArgs : EventArgs
    {
        public CardMovementArgs(CardMovementType movement, string cardId)
        {
            MovementType = movement;
            CardId = cardId;
        }
        public CardMovementType MovementType { get; private set; }
        public string CardId { get; private set; }
    }

    public class LogReader
    {
        public delegate void CardMovementHandler(LogReader sender, CardMovementArgs args);

        public delegate void GameStateHandler(LogReader sender, GameStateArgs args);

        private readonly string _fullOutputPath;
        private Thread _analyzerThread;

        private readonly Regex _cardMovementRegex = new Regex(@"");
        private long _previousSize;

        public LogReader(string hsDirPath)
        {
            while (hsDirPath.EndsWith("\\") || hsDirPath.EndsWith("/"))
            {
                hsDirPath = hsDirPath.Remove(hsDirPath.Length - 1);
            }
            _fullOutputPath = @hsDirPath + @"\Logs\Power.log";
        }

        public event CardMovementHandler CardMovement;
        public event GameStateHandler GameStateChange;

        public void Start()
        {
            if (_analyzerThread != null) return;
            _analyzerThread = new Thread(ReadFile);
            _analyzerThread.Start();
        }

        public void Stop()
        {
            _analyzerThread.Abort();
            _analyzerThread = null;
        }

        private void ReadFile()
        {
            while (true)
            {
                if (File.Exists(_fullOutputPath))
                {
                    using (var fs = new FileStream(_fullOutputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                        )
                    {
                        fs.Seek(_previousSize, SeekOrigin.Begin);
                        _previousSize = fs.Length;
                        using (var sr = new StreamReader(fs))
                        {
                            Analyze(sr.ReadToEnd());
                        }
                    }
                }
                Thread.Sleep(100);
            }
        }

        private void Analyze(string log)
        {

        }

        internal void Reset()
        {
            _previousSize = 0;
        }
    }
}

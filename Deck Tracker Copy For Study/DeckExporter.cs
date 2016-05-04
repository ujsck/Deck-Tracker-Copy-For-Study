﻿using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Deck_Tracker_Copy_For_Study
{
    internal class DeckExporter
    {
        private const int SwRestore = 9;

        private readonly Config _config;

        public DeckExporter(Config config)
        {
            _config = config;
        }

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        public async Task Export(Deck deck)
        {
            if (deck == null) return;

            var hsHandle = User32.FindWindow(null, "Hearthstone");

            if (!User32.IsForegroundWindow("Hearthstone"))
            {
                //restore window and bring to foreground
                User32.ShowWindow(hsHandle, SwRestore);
                User32.SetForegroundWindow(hsHandle);
                //wait it to actually be in foreground, else the rect might be wrong
                await Task.Delay(500);
            }
            if (!User32.IsForegroundWindow("Hearthstone"))
            {
                MessageBox.Show("Can't find Heartstone window.");
                return;
            }

            User32.Rect hsWindowRect = new User32.Rect();
            User32.GetWindowRect(hsHandle, ref hsWindowRect);

            var height = (hsWindowRect.bottom - hsWindowRect.top);
            var width = (hsWindowRect.right - hsWindowRect.left);

            var bounds = Screen.FromHandle(hsHandle).Bounds;
            bool isFullscreen = bounds.Width == width && bounds.Height == height;

            foreach (var card in deck.Cards)
            {
                await AddCardToDeck(card, width, height, hsHandle, isFullscreen);
            }
        }

        private async Task AddCardToDeck(Card card, int width, int height, IntPtr hsHandle, bool isFullscreen)
        {
            var ratio = (double)width / height;

            var searchBoxY = (isFullscreen ? _config.SearchBoxYFullscreen : _config.SearchBoxY);
            var cardPosX = ratio < 1.5 ? width * _config.CardPosX : width * _config.CardPosX * (ratio / 1.33);

            var searchBoxPos = new Point((int)(_config.SearchBoxX * width), (int)(searchBoxY * height));
            var cardPos = new Point((int)cardPosX, (int)(_config.CardPosY * height));

            await ClickOnPoint(hsHandle, searchBoxPos);
            SendKeys.SendWait("^(a)");
            SendKeys.SendWait(FixCardName(card.Name));
            SendKeys.SendWait("{ENTER}");

            await Task.Delay(_config.SearchDelay);

            for (int i = 0; i < card.Count; i++)
                await ClickOnPoint(hsHandle, cardPos);

            if (card.Count == 2)
            {
                var card2PosX = ratio < 1.5 ? width * _config.Card2PosX : width * _config.Card2PosX * (ratio / 1.33);
                var card2Pos = new Point((int)card2PosX, (int)(_config.CardPosY * height));
                await ClickOnPoint(hsHandle, card2Pos);

            }
        }

        private async Task ClickOnPoint(IntPtr wndHandle, Point clientPoint)
        {
            ClientToScreen(wndHandle, ref clientPoint);

            Cursor.Position = new Point(clientPoint.X, clientPoint.Y);

            //lmb down
            mouse_event(0x00000002, 0, 0, 0, UIntPtr.Zero);
            await Task.Delay(_config.ClickDelay);

            //lmb up
            mouse_event(0x00000004, 0, 0, 0, UIntPtr.Zero);
            await Task.Delay(_config.ClickDelay);
        }

        public string FixCardName(string cardName)
        {
            if (cardName == "Windfury") return "Windfury spell";
            if (cardName == "Fireball") return "Fireball spell";
            return cardName;
        }
    }
}

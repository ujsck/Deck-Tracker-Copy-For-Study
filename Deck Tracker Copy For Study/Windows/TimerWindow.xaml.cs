using System;
using System.Windows;

namespace Deck_Tracker_Copy_For_Study
{
    /// <summary>
    /// TimerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TimerWindow
    {
        private bool _appIsClosing;
        private readonly Config _config;
        public TimerWindow(Config config)
        {
            InitializeComponent();
            _config = config;

            if (config.TimerWindowLeft != 0 && _config.TimerWindowLeft != -32000)
            {
                Left = config.TimerWindowLeft;
            }
            if (config.TimerWindowTop != 0 && _config.TimerWindowTop != -32000)
            {
                Top = config.TimerWindowTop;
            }
            Topmost = _config.TimerWindowTopmost;
        }

        public void Update(TimerEventArgs timerEventArgs)
        {
            LblTurnTime.Text = string.Format("{0:00}:{1:00}", (timerEventArgs.Seconds / 60) % 60, timerEventArgs.Seconds % 60);
            LblPlayerTurnTime.Text = string.Format("{0:00}:{1:00}", (timerEventArgs.PlayerSeconds / 60) % 60, timerEventArgs.PlayerSeconds % 60);
            LblOpponentTurnTime.Text = string.Format("{0:00}:{1:00}", (timerEventArgs.OpponentSeconds / 60) % 60, timerEventArgs.OpponentSeconds % 60);

        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_appIsClosing) return;
            e.Cancel = true;
            Hide();
        }
        

        internal void Shutdown()
        {
            _appIsClosing = true;
            Close();
        }
        
        private void MetroWindow_LocationChanged(object sender, EventArgs e)
        {
            _config.TimerWindowLeft = Left;
            _config.TimerWindowTop = Top;
        }

        private void MetroWindow_Activated(object sender, EventArgs e)
        {
            Topmost = true;
        }

        private void MetroWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (WindowState == WindowState.Minimized) return;
            _config.TimerWindowHeight = (int)Height;
            _config.TimerWindowWidth = (int)Height;
        }

        private void MetroWindow_Deactivated(object sender, EventArgs e)
        {
            if (!_config.TimerWindowTopmost)
                Topmost = false;
        }
    }
}

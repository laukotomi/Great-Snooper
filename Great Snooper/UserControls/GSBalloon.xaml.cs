namespace GreatSnooper.UserControls
{
    using System;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Threading;

    using GreatSnooper.ViewModel;

    using Hardcodet.Wpf.TaskbarNotification;

    /// <summary>
    /// Interaction logic for GSBalloon.xaml
    /// </summary>
    public partial class GSBalloon : UserControl, IDisposable
    {
        private AbstractChannelViewModel chvm;
        private Dispatcher dispatcher;
        bool disposed = false;
        private bool isClosing = false;
        private Timer timer;

        // Not used!
        public GSBalloon()
        {
            InitializeComponent();
        }

        public GSBalloon(AbstractChannelViewModel chvm = null)
        {
            InitializeComponent();
            this.DataContext = this;
            this.chvm = chvm;
            dispatcher = Dispatcher.CurrentDispatcher;
            TaskbarIcon.AddBalloonClosingHandler(this, OnBalloonClosing);
        }

        ~GSBalloon()
        {
            Dispose(false);
        }

        public string BalloonText
        {
            get;
            set;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            if (disposing)
            {
                if (timer != null)
                {
                    timer.Dispose();
                    timer = null;
                }
            }
        }

        private void grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.chvm != null && e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {
                chvm.MainViewModel.SelectChannel(chvm);
                chvm.MainViewModel.ActivationCommand.Execute(null);
            }
        }

        /// <summary>
        /// If the users hovers over the balloon, we don't close it.
        /// </summary>
        private void grid_MouseEnter(object sender, MouseEventArgs e)
        {
            //if we're already running the fade-out animation, do not interrupt anymore
            //(makes things too complicated for the sample)
            if (isClosing)
            {
                return;
            }

            //the tray icon assigned this attached property to simplify access
            TaskbarIcon taskbarIcon = TaskbarIcon.GetParentTaskbarIcon(this);
            taskbarIcon.ResetBalloonCloseTimer();

            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
        }

        private void grid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (isClosing)
            {
                return;
            }

            this.timer = new Timer((t) =>
            {
                if (isClosing) return;

                this.dispatcher.BeginInvoke(new Action(() =>
                {
                    if (isClosing) return;

                    TaskbarIcon taskbarIcon = TaskbarIcon.GetParentTaskbarIcon(this);
                    taskbarIcon.CloseBalloon();
                }), null);
            }, null, 2000, Timeout.Infinite);
        }

        /// <summary>
        /// Resolves the <see cref="TaskbarIcon"/> that displayed
        /// the balloon and requests a close action.
        /// </summary>
        private void imgClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //the tray icon assigned this attached property to simplify access
            TaskbarIcon taskbarIcon = TaskbarIcon.GetParentTaskbarIcon(this);
            taskbarIcon.CloseBalloon();
        }

        /// <summary>
        /// By subscribing to the <see cref="TaskbarIcon.BalloonClosingEvent"/>
        /// and setting the "Handled" property to true, we suppress the popup
        /// from being closed in order to display the custom fade-out animation.
        /// </summary>
        private void OnBalloonClosing(object sender, RoutedEventArgs e)
        {
            e.Handled = true; //suppresses the popup from being closed immediately
            isClosing = true;
            this.Dispose();
        }

        /// <summary>
        /// Closes the popup once the fade-out animation completed.
        /// The animation was triggered in XAML through the attached
        /// BalloonClosing event.
        /// </summary>
        private void OnFadeOutCompleted(object sender, EventArgs e)
        {
            Popup pp = (Popup)Parent;
            pp.IsOpen = false;
        }
    }
}
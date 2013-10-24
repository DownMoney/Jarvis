using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.IO;

namespace Jarvis.Utilities
{
    public delegate void Scrub(int position, int duration);
    public class Player
    {
        private MediaElement player;
        private Window window;
        private bool playing = false;
        private Image play;
        private Border round;
        private Image mute;
        private Image full;
        private System.Windows.Threading.DispatcherTimer timer;
        private bool seeking = false;
        public event Scrub OnScrub;
        private Grid mainGrid;
        private Image img;

        public Player()
        {
            window = new Window();
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            player = new MediaElement();
            window.Background = Brushes.Transparent;
            //w.ResizeMode = ResizeMode.NoResize;
            window.WindowStyle = WindowStyle.None;
            mainGrid = new Grid();
            mainGrid.Background = Brushes.Transparent;
            player.ScrubbingEnabled = true;
            player.MouseLeftButtonDown += (a, b) => { window.DragMove(); };
            
            mainGrid.Children.Add(player);
            player.LoadedBehavior = MediaState.Manual;
            player.UnloadedBehavior = MediaState.Manual;
            player.MediaOpened += player_MediaOpened;
            window.Content = mainGrid;

            window.Closed += (p, h) =>
            {
                player.Stop();
                Dispatcher.CurrentDispatcher.InvokeShutdown();

            };

            round = new Border();
            round.BorderThickness = new Thickness(1);
            round.CornerRadius = new CornerRadius(30);
            round.BorderBrush = Brushes.White;
            round.Width = 400;
            round.Height = 50;
            round.HorizontalAlignment = HorizontalAlignment.Center;
            round.VerticalAlignment = VerticalAlignment.Bottom;
            round.Margin = new Thickness(0, 0, 0, 50);

            

            round.Background = Brushes.Black;
            round.Opacity = 0;

            

            Grid tools = new Grid();
            
            tools.Width = 400;
            tools.Height = 50;
            tools.Background = Brushes.Transparent;

            tools.MouseMove += round_MouseMove;
            tools.MouseLeftButtonUp += round_MouseLeftButtonUp;

            round.Child = tools;
            mainGrid.Children.Add(round);

            window.MouseLeave += (a, v) =>
            {
                Animate(0.5, 0.0);               
            };

            window.MouseEnter += (a, v) =>
            {
                Animate(0, 0.5);
            };

            play = new Image();
            play.Width = 30;
            play.Height = 30;
            play.VerticalAlignment = VerticalAlignment.Center;
            play.Source = new BitmapImage(new Uri(@"Media\pause.png", UriKind.Relative));
                      


            play.MouseLeftButtonDown += (a, v) =>
            {               
                PlayPause();
                v.Handled = true;
            };
            tools.Children.Add(play);

            full = new Image();

            full = new Image();
            full.Width = 30;
            full.Height = 30;
            full.VerticalAlignment = VerticalAlignment.Center;
            full.Margin = new Thickness(90, 0, 0, 0);
            full.Source = new BitmapImage(new Uri(@"Media\fullscr.png", UriKind.Relative));


           
            full.MouseLeftButtonDown += (a, v) =>
            {
                MaxMin();
                v.Handled = true;
            };
            tools.Children.Add(full);

           mute = new Image();

            mute = new Image();
            mute.Width = 30;
            mute.Height = 30;
            mute.VerticalAlignment = VerticalAlignment.Center;
            mute.Margin = new Thickness(-90, 0, 0, 0);
            mute.Source = new BitmapImage(new Uri(@"Media\muted.png", UriKind.Relative));

            mute.MouseLeftButtonDown += (a, v) =>
            {
                MuteUnMute();
                v.Handled = true;
            };

            tools.Children.Add(mute);

            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0,0,500);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        void player_MediaOpened(object sender, RoutedEventArgs e)
        {
           
        }

        public int getPosition()
        {           
            int i = 0;
            player.Dispatcher.Invoke(new Action(() => {
                i= (int)player.Position.TotalSeconds;
            }));
            return i;
        }

        public int getDuration()
        {
            int i = 0;
            player.Dispatcher.Invoke(new Action(() =>
            {
                if(player.NaturalDuration.HasTimeSpan)
                i = (int)player.NaturalDuration.TimeSpan.TotalSeconds;
            }));
            return i;
        }

        protected virtual void scrub(int position, int duration)
        {
            if(OnScrub!=null)
             OnScrub(position,duration);
        }

        void round_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (seeking)
            {
                seeking = false;
                if(playing)
                    player.Play();
            }
        }

        public void PlayerShow()
        {
            window.Show();
            window.Activate();
            Dispatcher.Run();
        }

        

        public void DisplayImage(string path)
        {
            try
            {
                img = new Image();
                img.Source = new BitmapImage(new Uri(path, UriKind.Relative));
                mainGrid.Children.Clear();
                mainGrid.Children.Add(img);
                PlayerShow();
            }
            catch
            {
                mainGrid.Dispatcher.BeginInvoke(new Action(() =>
                {
                    img = new Image();
                    img.Source = new BitmapImage(new Uri(path, UriKind.Relative));
                    mainGrid.Children.Clear();
                    mainGrid.Children.Add(img);
                    PlayerShow();
                }));
            }
        }

        void round_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                
                if (player.NaturalDuration.HasTimeSpan)
                {
                    player.Pause();
                    seeking = true;
                    double done = e.GetPosition((Grid)sender).X;
                    double all = ((Grid)sender).Width;
                    double s = player.NaturalDuration.TimeSpan.TotalSeconds * (done / all);

                    player.Position = new TimeSpan(0, 0, Convert.ToInt32(s));
                    Fill(done / all);
                }
            }
        }

        public void SeekPercent(double p)
        {
            player.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (player.NaturalDuration.HasTimeSpan)
                {
                    double s = player.NaturalDuration.TimeSpan.TotalSeconds;


                    player.Position = new TimeSpan(0, 0, Convert.ToInt32(p * s));
                    Fill(p);
                }
            }));
        }

        public void Seek(double seconds)
        {
            player.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (player.NaturalDuration.HasTimeSpan)
                {
                    double s = player.NaturalDuration.TimeSpan.TotalSeconds;


                    player.Position = new TimeSpan(0, 0, Convert.ToInt32(seconds));
                    Fill(seconds / s);
                }
            }));
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (player.LoadedBehavior == MediaState.Manual && player.NaturalDuration.HasTimeSpan)
            {
                double full = player.NaturalDuration.TimeSpan.TotalSeconds;
                double done = player.Position.TotalSeconds;
                scrub((int)done,(int)full);
                Fill(done / full);
            }
        }

        public void Close()
        {
            window.Dispatcher.BeginInvoke(new Action(() =>
            {
                window.Close();
            }));
        }

        private void Fill(double amount)
        {
            GradientStopCollection stops = new GradientStopCollection();
            stops.Add(new GradientStop(Colors.White, amount));
            stops.Add(new GradientStop(Colors.Black, amount));
            LinearGradientBrush brush = new LinearGradientBrush(stops);
            brush.StartPoint = new Point(0, 0.5);
            brush.EndPoint = new Point(1, 0.5);

            round.Background = brush;
        }

        private void Animate(double from, double to)
        {            
            DoubleAnimation myDoubleAnimation = new DoubleAnimation();
            myDoubleAnimation.From = from;
            myDoubleAnimation.To = to;
            myDoubleAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(150));
            Storyboard.SetTarget(myDoubleAnimation, round);
          
            Storyboard.SetTargetProperty(myDoubleAnimation, new PropertyPath(Border.OpacityProperty));

            Storyboard myWidthAnimatedButtonStoryboard = new Storyboard();
            myWidthAnimatedButtonStoryboard.Children.Add(myDoubleAnimation);
            myWidthAnimatedButtonStoryboard.Begin();          
        }

        public void MuteUnMute()
        {
            player.Dispatcher.BeginInvoke(new Action(() =>
            {
                if(player.IsMuted)
                    mute.Source = new BitmapImage(new Uri(@"Media\muted.png", UriKind.Relative));
                else
                    mute.Source = new BitmapImage(new Uri(@"Media\mute.png", UriKind.Relative));

                player.IsMuted = !player.IsMuted;
            }));
        }


        public void Play(string path)
        {            
           
            player.Source = new Uri(path);
            player.Play();
            playing = true;            
            window.Show();
            Dispatcher.Run();
        }

        public void MaxMin()
        {
            window.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (window.WindowState == WindowState.Normal)
                {
                    window.WindowState = WindowState.Maximized;
                    full.Source = new BitmapImage(new Uri(@"Media\_fullscr.png", UriKind.Relative));
                }
                else
                {
                    window.WindowState = WindowState.Normal;
                    full.Source = new BitmapImage(new Uri(@"Media\fullscr.png", UriKind.Relative));
                }
            }));

        }

        public void PlayPause()
        {           
            player.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (playing)
                {
                    player.Pause();
                    play.Source = new BitmapImage(new Uri(@"Media\play.png", UriKind.Relative));
                }
                else
                {
                    player.Play();
                    play.Source = new BitmapImage(new Uri(@"Media\pause.png", UriKind.Relative));
                }

                playing = !playing;

            }));
        }

        public void JustPlay()
        {
            player.Dispatcher.BeginInvoke(new Action(() =>
            {
                
                    player.Play();
                    play.Source = new BitmapImage(new Uri(@"Media\pause.png", UriKind.Relative));
                

                playing = true;

            }));
        }

        public void JustPause()
        {
            player.Dispatcher.BeginInvoke(new Action(() =>
            {
                
                    player.Pause();
                    play.Source = new BitmapImage(new Uri(@"Media\play.png", UriKind.Relative));
              

                playing = false;

            }));
        }
        
    }
}

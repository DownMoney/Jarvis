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
using System.Windows.Media.Animation;
using System.Threading;
using Jarvis.Modules.Knowledge;

namespace Jarvis
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Recognition rec;
        private System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        private bool showing = false;

        public MainWindow()
        {
            InitializeComponent();
            mainWindow.Width = SystemParameters.FullPrimaryScreenWidth;
            mainWindow.Top = -80;
            mainWindow.Left = 0;
            this.RegisterName(mainWindow.Name, mainWindow);
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 3);
          
        }

        void dispatcherTimer_Tick(object sender, EventArgs e)
        {
          /*  if (showing)
            {
                dispatcherTimer.Stop();
                Animate(0, -80);
                showing = false;
            }*/
        }

        private void Animate(int from, int to)
        {
            DoubleAnimation myDoubleAnimation = new DoubleAnimation();
            myDoubleAnimation.From = from;
            myDoubleAnimation.To = to;
            myDoubleAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(150));
            Storyboard.SetTargetName(myDoubleAnimation, mainWindow.Name);
            Storyboard.SetTargetProperty(myDoubleAnimation, new PropertyPath(Window.TopProperty));

            Storyboard myWidthAnimatedButtonStoryboard = new Storyboard();
            myWidthAnimatedButtonStoryboard.Children.Add(myDoubleAnimation);
            myWidthAnimatedButtonStoryboard.Begin(this);
            dispatcherTimer.Start();
        }
        NetworkMonitor nm = new NetworkMonitor();
        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
           
           // nm.MonitorDevice("192.168.0.4");


            rec = new Recognition();
            rec.OnRecognized += rec_OnRecognized;
            rec.OnGridChange += rec_OnGridChange;
            rec.OnMenuChange += rec_OnMenuChange;

            Utilities.Airplay air = new Utilities.Airplay();
            air.authorisationRequest += air_authorisationRequest;
            air.clientConnected += air_clientConnected;
            air.messageSent += air_messageSent;
            air.playbackEvent += air_playbackEvent;
            air.playImage += air_playImage;
            air.playURL += air_playURL;
            
            air.Start();


            
        }

        void rec_OnMenuChange(string text, string colour)
        {
            menuTxt.Dispatcher.BeginInvoke(new Action(() =>
            {
                menuTxt.Text = text;

                try
                {
                    colBorder.BorderBrush = (Brush)new BrushConverter().ConvertFromString(colour);
                }
                catch
                {

                }
            }));
        }

        void air_playURL(object sender, string url, double position)
        {
            
        }

        void air_playImage(object sender, System.IO.MemoryStream theImage)
        {
           
        }

        void air_playbackEvent(object sender, string action, string param = "")
        {
            
        }

        void air_messageSent(object sender, string message)
        {
           
        }

        void air_clientConnected(object sender, string message)
        {
            
        }

        void air_authorisationRequest()
        {
            
        }

        void rec_OnGridChange(Grid g)
        {
            ScrollViewer viewer = new ScrollViewer();
            viewer.Width = mainWindow.Width;
            viewer.Content = g;
            viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            viewer.PreviewMouseWheel += viewer_PreviewMouseWheel;

            mainWindow.Content = viewer;
            mainWindow.Activate();
        }

        void viewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollviewer = sender as ScrollViewer;
            if (e.Delta > 0)
            {
                for(int i=0; i<10; i++)
                    scrollviewer.LineLeft();
            }
                
            else
            {
                for (int i = 0; i < 10; i++)
                    scrollviewer.LineRight();
            }
            e.Handled = true;
        }

        private void changeFont(string text)
        {
            double size = ((recoDisplay.RenderSize.Width + 200) / text.Length)+3;
            if (!double.IsNaN(size) && size <= 48)
                recoTxt.FontSize = size;
            else
                recoTxt.FontSize = 48;
        }

        void rec_OnRecognized(string text, bool show)
        {
            try
            {
                if (text != "")
                {
                    recoTxt.Text = text;
                    changeFont(text);
                }
            }
            catch
            {
                recoTxt.Dispatcher.BeginInvoke(new Action(() => {
                    if (text != "")
                    {
                        recoTxt.Text = text;
                        changeFont(text);
                    }
                }));
            }
            
            if (show && !showing)
            {
                Animate(-80, 0);
                showing = true;
                mainWindow.Activate();
            }

            if (!show && showing)
            {
                Animate(0, -80);
                showing = false;
                mainWindow.Content = mainGrid;
            }
        }

        private void recoDisplay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            
        }

        private void recoDisplay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            rec.SetAttention(false);
            recoTxt.Visibility = System.Windows.Visibility.Hidden;
            inputBox.Visibility = System.Windows.Visibility.Visible;  
            inputBox.Focus();
            
        }

        private void inputBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                rec.ParseFree(inputBox.Text);
                inputBox.Text = "";
                rec.SetAttention(true);
            }
        }
    }
}

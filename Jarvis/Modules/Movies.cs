using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Recognition;
using Jarvis.Modules.Movies;
using System.Net;
using System.Xml;
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

namespace Jarvis.Modules
{
    public class Movie:Module
    {
        private Syntax syntax = new Syntax();
        private Utilities.Player player;

        private struct Film
        {
            public string Name;
            public string img;
            public string rating;
        }

        public event ModuleMessageReceived OnMessageReceived;
        private Speech speech = new Speech("Modules/Movies/Speech.xml");
        private Window chooseFilm = new Window();

        public Movie()
        {
            
            if (!System.IO.File.Exists("Modules/Movies/Films.xml"))
                CreateFilmList();          
        }

        public void SendData(string data)
        {

        }

        private Grammar[] LoadGrammar()
        {
            return syntax.BuildGrammar(RecoScript(), this);
        }

        public string RecoScript()
        {
            return "Modules/Movies/Movies.xml";
        }

        public string BehaviourScript()
        {
            return "Modules/Movies/Behaviour.xml";
        }

        public Recognition.Response Execute(string input, string ruleName)
        {
            string[] s = ruleName.Split('|');
            speech.Respond(input, s[s.Length - 1]);
            switch(s[s.Length-1])
            {
                case "playfilm":
                    Play(input);
                    return End();

                case "resumefilm":
                    PlayPause();
                    return End();

                case "playpausefilm":
                    PlayPause();
                    return End();

                case "choosefilm":
                    RandFilm();
                    return End();

                case "fullscreen":
                    FullScreen();
                    return End();

                case "mute":
                    Mute();
                    return End();

                case "showfilms":
                  //  BuildWindow(GetFilms());
                    Recognition.Response res = new Recognition.Response();
                    res.grid = Test(GetFilms());
                    res.grammars = LoadGrammar();
                    return res;
            }

             Recognition.Response r = new Recognition.Response();
             r.grammars = LoadGrammar();
            return r;
        }

        private void Mute()
        {
            player.MuteUnMute();
        }

        private void FullScreen()
        {
            player.MaxMin();
        }

        private Recognition.Response End()
        {
            Recognition.Response r = new Recognition.Response();
            r.grammars = new Grammar[0];
            return r;
        }

        private void PlayPause()
        {       
            if(player != null)
            player.PlayPause();
        }

        private void Play(string input)
        {
            try
            {
                if (chooseFilm != null)
                    chooseFilm.Close();
            }
            catch
            {

            }
            Thread th = new Thread(new ThreadStart(() => {
                player = new Utilities.Player();
                input = input.Replace("play ", "");
                player.Play(GetPath(input));
                player.JustPlay();
            }));
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
            
        }

        

      
        private void RandFilm()
        {
            Random r = new Random((int)DateTime.Now.Ticks);

            string[] s = GetOptions("");

            int i = r.Next(0, s.Length);
            speech.Speak("Playing "+ s[i]);
            Play(s[i]);
        }
        

        private string GetPath(string film)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("Modules/Movies/Films.xml");

            XmlNode node = doc.SelectSingleNode("/Films/Film[title=\"" + film + "\"]/path");

            return node.InnerText;
        }

        private static string getIP()
        {
            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            return ipHostInfo.AddressList.First().ToString();
        }

        private void CreateFilmList()
        {
            Thread th = new Thread(new ThreadStart(() => { 
            IMDB imdb = new IMDB();
            string[] dirs = new Settings().GetSetting("movieDirs").Split('|');
            for (int i = 0; i < dirs.Length;i++ )
                imdb.GetInfo(new Files().SearchDir(dirs[i]));

            new Speech().Speak("Finished updating the film list!");
            }));

            th.Start();
        }

        public string[] GetOptions(string token)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("Modules/Movies/Films.xml");

            XmlNodeList nodes = doc.SelectNodes("/Films/Film/title");
            string[] s= new string[0];

            foreach (XmlNode n in nodes)
            {
                Array.Resize<string>(ref s, s.Length + 1);
                s[s.Length - 1] = n.InnerText;
            }
           
            return s;
        }

        private Film[] GetFilms()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("Modules/Movies/Films.xml");

            XmlNodeList nodes = doc.SelectNodes("/Films/Film/title");
            Film[] s = new Film[0];

            foreach (XmlNode n in nodes)
            {
                Array.Resize<Film>(ref s, s.Length + 1);
                s[s.Length - 1] = new Film();
                s[s.Length - 1].Name = n.InnerText.Replace("&amp;","&");
                try
                {
                    s[s.Length - 1].rating = doc.SelectSingleNode("/Films/Film[title=\"" + n.InnerText + "\"]/rating").InnerText;
                    s[s.Length - 1].img = doc.SelectSingleNode("/Films/Film[title=\"" + n.InnerText + "\"]/poster").InnerText;
                }
                catch
                {
                    s[s.Length - 1].rating = "??";
                    s[s.Length - 1].img = "";
                }
            }

            return s;
        }

        private Grid Test(Film[] s)
        {
            Grid mainGrid = new Grid();
          
            int left = 80;
            for (int i = 0; i < s.Length; i++)
            {
                Grid g = new Grid();
                g.Margin = new Thickness(left * i, 0, 0, 0);
                TextBlock text = new TextBlock();
                text.Text = s[i].Name;
                text.Margin = new Thickness(0, 65, 0, 0);
                text.VerticalAlignment = VerticalAlignment.Center;
                text.FontSize = 12;
                text.Foreground = Brushes.WhiteSmoke;

                Image img = new Image();
                if (s[i].img != "")
                    img.Source = new BitmapImage(new Uri(s[i].img, UriKind.Absolute));
                img.Width = 75;
                img.Height = 75;
                img.HorizontalAlignment = HorizontalAlignment.Left;

                g.Children.Add(img);
               // g.Children.Add(text);
                g.Name = "a" + i.ToString();
                g.MouseLeftButtonUp += (a, v) => { Play(s[Convert.ToInt32(((Grid)a).Name.Substring(1))].Name); };

                mainGrid.Children.Add(g);

            }

            return mainGrid;
        }

        private void BuildWindow(Film[] s)
        {
            chooseFilm.Width = 600;
            chooseFilm.Height = 800;
            Grid mainGrid = new Grid();
            mainGrid.Background = new ImageBrush(new BitmapImage(new Uri(@"Media\gray_bg.jpg", UriKind.Relative)));
            ListBox box = new ListBox();
            box.Background = Brushes.Transparent;
            for (int i = 0; i < s.Length; i++)
            {
                Grid g = new Grid();
                TextBlock text = new TextBlock();
                text.Text = s[i].Name;
                text.Margin = new Thickness(80, 0, 0, 0);
                text.VerticalAlignment = VerticalAlignment.Center;
                text.FontSize = 22;
                text.Foreground = Brushes.WhiteSmoke;

                Image img = new Image();
                if(s[i].img!="")
                img.Source = new BitmapImage(new Uri(s[i].img, UriKind.Absolute));
                img.Width = 70;
                img.Height = 70;
                img.HorizontalAlignment = HorizontalAlignment.Left;

                g.Children.Add(img);
                g.Children.Add(text);
                g.Name = "a" + i.ToString();
                g.MouseLeftButtonUp += (a, v) => { Play(s[Convert.ToInt32(((Grid)a).Name.Substring(1))].Name); chooseFilm.Close(); };

                box.Items.Add(g);

            }
               // box.Items.Add("asdasd");
            mainGrid.Children.Add(box);
            chooseFilm.Content = mainGrid;
            chooseFilm.Show();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Xml;
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
using System.Threading;
using System.Windows.Threading;

namespace Jarvis.Modules.Knowledge
{
    public delegate void WindowClose();
    public class Wolfram
    {
        private string apiKey = "", endpoint = "http://api.wolframalpha.com/v2/query";

        public event WindowClose OnWindowClose; 

        public Wolfram()
        {

        }

        protected virtual void windowsClose()
        {
            OnWindowClose();
        }

        public void Search(string query)
        {
            string url = endpoint + "?input=" + query.Replace(" ", "+") + "&appid=" + apiKey;

            XmlDocument doc = GetDocument(url);
            XmlNodeList pods = doc.SelectNodes("//pod");

            Thread th = new Thread(new ParameterizedThreadStart(BuildReport));
            th.SetApartmentState(ApartmentState.STA);
            th.Start(pods);

            
        }

        private void BuildReport(object o)
        {
            XmlNodeList pods = (XmlNodeList)o;

            Grid mainGrid = new Grid();
            mainGrid.VerticalAlignment = VerticalAlignment.Top;
            ListBox list = new ListBox();
            mainGrid.Children.Add(list);

            for (int i = 0; i < pods.Count; i++)
            {
                try
                {
                    list.Items.Add(ProcessPod(pods[i]));
                }
                catch
                {

                }
            }

            Window window = new Window();
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Closing += window_Closing;

            window.Height = 700;
            window.Width = 550;

            window.ResizeMode = ResizeMode.NoResize;
            window.WindowStyle = WindowStyle.ToolWindow;
            window.Content = mainGrid;
            window.Show();

            Dispatcher.Run();
        }

        void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            windowsClose();
        }

        private Grid ProcessPod(XmlNode pod)
        {
            Grid g = new Grid();
            g.VerticalAlignment = VerticalAlignment.Top;
           // g.Margin = new Thickness(0, 200, 0, 0);

            string title = pod.Attributes["title"].InnerText;
            string imgLink = pod.SelectSingleNode("//pod[@title=\"" + title + "\"]/subpod/img").Attributes["src"].InnerText;

            TextBlock txt = new TextBlock();

            txt.Text = title;
            txt.VerticalAlignment = VerticalAlignment.Top;
            txt.HorizontalAlignment = HorizontalAlignment.Left;
            txt.FontFamily = new FontFamily("Kozuka Gothic Pr6N EL");
            txt.FontSize = 20;
            g.Children.Add(txt);

            Image img = new Image();
            img.Stretch = Stretch.None;
            img.Margin = new Thickness(0, 50, 0, 0);
            img.Source = new BitmapImage(new Uri(imgLink, UriKind.Absolute));
            g.Children.Add(img);

            return g;
        }

        private XmlDocument GetDocument(string url)
        {
            WebClient client = new WebClient();
            string data = client.DownloadString(url);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(data);

            return doc;
        }
    }
}

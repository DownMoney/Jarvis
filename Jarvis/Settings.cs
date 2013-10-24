using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Jarvis
{
    public class Settings
    {
        private Dictionary<string, string> _Settings = new Dictionary<string, string>();

        public Settings()
        {
            LoadSettings("Settings/Settings.xml");
        }

        public string GetSetting(string Name)
        {
            if (_Settings.ContainsKey(Name))
                return _Settings[Name];
            else
                return "";
        }

        private void LoadSettings(string path)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);

            XmlNodeList nodes = doc.SelectNodes("/Settings/Setting");

            foreach (XmlNode n in nodes)
            {
                if(!_Settings.ContainsKey(n.Attributes["type"].InnerText))
                    _Settings.Add(n.Attributes["type"].InnerText, n.InnerText);
            }
        }
    }    
}

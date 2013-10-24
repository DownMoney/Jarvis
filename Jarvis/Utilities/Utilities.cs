using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.Modules;
using System.IO;
using System.Xml;
using Jarvis.Modules.Knowledge;
using Jarvis.Modules.Home;

namespace Jarvis.Utilities
{
    public class Utilities
    {
        private Dictionary<string, Module> Modules = new Dictionary<string, Module>();

        public Utilities()
        {
            LoadModules();
        }

        private void LoadModules()
        {            
            Modules.Add("movies", new Movie());
            Modules.Add("knowledge", new Knowledge());
            Modules.Add("alarm", new Alarm());
            Modules.Add("weather", new Weather());
            
            string[] files = Directory.EnumerateDirectories("Apps/Home").ToArray<string>();

            for (int i = 0; i < files.Length; i++)
            {
                if (File.Exists(files[i] + "\\Settings.xml"))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(files[i] + "\\Settings.xml");
                    string name = doc.SelectSingleNode("/Settings/Setting[@type=\"name\"]").InnerText;
                    

                    Modules.Add(name, new Addon(files[i]));
                }
            }
        }

        public Dictionary<string,Module> GetModules()
        {
            return Modules;
        }
    }
}

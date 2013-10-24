using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Speech.Recognition;

namespace Jarvis
{
   
    public class Addon:Module
    {
        private string path = "";
        private Speech speech;
        private Process app;
        private Server server;
        private string _input = "", _rulename = "";
        private bool connected = false;

        public event ModuleMessageReceived OnMessageReceived;

        public Addon(string _path)
        {
            path = _path;
            speech = new Speech(path+"\\Speech.xml");
        }

        public void SendData(string data)
        {
            if (connected)
            {
                server.ExecuteCommand(data, "data");
            }
        }

        public string BehaviourScript()
        {
            if (File.Exists(path + "\\Behaviour.xml"))
                return path + "\\Behaviour.xml";

            return "";
        }

        public string RecoScript()
        {
            if (File.Exists(path + "\\Recognition.xml"))
                return path + "\\Recognition.xml";

            return "";
        }

        private Recognition.Response End()
        {
            Recognition.Response r = new Recognition.Response();
            r.grammars = new Grammar[0];
            return r;
        }
        
        public Recognition.Response Execute(string input, string ruleName)
        {
            string[] s = ruleName.Split('|');
            speech.Respond(input, s[s.Length - 1]);
            int port = new Random().Next(38880,38899);
            XmlDocument doc = new XmlDocument();
            doc.Load(path + "\\Settings.xml");
            string name = doc.SelectSingleNode("/Settings/Setting[@type=\"name\"]").InnerText;
            server = new Server(port,name);

           

            string start = doc.SelectSingleNode("/Settings/Setting[@type=\"startup\"]").InnerText;
            
            app = Process.Start(path+"\\"+start, "-port "+port.ToString());
            server.OnAuthorized += server_OnAuthorized;
            server.OnMessageReceived += server_OnMessageReceived;
            _input = input;
            _rulename = ruleName;
           // server.ExecuteCommand(input, ruleName);

            return End();
        }
               

        void server_OnAuthorized()
        {
            connected = true;
            server.ExecuteCommand(_input, _rulename);
        }

        private void server_OnMessageReceived(string input, string ruleName)
        {            
            OnMessageReceived(input, ruleName, this);
        }

        public string[] GetOptions(string token)
        {
            return new string[0];
        }
    }
}

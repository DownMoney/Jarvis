using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Recognition;
using System.Text.RegularExpressions;


namespace Jarvis.Modules.Knowledge
{
    public class Knowledge:Module
    {
        private Wikipedia wiki = new Wikipedia();
        private Wolfram wolf = new Wolfram();
        private Speech speech = new Speech();
        public event ModuleMessageReceived OnMessageReceived;

        public Knowledge()
        {
            wolf.OnWindowClose += wolf_OnWindowClose;
        }

        public string BehaviourScript()
        {
            return "";
        }

        public void SendData(string data)
        {

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

            string token = s[s.Length - 1];

            switch(token.ToLower())
            {
                case "report":
                    return Search(input);
            }

            return End();
        }

        public string RecoScript()
        {
            return "";
        }

        public string[] GetOptions(string token)
        {
            return new string[0];
        }

        private void wolf_OnWindowClose()
        {
            speech.Stop();
        }

        private Recognition.Response Search(string query)
        {
            Recognition.Response re = new Recognition.Response();
            re.grammars = new Grammar[0];

            Regex r = new Regex(@"(on|about) ((\w|\s)+) please", RegexOptions.Compiled);
            Match m = r.Match(query);
            if (m.Groups.Count > 1)
            {
                query = m.Groups[2].Value;
                wolf.Search(query);
                string text = wiki.Search(query);
                re.data = text;
                speech.Speak(text);
            }

            return re;
        }

    }
}

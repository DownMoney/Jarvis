using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Recognition;

namespace Jarvis.Modules
{
    public class Music:Module
    {
        private Syntax syntax = new Syntax();
        private Speech speech = new Speech("Modules/Music/Speech.xml");
        public event ModuleMessageReceived OnMessageReceived;
        public Music()
        {

        }

        public string BehaviourScript()
        {
            return "Modules/Music/Behaviour";
        }

        public void SendData(string data)
        {

        }

        public string RecoScript()
        {
            return "Modules/Music/Music.xml";
        }

        public Recognition.Response Execute(string input, string ruleName)
        {
            string[] s = ruleName.Split('|');
            speech.Respond(input, s[s.Length - 1]);
            switch (s[s.Length - 1])
            {
               
            }

            Recognition.Response r = new Recognition.Response();
            r.grammars = LoadGrammar();
            return r;
        }

        private Grammar[] LoadGrammar()
        {
            return syntax.BuildGrammar(RecoScript(), this);
        }

        public string[] GetOptions(string token)
        {
            return new string[0];
        }

        private Recognition.Response End()
        {
            Recognition.Response r = new Recognition.Response();
            r.grammars = new Grammar[0];
            return r;
        }
    }
}

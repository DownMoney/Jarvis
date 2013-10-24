using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Speech.Recognition;

namespace Jarvis.Modules.Home
{
    public class Alarm:Module
    {
        public event ModuleMessageReceived OnMessageReceived;
        private Speech speech = new Speech();
        private Syntax syntax = new Syntax();

        public Alarm()
        {

        }

        public string BehaviourScript()
        {
            return "Modules/Home/Behaviour.xml";
        }

        public string RecoScript()
        {
            return "Modules/Home/Alarm.xml";
        }

        public Recognition.Response Execute(string input, string ruleName)
        {
            string[] s = ruleName.Split('|');
            speech.Respond(input, s[s.Length - 1]);
            switch (s[s.Length - 1])
            {
                case "set":
                    Set(input);
                    break;
                case "on":
                    On(input);
                    break;

            }
            Recognition.Response r = new Recognition.Response();
            r.grammars = LoadGrammar();
            return r;
        }

        private void On(string input)
        {
            speech.Speak(input);
        }

        private Grammar[] LoadGrammar()
        {
            return syntax.BuildGrammar(RecoScript(), this);
        }

        private void Set(string input)
        {
            Regex r = new Regex(@"\d?\d(:|\s)?\d?\d?", RegexOptions.Compiled);
            Match m = r.Match(input);
            if (m.Groups.Count > 0)
            {
                Scheduler s = new Scheduler();
                s.AddTask("Good morning", "alarm|on", m.Groups[0].Value.Replace(" ",":"), false);
                speech.Speak("Setting an alarm for " + m.Groups[0].Value);
            }
        }

        public string[] GetOptions(string token)
        {
            if (token == "time")
            {
                return buildTime();
            }
            return new string[0];
        }

        private string[] buildTime()
        {
            string[] s = new string[0];
            string min = "";
            string hour = "";

            for (int i = 0; i < 24; i++)
            {
                hour = i.ToString();
                if (i < 10)
                    hour = "0" + hour;
                
                for (int j = 0; j < 60; j++)
                {
                    Array.Resize<string>(ref s, s.Length + 1);
                    min = j.ToString();
                    if (j < 10)       
                        min = "0" + min;
                    
                    s[s.Length - 1] = hour + " " + min;
                }
            }

            return s;
        }

        public void SendData(string data)
        {

        }
    }
}

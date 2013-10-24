using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Speech.Synthesis;
using WindowsMicrophoneMuteLibrary;
using System.Text.RegularExpressions;

namespace Jarvis
{
    public class Speech
    {
        private WindowsMicMute mute = new WindowsMicMute();
        private Dictionary<string, string[]> Responses = new Dictionary<string, string[]>();
        private SpeechSynthesizer speech = new SpeechSynthesizer();
        private Settings settings = new Settings();

        public Speech(string path = "Settings/Speech.xml")
        {
            speech.SpeakCompleted += speech_SpeakCompleted;
            LoadResponses(path);
        }

        void speech_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            mute.UnMuteMic();
        }

        public void Stop()
        {
            speech.SpeakAsyncCancelAll();
        }

        public void Speak(string s)
        {
            mute.MuteMic();
            speech.SpeakAsync(s);
        }

        private void LoadResponses(string path)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);

            XmlNodeList list = doc.SelectNodes("/Sentences/Sentence");

            foreach (XmlNode n in list)
            {
                string type = n.Attributes["type"].InnerText.ToLower();

                XmlNodeList pharses = doc.SelectNodes("/Sentences/Sentence[@type=\"" + type + "\"]/Phrase");

                string[] s = new string[0];

                foreach (XmlNode node in pharses)
                {
                    Array.Resize<string>(ref s, s.Length + 1);
                    s[s.Length - 1] = Update(node.InnerText);
                }

                Responses.Add(type, s);
            }
        }

        private string Update(string input)
        {
            string[] s = getTokens(input);

            for (int i = 0; i < s.Length; i++)
            {
                string set = settings.GetSetting(s[i]);

                input = input.Replace("{" + s[i] + "}", set);
            }

            return input;
        }

        private string[] getTokens(string text)
        {
            Regex brackets = new Regex(@"\{([a-zA-Z]+)\}", RegexOptions.Compiled);
            MatchCollection m = brackets.Matches(text);

            string[] s = new string[m.Count];

            for (int i = 0; i < m.Count; i++)
                s[i] = m[i].Groups[1].Value;

            return s;
        }

        public void Respond(string input, string ruleName)
        {
            if (Responses.ContainsKey(ruleName))
            {
                string res = RandomResponse(Responses[ruleName]);
                mute.MuteMic();
                speech.SpeakAsync(res);
            }
        }

        private string RandomResponse(string[] s)
        {
            Random r = new Random((int)DateTime.Now.Ticks);

            int i = r.Next(0, s.Length);

            return s[i];
        }
    }
}

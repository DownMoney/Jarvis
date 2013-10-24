using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Recognition;
using Jarvis.Modules;
using System.Text.RegularExpressions;
using System.Xml;

namespace Jarvis
{
    public delegate void AttChange(bool b);
    public delegate void Response(Recognition.Response res);
    public delegate void ChangeMenu(string text, string colour);

    public class Parser
    {
        private Speech speech = new Speech();
        private Dictionary<string, Module> Modules = new Dictionary<string, Module>();
        private Utilities.Utilities util = new Utilities.Utilities();
        private Server server = new Server();
        private Scheduler scheduler;
        public event AttChange OnAttentionChange;
        public event Response OnResponse;
        public event ChangeMenu OnMenuChange;

        public Parser()
        {
            server.OnMessageReceived += server_OnMessageReceived; 
            LoadModules();
            scheduler = new Scheduler();
            scheduler.OnTick += scheduler_OnTick;
        }

        void scheduler_OnTick(string input, string ruleName)
        {
            addResponse(Parse(input, ruleName));
        }

        void server_OnMessageReceived(string input, string ruleName)
        {
            addResponse(Parse(input, ruleName));
        }
        
        private void LoadModules()
        {
            Modules = util.GetModules();
           
            foreach(var v in Modules)
                Modules[v.Key].OnMessageReceived += Parser_OnMessageReceived;
        }

        void Parser_OnMessageReceived(string input, string ruleName, Module self)
        {

            string[] s = ruleName.Split('|');

           
           
            if (s[0] == "speak")
                speech.Speak(input);

            if (s[0] == "execute")
            {
                ruleName = "";

                for (int i = 1; i < s.Length; i++)
                    ruleName += s[i] + "|";

                ruleName = ruleName.Substring(0, ruleName.Length - 1);
                Recognition.Response rec = Parse(input, ruleName);
                self.SendData(rec.data);
                addResponse(rec);
            }


            Recognition.Response r = new Recognition.Response();
            r.text = input;
            r.grammars = new Grammar[0];

            addResponse(r);
        }

        public Recognition.Response Parse(string input, string ruleName)
        {

            if (ruleName == "schedule|add")
            {
                string[] ss = input.Split(';');
                try
                {
                    int u = Convert.ToInt32(ss[2]);
                    scheduler.AddTask(ss[0], ss[1], u, Convert.ToBoolean(ss[3]), Convert.ToBoolean(ss[4]));
                }
                catch
                {
                    scheduler.AddTask(ss[0], ss[1], ss[2], Convert.ToBoolean(ss[3]), Convert.ToBoolean(ss[4]));
                }

            }

            if (ruleName == "rest")
                changeAttention(false);

            if (ruleName == "attention")
            {
                changeAttention(true);
                changeMenu("home", "#FF0051FF");
            }


            string[] s = ruleName.Split('|');
            changeMenu(s[0], "red");
            speech.Respond(input, s[s.Length-1]);

            if (Modules.ContainsKey(s[0]))
            {
                ExecuteBehaviour(Modules[s[0]], ruleName);
                return Modules[s[0]].Execute(input, ruleName);

            }

            return new Recognition.Response();
        }

        protected virtual void changeAttention(bool b)
        {
            OnAttentionChange(b);
        }

        protected virtual void changeMenu(string text, string colour)
        {
            try
            {
                OnMenuChange(text, colour);
            }
            catch
            {

            }
        }

        private void ExecuteBehaviour(Module m, string ruleName)
        {
            XmlDocument doc = new XmlDocument();
            string path = m.BehaviourScript();
            if (path != "")
            {
                doc.Load(path);

                XmlNodeList nodes = doc.SelectNodes("/Behaviours/Behaviour[@onEvent=\"" + ruleName + "\"]");

                foreach (XmlNode n in nodes)
                {
                    for (int i = 0; i < n.ChildNodes.Count; i++)
                    {
                        string cmd = n.ChildNodes[i].InnerText;
                        string rule = n.ChildNodes[i].Attributes["type"].InnerText;

                        addResponse(Parse(cmd, rule));
                    }
                }
            }
        }

        protected virtual void addResponse(Recognition.Response r)
        {
            OnResponse(r);
        }

        public void ParseFree(string input)
        {
            string rule = isInModule(input, "Settings/Recognition.xml");
            if (rule != "")
                addResponse(Parse(input, rule));

            foreach (var m in Modules)
            {
                string mod = m.Key;
                string reco = m.Value.RecoScript();

                string ruleName = isInModule(input, reco);

                if (ruleName != "")
                    addResponse(Parse(input, ruleName));

            }
        }

        private string isInModule(string input, string path)
        {
            if (path != "")
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path);

                XmlNodeList nodes = doc.SelectNodes("/Sentences/Sentence");

                for (int i = 0; i < nodes.Count; i++)
                {
                    string type = nodes[i].Attributes["type"].InnerText;
                    XmlNodeList phrases = doc.SelectNodes("/Sentences/Sentence[@type=\"" + type + "\"]/Phrase");

                    for (int j = 0; j < phrases.Count; j++)
                    {
                        if (isThePhrase(input, phrases[j].InnerText))
                            return type;
                    }
                }
            }
            return "";
        }

        private bool isThePhrase(string input, string phrase)
        {
            phrase = Regex.Replace(phrase, @"\{(dict|wild)\}", @".+");

            Regex r = new Regex(phrase, RegexOptions.Compiled);

            return r.IsMatch(input);
        }
    }
}

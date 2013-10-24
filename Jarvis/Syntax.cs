using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Recognition;
using System.Xml;
using System.Text.RegularExpressions;

namespace Jarvis
{
    public class Syntax
    {
        //private Utilities.Utilities util = new Utilities.Utilities();
        private Dictionary<string, Module> Modules = new Dictionary<string, Module>();
        private Settings settings = new Settings();

        public struct GrammarExtension
        {
            public string Name;
            public Grammar grammar;
            public string file;
        }

        public Syntax()
        {
            LoadModules();
        }

        private void LoadModules()
        {
           // Modules = util.GetModules();
        }

        public Grammar[] BuildGrammar(string path, Module m)
        {
            XmlDocument doc = new XmlDocument();

            doc.Load(path);
            XmlNodeList list = doc.SelectNodes("/Sentences/Sentence");

            Grammar[] grammars = new Grammar[0];

            foreach (XmlNode node in list)
            {
                string module = node.Attributes["type"].InnerText;
                double weight = Convert.ToDouble(node.Attributes["weight"].InnerText);

                XmlNodeList nodes = doc.SelectNodes("/Sentences/Sentence[@type=\"" + module + "\"]/Phrase");

                foreach (XmlNode n in nodes)
                {
                    GrammarBuilder gb = new GrammarBuilder();
                    gb.Culture = new System.Globalization.CultureInfo("en-GB");
                    gb.Append(prepGrammar(n, module, m));

                    Array.Resize<Grammar>(ref grammars, grammars.Length + 1);
                    grammars[grammars.Length - 1] = new Grammar(gb);
                    grammars[grammars.Length - 1].Weight = (float)weight;
                    grammars[grammars.Length - 1].Name = module.ToLower();
                }
                
                
            }           

            return grammars;
        }

        private string[] getOptions(string module, string token)
        {
            string s = settings.GetSetting(token);

            if (s != "")
                return new string[] { s };

            if (token == "dict")
            {
                return new string[] { "dict" };
            }

            if (token == "wild")
            {
                return new string[] { "wild" };
            }

            
            return new string[0];
        }

        GrammarBuilder prepGrammar(XmlNode node, string module, Module m)
        {
            GrammarBuilder gb = new GrammarBuilder();
            gb.Culture = new System.Globalization.CultureInfo("en-GB");
            string sentence = node.InnerText;
            string[] tokens = getTokens(node.InnerText);
            string[] s = Regex.Split(sentence, @"(\{[a-zA-Z]+\})");

            for (int i = 0; i < tokens.Length; i++)
            {

                string[] options;
                if(m!=null)
                options= m.GetOptions(tokens[i]);
                else
                    options = getOptions(module, tokens[i]);

                    if (options.Length > 0)
                       gb.Append(replaceToken(ref s, options, tokens[i]));
                
            }

            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] != "" && s[i] != " ")
                    gb.Append(s[i]);
            }

            return gb;
        }

        GrammarBuilder replaceToken(ref string[] partialSentence, string[] options, string token)
        {
            GrammarBuilder gb = new GrammarBuilder();
            gb.Culture = new System.Globalization.CultureInfo("en-GB");
            token = "{" + token + "}";
            for (int i = 0; i < partialSentence.Length; i++)
            {
                if (partialSentence[i] == token)
                {
                    partialSentence[i] = " ";
                    if (token == "{dict}")
                    {
                        gb.AppendDictation();
                    }
                    else
                    {
                        if (token == "{wild}")
                            gb.AppendWildcard();
                        else
                        {
                            Choices ch = new Choices(options);
                            gb.Append(ch);
                            return gb;
                        }
                    }
                }
                else
                {
                    if (partialSentence[i] != "")
                    {
                        if (partialSentence[i].Contains("{"))
                            return gb;
                        gb.Append(partialSentence[i]);
                        partialSentence[i] = "";
                    }
                }
            }

            return gb;
        }



        string[] getTokens(string text)
        {
            Regex brackets = new Regex(@"\{([a-zA-Z]+)\}", RegexOptions.Compiled);
            MatchCollection m = brackets.Matches(text);

            string[] s = new string[m.Count];

            for (int i = 0; i < m.Count; i++)
                s[i] = m[i].Groups[1].Value;

            return s;
        }
    }
}

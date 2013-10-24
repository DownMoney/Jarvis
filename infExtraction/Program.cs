using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Text.RegularExpressions;
using System.IO;

namespace infExtraction
{
    class Program
    {
        static Dictionary<string, int> Freq = new Dictionary<string, int>();

        struct State
        {
            public Dictionary<string, int> Tokens;
            public Dictionary<States, double> Transition;
            public Dictionary<States, double> Emission;
        }

        enum States { start, bg, end, target, prefix, suffix };

        static Dictionary<States, State> hmm = new Dictionary<States, State>();

        static void Main(string[] args)
        {
            /*Train("Training Data/Data.xml");
            ConvertToProb();
            Tag(Console.ReadLine());*/

            Brain b = new Brain("Brain.xml");
            b.Train();
            StreamReader file = new StreamReader("test.txt");

            string q = file.ReadToEnd();
            HMM.Result result = b.Tag(q);
            Console.Write("\n\nFINAL: " + result.States[0]);
            for (int i = 0; i < result.Tokens.Length; i++)
            {
                Console.Write(" "+result.Tokens[i] + "/" + result.States[i + 1]);
            }
            Console.Write(" "+result.States[result.States.Length - 1] + "\n");

            Dictionary<string, string[]> d = b.Extract(result, "name,surname,location");

            Console.WriteLine("");

            foreach (var v in d)
            {
                Console.WriteLine(v.Key);
                for (int i = 0; i < v.Value.Length; i++)
                {
                    Console.WriteLine("     " + v.Value[i]);
                }
            }

            Console.ReadLine();
        }

        static void Tag(string input)
        {
            string[] tokens = input.Split(' ');
            States[] states = new States[tokens.Length+2];

            states[0] = States.start;

            for (int i = 0; i < tokens.Length; i++)
            {
                states[i+1] = SearchIndex(tokens[i]);
            }

            states[states.Length - 1] = States.end;

            for (int i = 0; i < states.Length-1; i++)
            {
                if (states[i + 1] == States.bg)
                {
                    
                    States posState = States.bg;
                    double max = -1;
                    try
                    {
                        max = hmm[states[i]].Transition[posState] * hmm[posState].Transition[states[i + 2]];
                    }
                    catch
                    {

                    }
                    foreach (var v in hmm[states[i]].Transition)
                    {
                        try
                        {
                            double d = hmm[states[i]].Transition[v.Key] * hmm[v.Key].Transition[states[i + 2]];
                            if (d >= max)
                            {
                                max = d;
                                posState = v.Key;
                            }
                        }
                        catch
                        {

                        }
                    }
                    states[i+1] = posState;
                }   
            }

            Console.Write("\n"+states[0].ToString());
            for(int i=1; i<states.Length-1; i++)
            {
                Console.Write(" " + tokens[i - 1] + "/" + states[i].ToString());
            }
            Console.Write(" " + states[states.Length - 1]);

        }

        static States SearchIndex(string s)
        {
            s = s.ToLower();
            foreach (var v in hmm)
            {
                if (hmm[v.Key].Tokens.ContainsKey(s))
                    return v.Key;
            }

            return States.bg;
        }


        static void Train(string path)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);

            XmlNodeList items = doc.SelectNodes("/Data/Item");

            for (int i = 0; i < items.Count; i++)
                Process(items[i]);
        }

        

        static void Process(XmlNode node)
        {
            string s = node.InnerText;
            States currentState = States.start;
            Regex r = new Regex(@"/((\w|\s)+)/(\w+)", RegexOptions.Compiled);

            string[] tokens = s.Split(' ');

            for (int i = 0; i < tokens.Length; i++)
            {
                tokens[i] = tokens[i].Replace("_", " ");
                Match m = r.Match(tokens[i]);
                if (m.Groups.Count > 1)
                {
                    
                    string prefix = "", suffix = "";

                    if (i > 0)
                    {
                        prefix = tokens[i - 1];
                        AddToHMM(currentState, States.prefix);

                        currentState = States.prefix;
                        AddToken(currentState, prefix);
                    }

                    string token = m.Groups[1].Value;
                    AddToHMM(currentState, States.target);

                    currentState = States.target;
                    AddToken(currentState, token);

                    if (i + 1 < tokens.Length)
                    {
                        suffix = tokens[i + 1];
                        AddToHMM(currentState, States.suffix);

                        currentState = States.suffix;
                        AddToken(currentState, suffix);
                    }



                }

                AddToHMM(currentState, States.bg);

                currentState = States.bg;
                AddToken(currentState, tokens[i]);
            }


            AddToHMM(currentState, States.end);

          

            currentState = States.end;
        }

        static void AddToken(States state, string token)
        {
            token = token.ToLower();
            if (hmm.ContainsKey(state))
            {
                if (hmm[state].Tokens.ContainsKey(token))
                {
                    hmm[state].Tokens[token]++;
                }
                else
                {
                    hmm[state].Tokens.Add(token, 1);
                }
            }
            else
            {
                State _state = new State();
                _state.Transition = new Dictionary<States, double>();
                _state.Emission = new Dictionary<States, double>();
                _state.Tokens = new Dictionary<string, int>();
                hmm.Add(state, _state);
                hmm[state].Tokens.Add(token, 1);
            }
        }

        static void AddToHMM(States currentState, States state)
        {
            if (hmm.ContainsKey(currentState))
            {
                if (hmm[currentState].Transition.ContainsKey(state))
                    hmm[currentState].Transition[state]++;
                else
                {
                    hmm[currentState].Transition.Add(state, 1);
                }
            }
            else
            {
                State _state = new State();
                _state.Transition = new Dictionary<States, double>();
                _state.Emission = new Dictionary<States, double>();
                _state.Tokens = new Dictionary<string, int>();
                hmm.Add(currentState, _state);               
                hmm[currentState].Transition.Add(state, 1);
            }
        }

        static void ConvertToProb()
        {
            Dictionary<States, double> f = new Dictionary<States, double>();

            foreach (var v in hmm)
            {
                f.Add(v.Key, 0);
                foreach (var k in hmm[v.Key].Transition)
                {
                    f[v.Key] += hmm[v.Key].Transition[k.Key];
                }
            }

            List<States> keys = new List<States>(hmm.Keys);

                foreach (var v in keys)
                {
                    List<States> _keys = new List<States>(hmm[v].Transition.Keys);
                    foreach (var k in _keys)
                    {
                        try
                        {
                            hmm[v].Transition[k] /= f[v];
                        }
                        catch
                        {

                        }
                    }
                }
        }
    }
}

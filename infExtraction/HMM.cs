using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace infExtraction
{
    public class HMM
    {

        private Dictionary<string, string[]> model;
        private string[] trainningData;

        private struct State
        {
            public Dictionary<string, double> Tokens;
            public Dictionary<string, double> Transition;
        }

        public struct Result
        {
            public string[] States;
            public string[] Tokens;
            public double Probability;
        }

        private Dictionary<string, State> FSM = new Dictionary<string, State>();

        public HMM(Dictionary<string, string[]> _model, string[] _trainingData)
        {
            model = _model;
            trainningData = _trainingData;
            Train();
        }

        public void Train()
        {
            for (int i = 0; i < trainningData.Length; i++)
            {
                Process(trainningData[i]);
            }

            ConvertToProb();
            ConvertToProb2();
        }

        private double CalculateProbability(string[] states)
        {
            double d = 0.00;
            for (int i = 0; i < states.Length - 1; i++)
            {
                if (FSM.ContainsKey(states[i]) && FSM[states[i]].Transition.ContainsKey(states[i+1]))
                {
                    d += FSM[states[i]].Transition[states[i + 1]];
                }
            }

            return d/states.Length;
        }

        private string Strip(string s)
        {
            s = s.Replace("\"", "");
            s = s.Replace(".", "");
            s = s.Replace(",", "");
            s = s.Replace("(", "");
            s = s.Replace(")", "");
            s = s.Replace("-", "");

            return s;
        }
        public Result Tag(string input)
        {
            input = Strip(input);
            string[] tokens = input.Split(' ');
            string[] states = new string[tokens.Length + 2];

            states[0] = "start";

            for (int i = 0; i < tokens.Length; i++)
            {
                states[i + 1] = SearchIndex(tokens[i]);
            }

            states[states.Length - 1] = "end";

            
            for (int i = 0; i < states.Length - 1; i++)
            {
                if (states[i + 1] == "bg")
                {

                    string posState = "bg";
                    double max = -1;
                    try
                    {
                        max = FSM[states[i]].Transition[posState] * FSM[posState].Transition[states[i + 2]];
                    }
                    catch
                    {

                    }
                    foreach (var v in FSM[states[i]].Transition)
                    {
                        try
                        {
                            double d = FSM[states[i]].Transition[v.Key] * FSM[v.Key].Transition[states[i + 2]];
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
                    states[i + 1] = posState;
                }
            }

            states = FillStates(states);

            Result r = new Result();
            r.Probability = CalculateProbability(states);

            r.States = states;
            r.Tokens = tokens;

         /*   Console.Write("\n\n" + states[0].ToString());
            for (int i = 1; i < states.Length - 1; i++)
            {
                Console.Write(" " + tokens[i - 1] + "/" + states[i].ToString());
            }
            Console.Write(" " + states[states.Length - 1]);*/

            return r;
        }

        private string[] FillStates(string[] s)
        {
            for (int i = 1; i < s.Length-1; i++)
            {
               // if (isTransitionPermitted(s[i], s[i + 1]))
               // {
                    string h = NecessaryTrans(s[i]);
                    if (h != "")
                    {
                        s[i + 1] = h;
                    }
              //  }
            }
            return s;
        }

        private string NecessaryTrans(string state)
        {
            if (model[state].Length == 1)
                return model[state][0];
            else
                return "";
        }

        private string SearchIndex(string s)
        {
            string cur = "bg";
            double max = -1;
            s = s.ToLower();
            foreach (var v in FSM)
            {
                if (FSM[v.Key].Tokens.ContainsKey(s))
                {
                    if (FSM[v.Key].Tokens[s] > max)
                    {
                        max = FSM[v.Key].Tokens[s];
                        cur = v.Key;
                    }
                }
            }

            return cur;
        }

        private void ConvertToProb2()
        {

            Dictionary<string, double> f = new Dictionary<string, double>();

            foreach (var v in FSM)
            {
                f.Add(v.Key, 0);
                foreach (var k in FSM[v.Key].Tokens)
                {
                    f[v.Key] += FSM[v.Key].Tokens[k.Key];
                }
            }

            List<string> keys = new List<string>(FSM.Keys);

            foreach (var v in keys)
            {
                List<string> _keys = new List<string>(FSM[v].Tokens.Keys);
                foreach (var k in _keys)
                {
                    try
                    {
                        FSM[v].Tokens[k] /= f[v];
                    }
                    catch
                    {

                    }
                }
            }
        }

        private void ConvertToProb()
        {

            Dictionary<string, double> f = new Dictionary<string, double>();

            foreach (var v in FSM)
            {
                f.Add(v.Key, 0);
                foreach (var k in FSM[v.Key].Transition)
                {
                    f[v.Key] += FSM[v.Key].Transition[k.Key];
                }
            }

            List<string> keys = new List<string>(FSM.Keys);

            foreach (var v in keys)
            {
                List<string> _keys = new List<string>(FSM[v].Transition.Keys);
                foreach (var k in _keys)
                {
                    try
                    {
                        FSM[v].Transition[k] /= f[v];
                    }
                    catch
                    {

                    }
                }
            }
        }

        private bool isTransitionPermitted(string currentState, string state)
        {
        
                if (model.ContainsKey(currentState))
                {
                    for (int j = 0; j < model[currentState].Length; j++)
                    {
                        if (model[currentState][j].ToLower() == state.ToLower())
                            return true;
                    }
                }                
            

            return false;
        }

        private void Process(string s)
        {
            string currentState = "start";
            Regex r = new Regex(@"/((\w|\s)+)/(\w+)", RegexOptions.Compiled);
            string[] tokens = s.Split(' ');

            for (int i = 0; i < tokens.Length; i++)
            {
                tokens[i] = tokens[i].Replace("_", " ");
                Match m = r.Match(tokens[i]);

                if (m.Groups.Count > 1)
                {
                    string tag = m.Groups[3].Value;

                    if (isTransitionPermitted(currentState, tag))
                    {
                        AddToHMM(currentState, tag);
                        currentState = tag;
                        AddToken(currentState, m.Groups[1].Value);
                    }
                    else
                    {
                        AddToHMM(currentState, "bg");

                        currentState = "bg";
                        AddToken(currentState, tokens[i]);
                    }

                }
                else
                {
                    AddToHMM(currentState, "bg");

                    currentState = "bg";
                    AddToken(currentState, tokens[i]);
                }

               
            }

            AddToHMM(currentState, "end");
            currentState = "end";
        }

        private void AddToken(string state, string token)
        {
            token = token.ToLower();
            if (FSM.ContainsKey(state))
            {
                if (FSM[state].Tokens.ContainsKey(token))
                {
                    FSM[state].Tokens[token]++;
                }
                else
                {
                    FSM[state].Tokens.Add(token, 1);
                }
            }
            else
            {
                State _state = new State();
                _state.Transition = new Dictionary<string, double>();                
                _state.Tokens = new Dictionary<string, double>();
                FSM.Add(state, _state);
                FSM[state].Tokens.Add(token, 1);
            }
        }

        private void AddToHMM(string currentState, string state)
        {
            if (FSM.ContainsKey(currentState))
            {
                if (FSM[currentState].Transition.ContainsKey(state))
                    FSM[currentState].Transition[state]++;
                else
                {
                    FSM[currentState].Transition.Add(state, 1);
                }
            }
            else
            {
                State _state = new State();
                _state.Transition = new Dictionary<string, double>();               
                _state.Tokens = new Dictionary<string, double>();
                FSM.Add(currentState, _state);
                FSM[currentState].Transition.Add(state, 1);
            }
        }
    }
}

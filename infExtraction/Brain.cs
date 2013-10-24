using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace infExtraction
{
    public class Brain
    {
        private string path = "";

        public Brain(string _path)
        {
            path = _path;
        }

        Dictionary<string, HMM> brain = new Dictionary<string, HMM>();

        public void Train()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            XmlNodeList list = doc.SelectNodes("/Brain/HMM");

            for (int i = 0; i < list.Count; i++)
            {
                string tag = list[i].Attributes["Tag"].InnerText;
                brain.Add(tag, Process(list[i]));
            }
        }

        public HMM.Result Tag(string input)
        {
            HMM.Result[] results = new HMM.Result[brain.Count];
            int i = 0;
            foreach (var v in brain)
            {
                results[i]=  v.Value.Tag(input);
                i++;
            }

            return Collapse(results);
        }

        public Dictionary<string, string[]> Extract(HMM.Result r, string q)
        {
            string[] tags = q.Split(',');
            Dictionary<string, string[]> d = new Dictionary<string, string[]>();
                
            for (int i = 0; i < tags.Length; i++)
            {
                tags[i] = tags[i].Trim();
                d.Add(tags[i], Grab(tags[i], r));
            }
            return d;
        }

        public Dictionary<string, string[]> Extract(string input, string q)
        {
            string[] tags = q.Split(',');
            Dictionary<string, string[]> d = new Dictionary<string, string[]>();
            HMM.Result r = Tag(input);
            for (int i = 0; i < tags.Length; i++)
            {
                tags[i] = tags[i].Trim();
                d.Add(tags[i], Grab(tags[i],r));
            }
            return d;
        }

        public void Save()
        {
           //Save the "brain"
        }

        private string[] Grab(string tag, HMM.Result r)
        {
            tag = tag.ToLower();
            string[] s = new string[0];
            for (int i = 1; i < r.States.Length-1; i++)
            {
                if (r.States[i] == tag)
                {
                    Array.Resize<string>(ref s, s.Length + 1);
                    s[s.Length - 1] = r.Tokens[i - 1];
                }
            }
            return s;
        }

        
        private HMM.Result Collapse(HMM.Result[] results)
        {
            HMM.Result r = new HMM.Result();
            r.States = new string[results[0].Tokens.Length +2];
            r.States[0] = "start";

            for (int i = 0; i < results.Length; i++)
            {
                if (results[i].Probability > 0.1)
                {
                    for (int j = 0; j < results[i].States.Length; j++)
                    {
                        if(results[i].States[j]!="bg")
                        {
                            //Array.Resize<string>(ref r.States, r.States.Length + 1);
                            r.States[j] = results[i].States[j];
                        }
                    }
                }
            }

            for (int i = 0; i < r.States.Length; i++)
            {
                if (r.States[i] == null)
                    r.States[i] = "bg";
            }

            r.States[r.States.Length - 1] = "end";
            r.Tokens = results[0].Tokens;
            return r;
        }

        private HMM Process(XmlNode node)
        {
            XmlNodeList model = node.SelectNodes("Model");
            
            Dictionary<string, string[]>d = new Dictionary<string, string[]>();
            d = new Dictionary<string, string[]>();

            for (int i = 0; i < model.Count; i++)
            {
                XmlNodeList list = model[i].SelectNodes("Link");
                string[] s = new string[list.Count];
                
                for (int j = 0; j < list.Count; j++)
                {
                    s[j] = list[j].InnerText;
                }

                d.Add(model[i].Attributes["State"].InnerText, s);   
            }

            
            XmlNodeList trainingData = node.SelectNodes("TrainingData/Data");
            string[] ss = new string[trainingData.Count];
            for (int i = 0; i < trainingData.Count; i++)
            {
                ss[i] = trainingData[i].InnerText;
            }

            return new HMM(d, ss);
        }
    }
}

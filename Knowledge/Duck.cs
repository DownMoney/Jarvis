using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Net;

namespace Knowledge
{
    public class Duck
    {
        private string endpoint = "http://api.duckduckgo.com/?format=xml&pretty=1&q=";

        public struct LinkText
        {
            public string Value;
            public string Url;
        }

        public struct Result
        {
            public LinkText Abstract;
            public LinkText[] Related;
        }

        public Duck()
        {

        }

        public Result Query(string q)
        {
            WebClient client = new WebClient();
            client.Headers.Add("User-Agent: Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.31 (KHTML, like Gecko) Chrome/26.0.1410.64 Safari/537.31");
            string res = client.DownloadString(endpoint + q.Replace(" ", "+"));

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(res);

            Result r = new Result();

            try
            {
                r.Abstract = new LinkText();
                r.Abstract.Value = doc.SelectSingleNode("/DuckDuckGoResponse/AbstractText").InnerText;
                r.Abstract.Url = doc.SelectSingleNode("/DuckDuckGoResponse/AbstractURL").InnerText;
                //r.Abstract = l;
            }
            catch
            {

            }

            try
            {
                XmlNodeList list = doc.SelectNodes("/DuckDuckGoResponse/RelatedTopics/RelatedTopic");
                LinkText[] results = new LinkText[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    results[i] = new LinkText();
                    results[i].Url = list[i].InnerText;
                    results[i].Value = list[i]["Text"].InnerText;
                }

                r.Related = results;
            }
            catch
            {

            }

            return r;
        }


    }
}

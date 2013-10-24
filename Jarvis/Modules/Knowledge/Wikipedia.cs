using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Xml;
using System.Text.RegularExpressions;

namespace Jarvis.Modules.Knowledge
{
    public class Wikipedia
    {

        private string endpoint = "http://en.wikipedia.org/w/api.php?action=query&prop=revisions&format=xml&rvprop=content&rvlimit=1&redirects=&titles=";

        public Wikipedia()
        {

        }


        public string Search(string query)
        {
            string url = endpoint + query.Replace(" ", "+");
            XmlDocument doc = GetDocument(url);
            XmlNode rev = doc.SelectSingleNode("/api/query/pages/page/revisions/rev");
            string ab = "Sorry I couldn't find enough information";

            if(rev!=null)
                ab = GetAbstract(rev);

            return ab;
        }

        private string GetAbstract(XmlNode rev)
        {
            string data = rev.InnerText;
           
            int start = data.IndexOf("'''");
            int end = data.IndexOf("==");

            string ab = data.Substring(start, end - start);

            ab = Cleanup(ab);

            return ab;
        }

        private string Cleanup(string data)
        {
            data = data.Replace("'''", "");
           

            data = Regex.Replace(data, @"{{([^}]+)}}", "");

            data = Regex.Replace(data, @"<([^>]+)>", "");
            data = Regex.Replace(data, @"\[\[((\w|\s|'|\(|\))+\|((\w|\s|'|\(|\))+))\]\]", "$3");
            data = Regex.Replace(data, @"(http|ftp|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?", "");
            data = data.Replace("}", "");
            data = data.Replace("{", "");

            data = data.Replace("[[", "");
            data = data.Replace("]]", "");

            data = Regex.Replace(data, @"\[([^\]]+)\]", "");
           
            return data;
        }

        private XmlDocument GetDocument(string url)
        {
            WebClient client = new WebClient();
            string data = client.DownloadString(url);

            XmlDocument doc = new XmlDocument();
            
            doc.LoadXml(data);

            return doc;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Xml;

namespace Jarvis.Modules.Movies
{
    public class IMDB
    {
        private string Endpoint = "http://imdbapi.org/";



        public IMDB()
        {

        }

        public void GetInfo(string[] names)
        {

            XmlDocument doc = new XmlDocument();
            XmlElement root = (XmlElement)(doc.AppendChild(doc.CreateNode(XmlNodeType.Element, "Films", "")));

            for (int i = 0; i < names.Length; i++)
            {
                string[] l = names[i].Split('\\');
                string name = l[l.Length - 1];
                string[] j = name.Split('.');
                name = name.Replace("."+j[j.Length - 1], "");
                string xml = GetXML(name);

                XmlDocument temp = new XmlDocument();
                temp.LoadXml(xml);

                

                XmlNode node = temp.SelectSingleNode("//item");
                if (node != null)
                {
                    XmlElement prov = (XmlElement)(root.AppendChild(doc.CreateNode(XmlNodeType.Element, "Film", "")));
                    prov.InnerXml = node.InnerXml+"<path>"+names[i].Replace("&","&amp;")+"</path>";
                }
            }

            doc.Save("Modules/Movies/Films.xml");

        }

        private string GetXML(string name)
        {
            WebClient client = new WebClient();

            return client.DownloadString(Endpoint + "?type=xml&q=" + name);
        }
    }
}

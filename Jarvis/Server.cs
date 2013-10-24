using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml;

namespace Jarvis
{
    public delegate void MessageReceived(string input, string ruleName);
    public delegate void Authorized();

    public class Server
    {
        private int port = 38876;
        private TcpListener server;
        private Thread serverThread;
        public event MessageReceived OnMessageReceived;
        private Dictionary<string, string> AuthorizedApplications = new Dictionary<string, string>();
        private Settings settings = new Settings();
        private Speech speech = new Speech();
        public event Authorized OnAuthorized;
        private NetworkStream curStream;
        private bool connected = false;
        private string moduleName = "";
        

        public Server(int _port=38876, string _moduleName="")
        {
            port= _port;
            moduleName = _moduleName;
            server = new TcpListener(IPAddress.Any, port);
            serverThread = new Thread(new ThreadStart(ListenForClients));
            serverThread.Start();
        }

        private void ListenForClients()
        {
            server.Start();
           
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleConnection));
                clientThread.Start(client);
            }
        }

        protected virtual void onAuthorized()
        {
            OnAuthorized();
        }

        private void HandleConnection(object o)
        {
            
            TcpClient client = (TcpClient)o;
            NetworkStream stream = client.GetStream();
            curStream = stream;
            byte[] message = new byte[4096];
            int bytesRead;

            while (true)
            {
                bytesRead = 0;

                try
                {
      //blocks until a client sends a message
                    bytesRead = stream.Read(message, 0, message.Length);
                }
                catch
                {
      //a socket error has occured
                    break;
                }

                if (bytesRead == 0)
                {
      //the client has disconnected from the server
                    break;
                }

    //message has successfully been received
                
                string Message = System.Text.Encoding.Unicode.GetString(message, 0, message.Length);
                ParseMessage(Message, stream);
                System.Diagnostics.Debug.WriteLine(Message);
            }

            client.Close();
        }    
    
        private void ParseMessage(string message, NetworkStream stream)
        {
            try
            {
                string[] parts = message.Split('\n');
                int len = Convert.ToInt32(parts[0]);
                string rest = "";
                for (int i = 1; i < parts.Length; i++)
                    rest += parts[i] + "\n";

                rest = rest.Substring(0, len);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(rest);
                string header = doc.SelectSingleNode("/JarvisProtocol/Header").InnerText;
                switch (header)
                {
                    case "POST /command":
                        RunCommand(doc);
                        break;
                    case "POST /authorize":
                        Authorize(doc, stream);
                        break;
                    case "GET /settings":
                        GiveSettings(doc, stream);
                        break;
                    case "POST /speak":
                        Speak(doc);
                        break;
                }

            }
            catch (Exception ex)
            {
                ;
            }
        }

        private void Speak(XmlDocument doc)
        {
            try
            {
                string modulename = CheckAuth(doc);
                if (modulename != "")
                {
                    XmlNode node = doc.SelectSingleNode("/JarvisProtocol/Data");
                    speech.Speak(node.InnerText);
                }
            }
            catch
            {

            }
        }

        private string CheckAuth(XmlDocument doc)
        {
            string moduleName = doc.SelectSingleNode("/JarvisProtocol/ModuleName").InnerText;
            string token = doc.SelectSingleNode("/JarvisProtocol/Token").InnerText;

            if (AuthorizedApplications.ContainsKey(moduleName) && AuthorizedApplications[moduleName] == token)
                return moduleName;
            else
                return "";
        }

        private void GiveSettings(XmlDocument doc, NetworkStream stream)
        {
            try
            {
                string modulename = CheckAuth(doc);
                if (modulename != "")
                {
                    Dictionary<string, string> d = new Dictionary<string, string>();
                    XmlNodeList list = doc.SelectNodes("/JarvisProtocol/ReqSetting");

                    d.Add("ModuleName", modulename);
                    d.Add("Header", "POST /settings");
                    for (int i = 0; i < list.Count; i++)
                    {
                        string s = list[i].InnerText;
                        string res = settings.GetSetting(s);
                        d.Add("ResSetting|"+s, s+"|"+res);
                    }

                    string message = CreateMessage(d);
                    Write(message, stream);
                }
            }
            catch
            {
                ;
            }
        }

        private string CreateMessage(Dictionary<string,string> d)
        {
            string mes = "<JarvisProtocol>";
            foreach (var v in d)
            {
                string key = v.Key.Split('|')[0];
                mes += "<" + key + ">" + v.Value + "</" + key + ">";
            }
            mes += "</JarvisProtocol>";
            return mes;
        }

        public void ExecuteCommand(string input, string ruleName)
        {
            //while (!connected)
            //    ;

            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add("Header", "POST /command");
            d.Add("ModuleName", moduleName);
            d.Add("RuleName", ruleName);
            d.Add("Data", input);
            string message = CreateMessage(d);

            Write(message, curStream);
        }

        private void RunCommand(XmlDocument doc)
        {
            string modulename = CheckAuth(doc);

            if (modulename!="")
            {
                string input = doc.SelectSingleNode("/JarvisProtocol/Data").InnerText;
                string ruleName = doc.SelectSingleNode("/JarvisProtocol/RuleName").InnerText;
                messageReceived(input, ruleName);
            }
        }

        private void Write(string message, NetworkStream stream)
        {
            if (stream != null)
            {
                System.Threading.Thread.Sleep(50);
                int len = message.Length;
                message = len.ToString() + "\n" + message;
                byte[] mes = System.Text.Encoding.Unicode.GetBytes(message);
                stream.Write(mes, 0, mes.Length);
                stream.Flush();
            }
        }

        private void Authorize(XmlDocument doc, NetworkStream stream)
        {
            try
            {
                string apikey = doc.SelectSingleNode("/JarvisProtocol/ApiKey").InnerText;
                string apisecret = doc.SelectSingleNode("/JarvisProtocol/ApiSecret").InnerText;
                string ModuleName = doc.SelectSingleNode("/JarvisProtocol/ModuleName").InnerText;
                XmlDocument document = new XmlDocument();
                document.Load("Settings/Applications.xml");
           
                string name = document.SelectSingleNode("/Applications/Application[Key=\"" + apikey + "\"]/Name").InnerText;
                string secret = CalculateSecret(name, apikey);

                if (secret == apisecret)
                {                    
                    string token = CreateToken(apisecret);
                    Dictionary<string, string> d = new Dictionary<string, string>();
                    d.Add("Header", "POST /authorize");
                    d.Add("ModuleName", ModuleName);
                    d.Add("Token", token);

                    
                   

                    string message = CreateMessage(d);
                    if (AuthorizedApplications.ContainsKey(ModuleName))
                        AuthorizedApplications[ModuleName] = token;
                    else
                        AuthorizedApplications.Add(ModuleName, token);
                    Write(message, stream);
                    onAuthorized();
                    connected = true;
                }
          
            }
            catch
            {
                ;
            }
        }

        private string CreateToken(string apisecret)
        {
            long ticks = DateTime.Now.Ticks;
            int hash = 0;

            for (int i = 0; i < apisecret.Length; i++)
            {
                hash += (33 * (int)apisecret[i] + hash);
            }

            ticks += hash;
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] b = BitConverter.GetBytes(ticks);
            byte[] computed = md5.ComputeHash(b, 0, b.Length);
            return ConvertMD5ToString(computed);
        }

        private string ConvertMD5ToString(byte[] hash)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        private string MD5Hash(string value)
        {
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(value);
            byte[] hash = System.Security.Cryptography.MD5.Create().ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        private string CalculateSecret(string name, string apikey)
        {
            string secret = "";
            for (int i = 0; i < apikey.Length; i++)
            {
                for (int j = 0; j < name.Length; j++)
                {
                    secret += (char)((int)apikey[i] + (int)name[j]);
                }
            }

            return MD5Hash(secret);
        }

        protected virtual void messageReceived(string input, string ruleName)
        {
            try
            {
                OnMessageReceived(input, ruleName);
            }
            catch (Exception ex)
            {
                ;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Xml;
using System.Threading;

namespace JarvisAPI
{
	public delegate void MessageReceived(string message, string ruleName);
	public delegate void SettingsReceived(Dictionary<string,string> settings);
  
	public class API
	{
		private TcpClient client;
		private NetworkStream stream;
		private string ModuleName = "";
		public event MessageReceived OnMessageReceived;
		public event SettingsReceived OnSettingsReceived;
		private string token = "";
        private int port = 38876;

		public API(string moduleName, int _port=38876)
		{
            port = _port;
			ModuleName = moduleName;
			Connect();
		}

		private void Connect()
		{
			IPEndPoint ip = new IPEndPoint(IPAddress.Loopback, port);
			client = new TcpClient();
			try
			{
				client.Connect(ip);
				stream = client.GetStream();

				Thread clientThread = new Thread(new ThreadStart(HandleConnection));
				clientThread.IsBackground = true;
				clientThread.Start();
			}
			catch
			{
				Console.WriteLine("Jarvis is not running! Press enter to reconnect.");
				Console.ReadLine();
				Connect();
			}
			
		}

		private void HandleConnection()
		{
            try
            {
                if (stream != null)
                {
                    while (client.Connected)
                    {
                        try
                        {
                            byte[] mes = new byte[4096];
                            stream.Read(mes, 0, mes.Length);

                            string message = System.Text.Encoding.Unicode.GetString(mes, 0, mes.Length);
                            ParseMessage(message);
                        }
                        catch
                        {
                            ;
                        }
                    }
                }
            }
            catch
            {
                ;
            }
		}

		public void Authorize(string apikey, string apisecret)
		{
			Dictionary<string, string> d = new Dictionary<string, string>();
			d.Add("Header", "POST /authorize");
			d.Add("ModuleName", ModuleName);
			d.Add("ApiKey", apikey);
			d.Add("ApiSecret", apisecret);
			string message = CreateMessage(d);
			Write(message);

			while (token == "")
				;
		}

		private void Write(string message)
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

		public void RunCommand(string input, string ruleName)
		{
			Dictionary<string, string> d = new Dictionary<string, string>();
			d.Add("Header", "POST /command");
			d.Add("Token", token);
			d.Add("ModuleName", ModuleName);
			d.Add("RuleName", ruleName);
			d.Add("Data", input);
			string message = CreateMessage(d);

			Write(message);
		}

		private string CreateMessage(Dictionary<string, string> d)
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

		public void GetSettings(string[] settings)
		{
			Dictionary<string, string> d = new Dictionary<string, string>();
			d.Add("ModuleName", ModuleName);
			d.Add("Token", token);
			d.Add("Header", "GET /settings");

			for (int i = 0; i < settings.Length; i++)
			{
				d.Add("ReqSetting|"+settings[i], settings[i]);
			}
			string message = CreateMessage(d);
			Write(message);
		}

		public void Speak(string text)
		{            
			Dictionary<string, string> d = new Dictionary<string, string>();
			d.Add("ModuleName", ModuleName);
			d.Add("Token", token);
			d.Add("Header", "POST /speak");
			d.Add("Data", text);

			string message = CreateMessage(d);
			Write(message);
		}

		private void tokenReceived(XmlDocument doc)
		{
			try
			{
				if (token=="")
					token = doc.SelectSingleNode("/JarvisProtocol/Token").InnerText;
				
			}
			catch
			{
				;
			}
		}

		protected virtual void settingsReceived(XmlDocument doc)
		{
			try
			{
				Dictionary<string, string> settings = new Dictionary<string, string>();
				XmlNodeList list = doc.SelectNodes("/JarvisProtocol/ResSetting");
				for (int i = 0; i < list.Count; i++)
				{
					string[] s = list[i].InnerText.Split('|');
					settings.Add(s[0], s[1]);
				}
					OnSettingsReceived(settings);
			}
			catch
			{

			}
		}

		private void ParseMessage(string message)
		{
           // Console.WriteLine(message);
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
				if (ModuleName == doc.SelectSingleNode("/JarvisProtocol/ModuleName").InnerText)
				{
					string header = doc.SelectSingleNode("/JarvisProtocol/Header").InnerText;
					switch (header)
					{
						case "POST /authorize":
							tokenReceived(doc);
							break;
						case "POST /settings":
							settingsReceived(doc);
							break;
						case "POST /command":
							ExecuteCommand(doc);
							break;                         

					}
					/*string input = doc.SelectSingleNode("/JarvisProtocol/Data").InnerText;
					string ruleName = doc.SelectSingleNode("/JarvisProtocol/RuleName").InnerText;
					messageReceived(input, ruleName);*/
				}
			}
			catch
			{
				;
			}
		}

		private void ExecuteCommand(XmlDocument doc)
		{           
			try
			{
				string data = doc.SelectSingleNode("/JarvisProtocol/Data").InnerText;
				string ruleName = doc.SelectSingleNode("/JarvisProtocol/RuleName").InnerText;
				messageReceived(data, ruleName);
			}
			catch
			{

			}
		}

		protected virtual void messageReceived(string message, string ruleName)
		{
			try
			{
				OnMessageReceived(message, ruleName);

			}
			catch
			{
				;
			}
		}
	}
}

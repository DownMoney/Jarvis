using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JarvisAPI;

namespace APITest
{
    class Program
    {
        static void Main(string[] args)
        {
            API api = null;

            for (int i = 0; i < args.Length; i += 2)
            {

                if (args[i] == "-port")
                {
                    api = new API("test", Convert.ToInt32(args[i + 1]));
                    Console.WriteLine(args[i + 1]);
                }
                
            }

            if(api == null)
               api = new API("test");

            
            string key = GenerateApiKey("test");
            string secret = CalculateSecret("test", key);
           // Console.WriteLine(secret);
            api.Authorize(key, secret);
            
           // api.Speak("Hi my name is mr awesome");
             api.OnMessageReceived += api_OnMessageReceived;
            //api.RunCommand("i would like to watch a film", "execute|movies");
            api.RunCommand("the temperature is 18 degrees", "speak");
            api.OnSettingsReceived += api_OnSettingsReceived;
           // api.GetSettings(new string[] { "programName", "userName" });           
            
          //  Console.ReadLine();
        }

        static void api_OnSettingsReceived(Dictionary<string, string> settings)
        {
            foreach (var v in settings)
                Console.WriteLine(v.Key + " - " + v.Value);
        }

        static void api_OnMessageReceived(string message, string ruleName)
        {
            Console.WriteLine(message);
        }

        static string GenerateApiKey(string name)
        {
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(name);
            byte[] hash = System.Security.Cryptography.MD5.Create().ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        private static string CalculateSecret(string name, string apikey)
        {
            string secret = "";
            for (int i = 0; i < apikey.Length; i++)
            {
                for (int j = 0; j < name.Length; j++)
                {
                    secret += (char)((int)apikey[i] + (int)name[j]);
                }
            }

            return GenerateApiKey(secret);
        }
    }
}

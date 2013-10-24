using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Jarvis.Modules.Home
{
    public class Weather:Module
    {
        public event ModuleMessageReceived OnMessageReceived;
        const string key = "";
        private Speech speech = new Speech();

        private struct Conditions
        {
            public double temp;
            public double feelsLike;
            public string city;
            public string windDirection;
            public string windType;
            public string weatherType;
        }

        public Weather()
        {

        }

        public string BehaviourScript()
        {
            return "";
        }

        public string RecoScript()
        {
            return "";
        }

        public Recognition.Response Execute(string input, string ruleName)
        {
            string[] s = ruleName.Split('|');
            speech.Respond(input, s[s.Length - 1]);
            switch (s[s.Length - 1])
            {
                
            }
            return GetWeather();
        }

        private Recognition.Response GetWeather()
        {
            Recognition.Response res = new Recognition.Response();
            Conditions con = getConditions();
            string text ="It is "+con.weatherType + ". the temperature outside is " + con.temp.ToString() + " degrees";
            if (con.temp != con.feelsLike)
                text += ", but it feels like " + con.feelsLike.ToString() + " degrees due to the wind " + con.windType;
            else
                text += ", there is wind " + con.windType;

            res.text = text;
            

            return res;
        }

        public string[] GetOptions(string token)
        {
            return new string[0];
        }

        public void SendData(string data)
        {

        }

        private Conditions getConditions()
        {
            string address = "http://api.wunderground.com/api/"+key+"/conditions/q/autoip.json";
           //string address = "http://api.wunderground.com/api/" + key + "/conditions/q/UK/Edinburgh.json";
            WebClient client = new WebClient();
            client.Headers.Add("User-Agent: Jarvis");
            string res = client.DownloadString(address);

            JObject j = JObject.Parse(res);

            Conditions conditions = new Conditions();

            conditions.city = j.Last.First["display_location"]["city"].ToString();
            conditions.temp = Convert.ToDouble(j.Last.First["temp_c"].ToString());
            conditions.feelsLike = Convert.ToDouble(j.Last.First["feelslike_c"].ToString());
            conditions.weatherType = j.Last.First["weather"].ToString();
            conditions.windType = j.Last.First["wind_string"].ToString();
            conditions.windDirection = j.Last.First["wind_dir"].ToString();

            return conditions;
        }
    }
}

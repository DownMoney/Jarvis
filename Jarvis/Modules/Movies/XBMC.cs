using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Speech.Recognition;

namespace Jarvis.Modules.Movies
{
    public class XBMC
    {
        private TcpClient client;
        private NetworkStream stream;
        private byte[] buffer;
        private Dictionary<string, int> Movies = new Dictionary<string, int>();
        private Dictionary<string, int> TvShows = new Dictionary<string, int>();
        private Dictionary<string, int> Seasons = new Dictionary<string, int>();
        private Dictionary<string, int> Episodes = new Dictionary<string, int>();
        private int TVShowID = -1;
        public bool Connected = false;
        private string IP = "";
        private int tries = 0;
        private const int maxTries = 10;
        private LatestTV[] latestTv = new LatestTV[0];

        public struct Movie
        {
            public string name;
            public int id;
        }

        public struct LatestTV
        {
            public string name;
            public int season;
            public string episodeName;
            public int episode;
        }

        public XBMC(string ip, bool connect= false)
        {

            IP = ip;
            if (connect)
            {
                try
                {
                    TryConnect(ip);
                }
                catch
                {
                    ;
                }
            }

        }

        public string BehaviourScript()
        {
            return "";
        }

        public Recognition.Response Execute(string input, string ruleName)
        {
            return new Recognition.Response();
        }

        public string[] GetOptions(string token)
        {
            if (Connected)
            {
                Dictionary<string, int> m = getMovies();
                string[] s = new string[0];
                foreach (var v in m)
                {
                    Array.Resize<string>(ref s, s.Length + 1);
                    s[s.Length - 1] = v.Key;
                }
                return s;
            }
            return new string[0]; 
        }

        public LatestTV[] GetLatestTV()
        {

            LatestTV[] l = new LatestTV[0];
            TvShows = getTVShows();

            foreach (var v in TvShows)
            {
                Array.Resize<LatestTV>(ref l, l.Length + 1);
                l[l.Length - 1] = new LatestTV();
                l[l.Length - 1].name = v.Key;
                Seasons = getSeasons(v.Key);
                foreach (var w in Seasons)
                {
                    int mSe = 0;
                    int mEp = 0;
                    int i = Convert.ToInt32(w.Key.Substring(7));
                    if (i > l[l.Length - 1].season)
                    {
                        l[l.Length - 1].season = i;
                        Episodes = getEpisodes2(w.Key);

                        l[l.Length - 1].episode = 0;
                        foreach (var x in Episodes)
                        {
                            if (x.Key != "")
                            {
                                string s = x.Key.Split(' ')[0];
                                string[] h = s.Split('x');
                                int se = Convert.ToInt32(h[0]);
                                int ep = Convert.ToInt32(h[1].Substring(0, h[1].Length - 1));
                                if (se >= mSe && ep > mEp)
                                {
                                    mSe = se;
                                    mEp = ep;
                                    l[l.Length - 1].episodeName = x.Key.Substring(x.Key.IndexOf(" ") + 1);
                                    l[l.Length - 1].episode = ep;

                                }
                            }

                        }

                    }
                }
            }
            latestTv = l;
            return l;
        }

        private void TryConnect(string ip)
        {
            Settings settings = new Settings();
            string path = settings.GetSetting("xbmcPath");
            try
            {
                tries++;
                Connect(ip);
            }
            catch
            {
                if (System.IO.File.Exists(path))
                {
                    System.Diagnostics.Process.Start(path);
                    System.Threading.Thread.Sleep(500);
                    Console.WriteLine("XBMC is not running!");
                    Connect(ip);
                }
                else
                    tries = maxTries;
                Console.WriteLine("XBMC is not running!");
            }
        }


        public void Connect(string ip)
        {
            client = new TcpClient(ip, 9090);
            if (client.Connected)
            {
                stream = client.GetStream();
                client.ReceiveBufferSize = 3145728;
                buffer = new byte[client.ReceiveBufferSize];
                Connected = true;
            }
        }

        private string read()
        {
            if (Connected)
            {
                if (stream != null)
                {
                    int lData = stream.Read(buffer, 0, client.ReceiveBufferSize);
                    string myString = Encoding.ASCII.GetString(buffer).Replace("\0", "");

                    return myString;
                }
            }
            else
            {
                if (maxTries > tries)
                {
                    TryConnect(IP);
                    return read();
                }
            }
            return "";
        }

        private string write(string text)
        {
            if (Connected)
            {
                text = "{\"id\":1,\"jsonrpc\":\"2.0\",\"method\":\"" + text + "\"}";
                if (stream != null)
                {
                    stream.Write(Encoding.ASCII.GetBytes(text.ToCharArray()), 0, text.Length);
                    return read();
                }
                else
                    return text;
            }
            else
            {
                if (tries < maxTries)
                {
                    TryConnect(IP);
                    return write(text);
                }
                else
                    return "";
            }
        }

        private string writeItem(string text, Dictionary<string, string> param)
        {
            if (Connected)
            {
                string p = "{";
                foreach (var a in param)
                {
                    try
                    {
                        int i = Convert.ToInt32(a.Value);
                        p += "\"" + a.Key + "\":" + i.ToString() + ",";
                    }
                    catch
                    {
                        p += "\"" + a.Key + "\":\"" + a.Value + "\",";
                    }
                }

                p = p.Substring(0, p.Length - 1);
                p += "}";
                text = "{\"id\":1,\"jsonrpc\":\"2.0\",\"method\":\"" + text + "\", \"params\": {\"item\":" + p + "}}";
                stream.Write(Encoding.ASCII.GetBytes(text.ToCharArray()), 0, text.Length);
                return read();
            }
            else
            {
                if (tries < maxTries)
                {
                    TryConnect(IP);
                    return writeItem(text, param);
                }
                else
                    return "";
            }
        }

        private string write(string text, Dictionary<string, string> param)
        {
            string p = "{";
            foreach (var a in param)
            {
                try
                {
                    bool b;
                    if (Boolean.TryParse(a.Value, out b))
                        p += "\"" + a.Key + "\":" + a.Value + ",";
                    else
                    {
                        int i = Convert.ToInt32(a.Value);
                        p += "\"" + a.Key + "\":" + i.ToString() + ",";
                    }
                }
                catch
                {
                    p += "\"" + a.Key + "\":\"" + a.Value + "\",";
                }
            }

            p = p.Substring(0, p.Length - 1);
            p += "}";
            text = "{\"id\":1,\"jsonrpc\":\"2.0\",\"method\":\"" + text + "\", \"params\": " + p + "}";
            stream.Write(Encoding.ASCII.GetBytes(text.ToCharArray()), 0, text.Length);
            return read();
        }

        public Dictionary<string, int> getMovies()
        {
            if (Movies.Count <= 0)
            {
                try
                {
                    Dictionary<string, int> movies = new Dictionary<string, int>();

                    write("VideoLibrary.GetMovies");
                    string res = write("VideoLibrary.GetMovies");

                    JObject j = JObject.Parse(res);

                    foreach (JToken o in j["result"]["movies"])
                    {
                        try
                        {
                            movies.Add(o["label"].ToString(), Convert.ToInt32(o["movieid"].ToString()));
                        }
                        catch { ;}
                    }
                    Movies = movies;
                }
                catch
                {
                    return getMovies();
                }
            }
            return Movies;
        }

        public Dictionary<string, int> getTVShows()
        {
            if (TvShows.Count <= 0)
            {
                try
                {
                    Dictionary<string, int> shows = new Dictionary<string, int>();

                    string res = write("VideoLibrary.GetTVShows");

                    JObject j = JObject.Parse(res);

                    foreach (JToken o in j["result"]["tvshows"])
                    {
                        try
                        {
                            shows.Add(o["label"].ToString(), Convert.ToInt32(o["tvshowid"].ToString()));
                        }
                        catch { ;}
                    }
                    TvShows = shows;
                }
                catch
                {
                    return getTVShows();
                }
            }
            return TvShows;
        }

        public void playMovie(string movie)
        {
            if (Movies.ContainsKey(movie))
            {
                int i = Movies[movie];
                Dictionary<string, string> d = new Dictionary<string, string>();
                d.Add("movieid", i.ToString());
                writeItem("Player.Open", d);
            }
        }

        public void playPause()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add("playerid", "1");
            Console.WriteLine(write("Player.PlayPause", d));
        }

        public void playerStop()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add("playerid", "1");
            Console.WriteLine(write("Player.Stop", d));
        }

        public void navigate(string where)
        {
            write("Input." + where);
        }

        public void update()
        {
            Movies = new Dictionary<string, int>();
            write("VideoLibrary.Scan");
        }

        public Dictionary<string, int> getSeasons(string show)
        {
            int tid = 0;
            if (TvShows.ContainsKey(show))
                tid = TvShows[show];
            TVShowID = tid;
            try
            {
                Dictionary<string, int> shows = new Dictionary<string, int>();
                Dictionary<string, string> d = new Dictionary<string, string>();
                d.Add("tvshowid", tid.ToString());
                write("VideoLibrary.GetSeasons", d);
                string res = write("VideoLibrary.GetSeasons", d);

                JObject j = JObject.Parse(res);

                foreach (JToken o in j["result"]["seasons"])
                {
                    string[] s = o.First.ToString().Split(':');

                    string name = s[1].Replace("\"", "").Trim();

                    string id = name.Split(' ')[1].Trim();
                    try
                    {
                        shows.Add(name, Convert.ToInt32(id));
                    }
                    catch { ;}
                }
                Seasons = shows;
            }
            catch
            {
                return getSeasons(show);
            }

            return Seasons;
        }




        public Dictionary<string, int> getEpisodes(string season)
        {
            Regex getName = new Regex(@"\d?\dx\d\d\.\s(.+\s*)", RegexOptions.Compiled);
            int sid = 0;
            if (Seasons.ContainsKey(season))
                sid = Seasons[season];

            try
            {
                Dictionary<string, int> episodes = new Dictionary<string, int>();
                Dictionary<string, string> d = new Dictionary<string, string>();
                d.Add("tvshowid", TVShowID.ToString());
                d.Add("season", sid.ToString());
                write("VideoLibrary.GetEpisodes", d);
                string res = write("VideoLibrary.GetEpisodes", d);

                JObject j = JObject.Parse(res);

                foreach (JToken o in j["result"]["episodes"])
                {
                    try
                    {
                        int pos = o["label"].ToString().IndexOf('.');
                        episodes.Add(o["label"].ToString().Substring(pos + 2), Convert.ToInt32(o["episodeid"].ToString()));
                    }
                    catch { ;}
                }
                Episodes = episodes;
            }
            catch
            {
                return getEpisodes(season);
            }

            return Episodes;
        }

        public Dictionary<string, int> getEpisodes2(string season)
        {
            Regex getName = new Regex(@"\d?\dx\d\d\.\s(.+\s*)", RegexOptions.Compiled);
            int sid = 0;
            if (Seasons.ContainsKey(season))
                sid = Seasons[season];

            try
            {
                Dictionary<string, int> episodes = new Dictionary<string, int>();
                Dictionary<string, string> d = new Dictionary<string, string>();
                d.Add("tvshowid", TVShowID.ToString());
                d.Add("season", sid.ToString());
                write("VideoLibrary.GetEpisodes", d);
                string res = write("VideoLibrary.GetEpisodes", d);

                JObject j = JObject.Parse(res);

                foreach (JToken o in j["result"]["episodes"])
                {
                    try
                    {
                        int pos = o["label"].ToString().IndexOf('.');
                        episodes.Add(o["label"].ToString(), Convert.ToInt32(o["episodeid"].ToString()));
                    }
                    catch { ;}
                }
                Episodes = episodes;
            }
            catch
            {
                return getEpisodes2(season);
            }

            return Episodes;
        }

        public void playEpisode(string episode)
        {
            int i = Episodes[episode];
            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add("episodeid", i.ToString());
            writeItem("Player.Open", d);
        }


        public void disableSub()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add("playerid", "1");
            d.Add("subtitle", "0");
            d.Add("enable", "false");
            string s = write("Player.SetSubtitle", d);
        }

    }
}

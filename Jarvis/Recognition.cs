using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Recognition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.Threading;

namespace Jarvis
{
    public delegate void Recognized(string text, bool show);
    public delegate void ChangeGrid(Grid g);

    public class Recognition
    {
        private static SpeechRecognitionEngine rec = new SpeechRecognitionEngine();
        private float ThreshHold = 0.70f;
        private Parser parser = new Parser();
        private bool Attention = false, Thanked =true;
        private Speech speech = new Speech();
        private Settings settings = new Settings();
        private Utilities.Utilities util = new Utilities.Utilities();
        public event Recognized OnRecognized;
        public event ChangeGrid OnGridChange;
        public event ChangeMenu OnMenuChange;
        private WaveInEvent s_WaveIn;
        private WaveFileWriter writer;// = new WaveFileWriter("test2.wav", new NAudio.Wave.WaveFormat(16000, 1));

        private Utilities.GlobalKeyboardHooks gkh = new Utilities.GlobalKeyboardHooks();

        public struct Response
        {
            public Grammar[] grammars;
            public Grid grid;
            public string text;
            public string data;
        }

        public Recognition()
        {            
            gkh.HookedKeys.Add(Keys.RShiftKey);
            gkh.KeyDown += gkh_KeyDown;
            gkh.KeyUp += gkh_KeyUp;
            parser.OnAttentionChange += parser_OnAttentionChange;
            parser.OnMenuChange += parser_OnMenuChange;
            parser.OnResponse += parser_OnResponse;
            string s = settings.GetSetting("recThreshHold");

            if (s != "")
                ThreshHold = float.Parse(s);

            
            rec.SpeechRecognitionRejected += rec_SpeechRecognitionRejected;
            rec.SpeechDetected += new EventHandler<SpeechDetectedEventArgs>(rec_SpeechDetected);
            rec.SpeechHypothesized += new EventHandler<SpeechHypothesizedEventArgs>(rec_SpeechHypothesized);
            rec.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(rec_SpeechRecognized);
            rec.RecognizeCompleted += new EventHandler<RecognizeCompletedEventArgs>(rec_RecognizeCompleted);
            /*rec.BabbleTimeout = TimeSpan.FromSeconds(2);
            rec.EndSilenceTimeout = TimeSpan.FromSeconds(1);
            rec.EndSilenceTimeoutAmbiguous = TimeSpan.FromSeconds(1.5);
            rec.InitialSilenceTimeout = TimeSpan.FromSeconds(3);*/
            rec.EndSilenceTimeout = TimeSpan.FromSeconds(1);
            rec.EndSilenceTimeoutAmbiguous = TimeSpan.FromSeconds(1);
            try
            {
                rec.SetInputToDefaultAudioDevice();
                rec.AudioLevelUpdated += rec_AudioLevelUpdated;
                LoadGrammar();
              //  rec.RecognizeAsync();
            }
            catch
            {

            }
        }
        bool started = false;
        void gkh_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (!started)
            {
                started = true;
                Attention = true;
                StartRecording();
            }
        }

        void gkh_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            started = false;
            
            System.Threading.Thread.Sleep(500);
            s_WaveIn.StopRecording();
            writer.Close();

            rec.SetInputToWaveFile("test2.wav");
            RecognitionResult res = rec.Recognize();
            rec.SetInputToNull();
            if (res!=null && res.Confidence * res.Grammar.Weight >= ThreshHold)
            {
                Analyze(res);
            }
            else
            {
                ConvertToFlac();
                System.Threading.Thread.Sleep(500);
                string s = Send("test2.flac");
            }
            Attention = false;
        }
       
        private void StartRecording()
        {       
            writer = new WaveFileWriter("test2.wav", new NAudio.Wave.WaveFormat(16000, 1));
            Thread thread = new Thread(delegate()
            {
                init();
            });

            thread.Start();
        }

        private void init()
        {

            s_WaveIn = new WaveInEvent();
            s_WaveIn.WaveFormat = new NAudio.Wave.WaveFormat(16000, 1);

            s_WaveIn.BufferMilliseconds = 1000;
            s_WaveIn.DataAvailable += new EventHandler<WaveInEventArgs>(SendCaptureSamples);

            s_WaveIn.StartRecording();
        }

        void rec_AudioLevelUpdated(object sender, AudioLevelUpdatedEventArgs e)
        {
            if (e.AudioLevel > 30)
            {
               // Attention = !Attention;
               // Reco(e.AudioLevel.ToString(), Attention);
            }
        }

        private void SendCaptureSamples(object sender, WaveInEventArgs e)
        {

            writer.Write(e.Buffer, 0, e.Buffer.Length);
            Console.WriteLine("Bytes recorded: {0}", e.BytesRecorded);
        }

        void parser_OnMenuChange(string text, string colour)
        {
            OnMenuChange(text, colour);
        }

        protected virtual void Reco(string text, bool show)
        {
            OnRecognized(text, show);
        }

        void parser_OnResponse(Recognition.Response res)
        {
            if (res.text != null && res.text != "")
                Reco(res.text, Attention);
            try
            {
                rec.UnloadAllGrammars();
            }
            catch
            { }
            Grammar[] g = res.grammars;
            if (g==null || g.Length == 0)
            {
                LoadGrammar();
            }
            else
            {
                for (int i = 0; i < g.Length; i++)
                    rec.LoadGrammar(g[i]);
            }
        }

        

        void rec_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
          /*  if (Attention)
            {
                FileStream m = File.Create("test.wav");
                e.Result.Audio.WriteToWaveStream(m);

                m.Close();
                ConvertToFlac();
                System.Threading.Thread.Sleep(500);
                string s = Send("test.flac");
            }*/
        }

        void parser_OnAttentionChange(bool b)
        {
            Attention = b;
            Thanked = true;
            Reco("", Attention);
        }

        private void rec_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            if (e.Result != null)
                ;// Console.WriteLine("Completed: " + e.Result.Text + ", " + e.Result.Confidence.ToString());
            try
            {
                if (rec.Grammars.Count > 0)
                    ;// rec.RecognizeAsync();
            }
            catch
            {
                ;
            }
        }

        public void ParseFree(string text)
        {
            parser.ParseFree(text);
        }

        protected virtual void changeGrid(Grid g)
        {
            OnGridChange(g);
        }

        public void SetAttention(bool b)
        {
            if (!b)
                rec.RecognizeAsyncCancel();
            else
                rec.RecognizeAsync();
            Attention = b;
        }

        void Analyze(RecognitionResult Result)
        {
            if (Result.Grammar.Name == "attention")
            {
                Attention = true;
                Thanked = false;
            }



            if (Attention)
            {

                if (Result.Grammar.Name == "rest")
                {
                    Attention = false;

                }

                Reco(Result.Text, Attention);
                rec.UnloadAllGrammars();
                Response res = parser.Parse(Result.Text, Result.Grammar.Name);
                if (res.grid != null)
                    changeGrid(res.grid);

                if (res.text != null && res.text != "")
                {
                    Reco(res.text, Attention);
                    speech.Speak(res.text);
                }

                Grammar[] g = res.grammars;
                if (g == null || g.Length == 0)
                {
                    LoadGrammar();
                }
                else
                {
                    for (int i = 0; i < g.Length; i++)
                        rec.LoadGrammar(g[i]);
                }

                //  rec.RecognizeAsync();
            }
        }

        void rec_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {           

            if (e.Result != null && ((e.Result.Confidence*e.Result.Grammar.Weight) >= ThreshHold))
            {
                ;// Console.WriteLine("Recognized: " + e.Result.Text + ", " + e.Result.Confidence.ToString());
                Analyze(e.Result);

             
            }
           /* else
            {
                if (Attention)
                {
                    FileStream m = File.Create("test.wav");
                    
                    e.Result.Audio.WriteToWaveStream(m);
                    
                    m.Close();
                    ConvertToFlac();
                    System.Threading.Thread.Sleep(500);
                    string s= Send("test.flac");
                }

            }*/
        }

        private void ConvertToFlac()
        {
            try
            {
                File.Delete("test2.flac");
                System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo("flac.exe", "--totally-silent --delete-input-file test2.wav");
                info.CreateNoWindow = true;
                info.RedirectStandardError = true;
                info.RedirectStandardOutput = true;
                info.UseShellExecute = false;
                System.Diagnostics.Process.Start(info);
            }
            catch
            {
                ;
            }
        }

        private string Send(string path)
        {
            try
            {
                FileStream file = File.Open(path, FileMode.Open);
                byte[] b = new byte[file.Length];
                file.Read(b, 0, b.Length);

                string Response = "";
                StreamReader StreamResponseReader = null;
                HttpWebRequest req = WebRequest.Create("http://www.google.com/speech-api/v1/recognize?xjerr=1&lang=en-US&client=chromium") as HttpWebRequest;
                req.Method = "POST";
                req.ContentType = "audio/x-flac; rate=16000;";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.31 (KHTML, like Gecko) Chrome/26.0.1410.64 Safari/537.31";
                req.ContentLength = b.Length;
                Stream requestStream = req.GetRequestStream();
                requestStream.Write(b, 0, b.Length);
                requestStream.Close();

                HttpWebResponse WebRes = (HttpWebResponse)req.GetResponse();
                StreamResponseReader = new StreamReader(WebRes.GetResponseStream(), Encoding.UTF8);
                Response = StreamResponseReader.ReadToEnd();

                file.Close();

                Console.WriteLine(Response);
                JObject o = JObject.Parse(Response);
                string s = "";

                foreach (JToken t in o["hypotheses"])
                {
                    s += (t["utterance"].ToString());

                    if (Convert.ToDouble(t["confidence"].ToString()) > ThreshHold)
                    {

                        Reco(t["utterance"].ToString(), Attention);

                        parser.ParseFree(t["utterance"].ToString());
                    }
                    else
                    {
                        Reco("Sorry didn't get that", Attention);
                    }
                }

                return s;
            }
            catch(Exception ex)
            {
                Console.WriteLine("Not connected to the internet!");
                return "";
            }
        }

        void rec_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {         
            if (e.Result != null)
            {
               // Console.WriteLine("Hypothesis: " + e.Result.Text + ", " + e.Result.Confidence.ToString());
               // for (int i = 0; i < e.Result.Alternates.Count; i++)
                 //   Console.WriteLine("Alt Hyp: " + e.Result.Alternates[i].Text + ", " + e.Result.Alternates[i].Confidence.ToString());
            }
        }

        private void LoadGrammar()
        {
            Syntax syntax = new Syntax();
          
            Grammar[] g = syntax.BuildGrammar("Settings/Recognition.xml", null);
            
            for (int i = 0; i < g.Length; i++ )
                rec.LoadGrammar(g[i]);
        }

        private void rec_SpeechDetected(object sender, SpeechDetectedEventArgs e)
        {
            if(Attention)
            Reco("Listening...", Attention);
           // Console.WriteLine("Speech detected! ");
        }

    }
}

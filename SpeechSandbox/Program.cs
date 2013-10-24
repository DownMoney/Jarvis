using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Recognition;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.Threading;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;


namespace SpeechSandbox
{
    class Program
    {
        static WaveInEvent s_WaveIn;
        static WaveFileWriter writer = new WaveFileWriter("test.wav", new NAudio.Wave.WaveFormat(16000, 1));
        [STAThread]
        static void Main(string[] args)
        {

            string s = "";
            while (s != "q")
            {
                
                Thread thread = new Thread(delegate()
                 {
                     init();
                 });

                thread.Start();
                Console.ReadLine();

                s_WaveIn.StopRecording();
                writer.Close();

                convert();

                System.Threading.Thread.Sleep(500);


                Console.WriteLine(Send("test.flac"));
                s=Console.ReadLine();
            }
        }

        private static string Send(string path)
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
            JObject o = JObject.Parse(Response);
            string s = "";
            foreach (JToken t in o["hypotheses"])
                s+=(t["utterance"].ToString());

           

            return s;
        }

        private static void convert()
        {
             File.Delete("test.flac");
            System.Diagnostics.Process.Start("flac.exe", "--totally-silent test.wav");
        }
        

        public static void init()
        {
            
            s_WaveIn = new WaveInEvent();
            s_WaveIn.WaveFormat = new NAudio.Wave.WaveFormat(16000, 1);
            
            s_WaveIn.BufferMilliseconds = 1000;
            s_WaveIn.DataAvailable += new EventHandler<WaveInEventArgs>(SendCaptureSamples);
            
            s_WaveIn.StartRecording();
        }

        static void SendCaptureSamples(object sender, WaveInEventArgs e)
        {
            
            writer.Write(e.Buffer, 0, e.Buffer.Length);
            Console.WriteLine("Bytes recorded: {0}", e.BytesRecorded);
        }
    }
}

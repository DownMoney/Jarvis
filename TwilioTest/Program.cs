using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Twilio;

namespace TwilioTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Twilio.TwilioRestClient client = new TwilioRestClient("", "");
            string s ="";
            while(s!="q")
            {
                s = Console.ReadLine();
                var msg = client.SendSmsMessage("", "", s);
                    //var call = client.InitiateOutboundCall("+441143599292", "+447946323985", "http://demo.twilio.com/docs/voice.xml");
                    //Console.WriteLine(call.Sid);
                    Console.WriteLine("Sent!");
            }
           

        }
    }
}

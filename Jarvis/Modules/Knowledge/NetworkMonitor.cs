using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Net;
using System.Threading;

namespace Jarvis.Modules.Knowledge
{
    public class NetworkMonitor
    {
        public NetworkMonitor()
        {

        }

        public bool IsPresent(string ip)
        {
            IPAddress ipa = IPAddress.Parse(ip);
            Ping ping = new Ping();
            PingReply reply = ping.Send(ipa);
            if (reply.Status == IPStatus.Success)
                return true;
            else
                return false;
        }

        public void MonitorDevice(string ip)
        {
            Timer timer = new Timer(new TimerCallback(Tick), ip, 0, 1000); 
         
        }

        private void Tick(object o)
        {
            string ip = (string)o;
            if (IsPresent(ip))
            {

            }
            else
            {

            }
        }
    }
}

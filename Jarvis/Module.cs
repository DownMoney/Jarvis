using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Recognition;

namespace Jarvis
{    
    public delegate void ModuleMessageReceived(string input, string ruleName, Module self);

    public interface Module
    {
        string BehaviourScript();

        string RecoScript();

        Recognition.Response Execute(string input, string ruleName);

        string[] GetOptions(string token);

        event ModuleMessageReceived OnMessageReceived;

        void SendData(string data);
    }
}

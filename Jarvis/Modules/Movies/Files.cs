using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Jarvis.Modules.Movies
{
    public class Files
    {
        private Settings settings = new Settings();
        private string[] ext = new string[0];
        public Files()
        {
            ext = settings.GetSetting("movieExt").Split('|');
        }

        string[] temp = new string[0];
        public string[] SearchDir(string path)
        {
            
            string[] h = Directory.EnumerateFiles(path).ToArray<string>();

            for (int i = 0; i < h.Length; i++)
            {
                if (isMovie(h[i]))
                {                    
                    Array.Resize<string>(ref temp, temp.Length + 1);
                    temp[temp.Length - 1] = h[i];
                }
            }

            string[] s = Directory.EnumerateDirectories(path).ToArray<string>();
                for (int i = 0; i < s.Length; i++)
                     SearchDir(s[i]);
            
                return temp;
                  
        }

        private bool isMovie(string input)
        {
            string[] s = input.Split('.');
            for (int i = 0; i < ext.Length; i++)
            {
                
                if ("."+s[s.Length-1].ToLower()==ext[i].ToLower())
                    return true;
            }

            return false;
        }
    }
}

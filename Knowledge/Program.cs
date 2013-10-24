using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Xml;

namespace Knowledge
{
    class Program
    {
        static void Main(string[] args)
        {
            Duck duck = new Duck();

            Duck.Result r = duck.Query(Console.ReadLine().Replace("\n","").Replace("\r",""));

            Console.WriteLine(r.Abstract.Value);
            Console.WriteLine("\nAlso Interested\n");

            for (int i = 0; i < r.Related.Length; i++)
                Console.WriteLine(r.Related[i].Value);

                Console.ReadLine();
        }
    }
}

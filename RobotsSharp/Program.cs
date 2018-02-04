using RobotsSharpParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotsSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            Robots robot = new Robots(new Uri("http://www.loot.co.za"), "Mr.Robot");
            robot.Load();
            var links = robot.GetSitemapLinks();
            foreach (var link in links)
            {
                Console.WriteLine(link);
            }
            Console.ReadLine();
        }
    }
}

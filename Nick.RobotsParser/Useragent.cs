using System;
using System.Collections.Generic;
using System.Linq;

namespace RobotsParser
{
    public class Useragent
    {
        public Useragent(string name)
        {
            Name = name;
            Allowed = new HashSet<string>();
            Disallowed = new HashSet<string>();
        }

        public string Name { get; private set; }
        public int Crawldelay { get; set; }
        public HashSet<string> Allowed { get; set; }
        public HashSet<string> Disallowed { get; set; }
        public bool IsAllowed(string path) => Allowed.Any(x => path.Contains(x));
        private bool AllDissallowed => Disallowed.Contains("/");
        public bool IsDisallowed(string path) => AllDissallowed || Disallowed.Any(x => path.Contains(x));
    }
}

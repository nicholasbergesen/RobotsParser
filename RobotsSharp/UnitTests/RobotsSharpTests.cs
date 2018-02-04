using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RobotsSharpParser;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace UnitTests
{
    [TestClass]
    public class RobotsSharpTests
    {
        [TestMethod]
        public void LoadRobotsTest()
        {
            Robots robots = new Robots("https://fake.com", "RobotsUnitTest");
            using (StreamReader sr = new StreamReader("lootRobots.txt"))
            {
                robots.Load(sr.ReadToEnd());
                Assert.AreEqual(robots.UserAgents.Count, robots.UserAgentCount());
                Assert.IsTrue(robots.IsPathDisallowed("https://loot.co.za/shop/"));
                Assert.IsTrue(robots.IsPathDisallowed("/cgi-bin/"));
                Assert.IsTrue(robots.IsPathDisallowed("/refer.html"));
                Assert.IsTrue(robots.IsPathDisallowed("/search/"));
                Assert.IsTrue(robots.IsPathDisallowed("/images/"));
                Assert.IsFalse(robots.IsPathDisallowed("/"));
                Assert.AreEqual(1, robots.GetCrawlDelay());

                Assert.IsTrue(robots.IsPathDisallowed("/", "Ahrefsbot"));
                Assert.IsFalse(robots.IsPathDisallowed("", "Ahrefsbot"));

                Assert.AreEqual("http://www.loot.co.za/index.xml.gz", robots.Sitemaps.First());
            }
        }
    }
}

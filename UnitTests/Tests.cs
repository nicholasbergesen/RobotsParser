using NUnit.Framework;
using RobotsParser;
using System.IO;
using System.Linq;

namespace UnitTests;

public class LootTest
{
    private const string TakealotRobotsTxt = @"takealotRobots.txt";
    private const string UserAgent = "FakeAgent";

    private IRobots _robots = new Robots(websiteUri: "https://loot.co.za", userAgent: UserAgent);

    [SetUp]
    public void Setup()
    {
        using (var sr = new StreamReader("lootRobots.txt"))
        {
            _robots.Load(sr.ReadToEnd()).Wait();
        }
    }

    [Test]
    public void Sitemaps_Test()
    {
        Assert.AreEqual("http://www.loot.co.za/index.xml.gz", _robots.Sitemaps.First());
    }

    [TestCase("https://loot.co.za/shop/", "one")]
    [TestCase("https://loot.co.za/cgi-bin/", "two")]
    [TestCase("https://loot.co.za/refer.html", "three")]
    [TestCase("https://loot.co.za/search/", "four")]
    [TestCase("https://loot.co.za/images/", "five")]
    [TestCase("everything is dissallowed", "Yandexbot")]
    [TestCase("/", "Baiduspider")]
    [TestCase("", "Ahrefsbot")]
    public void IsPathDisallowed_Test(string path, string userAgent)
    {
        Assert.IsTrue(_robots.IsPathDisallowed(path, userAgent), $"{path} should be dissallowed for user-agent {userAgent}");
    }

    [TestCase("https://loot.co.za/test/", "one")]
    public void IsPathAllowed_Test(string path, string userAgent)
    {
        Assert.IsTrue(_robots.IsPathAllowed(path, userAgent), $"{path} should be dissallowed for user-agent {userAgent}");
    }

    [TestCase("Ahrefsbot")]
    [TestCase("Baiduspider")]
    [TestCase("Yandexbot")]
    [TestCase("Googlebot-image")]
    [TestCase("*")]
    public void UserAgents_Contains_Test(string userAgent)
    {
        Assert.IsTrue(_robots.UserAgents.Any(x => userAgent.Equals(x.Name)), $"User-agent list should contain {userAgent}");
    }

    [TestCase("Ahrefsbot", 0)]
    [TestCase("Googlebot-image", 2)]
    [TestCase("*", 1)]
    [TestCase("other", 1)]
    public void CrawlDelay_Test(string userAgent, int delay)
    {
        Assert.AreEqual(delay, _robots.GetCrawlDelay(userAgent), $"Crawldelay for {userAgent} should be {delay}");
    }
}
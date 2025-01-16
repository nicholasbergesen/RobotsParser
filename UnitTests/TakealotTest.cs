using NUnit.Framework;
using RobotsParser;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace UnitTests;

public class TakealotTest
{
    private IRobots _robots = new Robots((url) => Download(url));

    private static async Task<string> Download(string url)
    {
        return "";
    }

    [SetUp]
    public void Setup()
    {
        using (var sr = new StreamReader("takealotRobots.txt"))
        {
            _robots.LoadRobotsContent(sr.ReadToEnd()).Wait();
        }
    }

    [Test]
    public void Sitemaps_Test()
    {
        Assert.AreEqual("https://www.takealot.com/sitemap.xml", _robots.Sitemaps.First());
    }

    [TestCase("/*filter=ColourVariant", "one")]
    public void IsPathDisallowed_Test(string path, string userAgent)
    {
        Assert.IsTrue(_robots.IsPathDisallowed(path, userAgent), $"{path} should be dissallowed for user-agent {userAgent}");
    }

    [TestCase("https://takealot.com/test/", "one")]
    [TestCase("/", "AdsBot-Google")]
    public void IsPathAllowed_Test(string path, string userAgent)
    {
        Assert.IsTrue(_robots.IsPathAllowed(path, userAgent), $"{path} should be allowed for user-agent {userAgent}");
    }

    [TestCase("Googlebot-Image")]
    [TestCase("*")]
    public void UserAgents_Contains_Test(string userAgent)
    {
        Assert.IsTrue(_robots.UserAgents.Any(x => userAgent.Equals(x.Name)), $"User-agent list should contain {userAgent}");
    }

    [TestCase("Ahrefsbot", 0)]
    [TestCase("Googlebot-image", 0)]
    [TestCase("*", 0)]
    [TestCase("other", 0)]
    public void CrawlDelay_Test(string userAgent, int delay)
    {
        Assert.AreEqual(delay, _robots.GetCrawlDelay(userAgent), $"Crawldelay for {userAgent} should be {delay}");
    }
}
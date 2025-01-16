# RobotsParser

**Nuget Page**: https://www.nuget.org/packages/Nick.RobotsParser/

## Install options
- Install-Package Nick.RobotsParser -Version 2.0.6
- dotnet add package Nick.RobotsParser --version 2.0.6

## Example Snippets
### Load robots.txt
```
var robots = new Robots(websiteUri: "https://websiteurl.com", userAgent: "my custom user agent");
await robots.Load();
```

### Get robots info
```
foreach (var userAgent in robots.UserAgents)
{
    System.Console.WriteLine(userAgent.Name);
    System.Console.WriteLine($"Allowed: {string.Join(',', userAgent.Allowed)}");
    System.Console.WriteLine($"Dis-Allowed: {string.Join(',', userAgent.Disallowed)}");
    System.Console.WriteLine($"Crawldelay: {userAgent.Crawldelay}");
    System.Console.WriteLine(Environment.NewLine);
}
System.Console.WriteLine($"Sitemaps: {string.Join(',', robots.Sitemaps)}");
```

### Get Sitemap Urls
```
var sitemaps = await robots.GetSitemapIndexes();
foreach (var sitemap in sitemaps)
{
    var urls = await robots.GetUrls(sitemap);
}
```

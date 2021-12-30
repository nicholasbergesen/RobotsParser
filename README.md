# Welcome to the robotsSharpParser wiki!

**Nuget Page**: https://www.nuget.org/packages/RobotsSharpParser/

## Install options
- Install-Package RobotsSharpParser -Version 2.0.0
- dotnet add package RobotsSharpParser --version 2.0.0

## Example Snippets
### Load robots.txt
```
IRobots robots = new Robots(websiteUri: "https://websiteurl.com", userAgent: "my custom user agent");
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

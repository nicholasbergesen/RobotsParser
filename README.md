# RobotsParser

**Nuget Page**: https://www.nuget.org/packages/Nick.RobotsParser/

## Install options
- Install-Package Nick.RobotsParser -Version 2.0.8
- dotnet add package Nick.RobotsParser --version 2.0.8

## Example Snippets
### Load robots.txt
```
public static async Task<string> GetHttpResponseBodyAsync(string url)
{
    using var httpClient = new HttpClient();
    HttpResponseMessage response = await httpClient.GetAsync(url);
    response.EnsureSuccessStatusCode();
    string responseBody = await response.Content.ReadAsStringAsync();
    return responseBody;
}

var robots = new Robots(downloadFunc: GetHttpResponseBodyAsync);
await robots.LoadRobotsFromUrl("https://www.google.com/robots.txt");
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

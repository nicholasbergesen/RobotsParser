using System.Xml;
using System.Xml.Serialization;

namespace RobotsParser
{
    public interface IRobots
    {
        Task<bool> LoadRobotsFromUrl(string robotsUrl);
        Task<bool> LoadRobotsContent(string robotsContent);
        IReadOnlyList<Useragent> UserAgents { get; }
        HashSet<string> Sitemaps { get; }
        IEnumerable<string> GetAllowedPaths(string userAgent = "*");
        IEnumerable<string> GetDisallowedPaths(string userAgent = "*");
        bool IsPathAllowed(string path, string userAgent = "*");
        bool IsPathDisallowed(string path, string userAgent = "*");
        int GetCrawlDelay(string userAgent = "*");
        Task<IReadOnlyList<tSitemap>> GetSitemapIndexes(string? sitemapUrl = null);
        Task<IReadOnlyList<tUrl>> GetUrls(tSitemap tSitemap);
    }

    public class ProgressEventArgs : EventArgs
    {
        public string ProgressMessage;
        public ProgressEventArgs(string progressMessage)
        {
            ProgressMessage = progressMessage;
        }
    }

    public class Robots : IRobots
    {
        private string? _robotsContent;
        private readonly bool _supressSitemapErrors;
        private readonly Func<string, Task<string>> _downloadFunc;

        public event ProgressEventHandler? OnProgress;
        public delegate void ProgressEventHandler(object sender, ProgressEventArgs e);

        public Robots(Func<string, Task<string>> downloadFunc, bool supressSitemapErrors = false)
        {
            _supressSitemapErrors = supressSitemapErrors;
            _downloadFunc = downloadFunc;
        }

        private void RaiseOnProgress(string progressMessage)
        {
            if (OnProgress is null)
                return;
            OnProgress(this, new ProgressEventArgs(progressMessage));
        }

        private async Task ParseRobots()
        {
            if(_robotsContent is null)
                throw new RobotsNotloadedException();

            _userAgents = new List<Useragent>();
            _sitemaps ??= new HashSet<string>();

            string? line;
            using StringReader sr = new(_robotsContent);
            Useragent currentAgent = new("*");
            while ((line = await sr.ReadLineAsync()) != null)
            {
                if (line.ToLower().StartsWith(Const.UserAgent.ToLower()))
                {
                    string name = line.Substring(Const.UserAgentLength, line.Length - Const.UserAgentLength).Trim(' ');
                    currentAgent = new Useragent(name);
                    _userAgents.Add(currentAgent);
                }
                else if (line.ToLower().StartsWith(Const.Disallow))
                    currentAgent.Disallowed.Add(line.Substring(Const.DisallowLength, line.Length - Const.DisallowLength).Trim(' '));
                else if (line.ToLower().StartsWith(Const.Allow))
                    currentAgent.Allowed.Add(line.Substring(Const.AllowLength, line.Length - Const.AllowLength).Trim(' '));
                else if (line.ToLower().StartsWith(Const.Sitemap))
                    _sitemaps.Add(line.Substring(Const.SitemapLength, line.Length - Const.SitemapLength).Trim(' '));
                else if (line.ToLower().StartsWith(Const.Crawldelay))
                    currentAgent.Crawldelay = int.Parse(line.Substring(Const.CrawldelayLength, line.Length - Const.CrawldelayLength).Trim(' '));
                else if (line == string.Empty || line[0] == '#' || line == "<!DOCTYPE html> ")
                    continue;
                else
                    throw new Exception($"Unable to parse {line} in robots.txt");
            }
        }

        private async Task<string> WebRequest(string url)
        {
            try
            {
                return await _downloadFunc(url);
            }
            catch (HttpRequestException ex)
            {
                RaiseOnProgress($"Web request returned failed status code: {ex.StatusCode}\r\n{ex.Message}");
            }
            catch (Exception ex)
            {
                RaiseOnProgress($"Error during web request:\r\n{ex.Message}");
            }

            return string.Empty;
        }

        #region Interface Methods

        public async Task<bool> LoadRobotsFromUrl(string robotsUrl)
        {
            if (!Uri.TryCreate(robotsUrl, UriKind.Absolute, out Uri? robots))
                throw new ArgumentException($"Unable to create uri from: {robotsUrl}");

            _robotsContent = await WebRequest(robots.ToString());
            if (!string.IsNullOrWhiteSpace(_robotsContent)) return false;

            await ParseRobots();
            return true;
        }

        public async Task<bool> LoadRobotsContent(string robotsContent)
        {
            _robotsContent = robotsContent;
            await ParseRobots();
            return true;
        }

        private List<Useragent>? _userAgents;
        public IReadOnlyList<Useragent> UserAgents
        {
            get
            {
                if (_userAgents is null)
                    throw new RobotsNotloadedException("Useragents is null");
                return _userAgents;
            }
        }

        private HashSet<string>? _sitemaps;
        public HashSet<string> Sitemaps
        {
            get
            {
                if (_sitemaps is null)
                    throw new RobotsNotloadedException("Sitemaps is null");
                return _sitemaps;
            }
        }

        public IEnumerable<string> GetAllowedPaths(string userAgent = "*") => UserAgents.First(x => x.Name == userAgent).Allowed;
        public IEnumerable<string> GetDisallowedPaths(string userAgent = "*") => UserAgents.First(x => x.Name == userAgent).Disallowed;
        public bool IsPathAllowed(string path, string userAgent = "*") {
            var isAllowed = UserAgents.FirstOrDefault(x => x.Name == userAgent)?.IsAllowed(path) ?? false;
            if(!isAllowed && userAgent != "*") {
                isAllowed = UserAgents.First(x => x.Name == "*").IsAllowed(path);
            }
            return isAllowed;
        }

        public bool IsPathDisallowed(string path, string userAgent = "*") {
            var isDissallowed = UserAgents.FirstOrDefault(x => x.Name == userAgent)?.IsDisallowed(path) ?? false;
            if(!isDissallowed && userAgent != "*") {
                isDissallowed = UserAgents.First(x => x.Name == "*").IsDisallowed(path);
            }
            return isDissallowed;
        } 

        public int GetCrawlDelay(string userAgent = "*") {
            var crawlDelay = UserAgents.FirstOrDefault(x => x.Name == userAgent)?.Crawldelay;
            if(crawlDelay is null && userAgent != "*") {
                crawlDelay = UserAgents.First(x => x.Name == "*").Crawldelay;
            }
            return crawlDelay ?? 0;
        }

        public async Task<IReadOnlyList<tSitemap>> GetSitemapIndexes(string? sitemapUrl = null)
        {
            if (!string.IsNullOrEmpty(sitemapUrl))
            {
                _sitemaps ??= new HashSet<string>();
                _sitemaps.Add(sitemapUrl);
            }

            if (_sitemaps is null)
            {
                if (_robotsContent is null)
                    throw new RobotsNotloadedException("Please call LoadRobotsFromUrl, LoadRobotsContent or pass a sitemap url to GetSitemapIndexes.");

                return new List<tSitemap>();
            }

            List<tSitemap> sitemaps = new List<tSitemap>(100000);
            if(_sitemaps.Any())
            {
                foreach (var sitemap in _sitemaps)
                    sitemaps.AddRange(await GetSitemapsInternal(sitemap));
            }

            return sitemaps;
        }

        public async Task<IReadOnlyList<tUrl>> GetUrls(tSitemap tSitemap)
        {
            if (tSitemap is null)
                throw new ArgumentNullException(nameof(tSitemap), "sitemap requires a value");

            var response = await WebRequest(tSitemap.loc);
            if (TryDeserializeXMLStream(response, out urlset? urlSet) && urlSet?.url is not null)
                return urlSet.url;
            else if (!_supressSitemapErrors)
                throw new Exception($"Unable to deserialize content from {tSitemap.loc} to type urlset");
            else
                return new List<tUrl>();
        }

        #endregion

        private async Task<IReadOnlyList<tSitemap>> GetSitemapsInternal(string sitemapUrl)
        {
            var response = await _downloadFunc(sitemapUrl);
            if (TryDeserializeXMLStream(response, out sitemapindex? sitemapIndex) && sitemapIndex?.sitemap is not null)
                return sitemapIndex.sitemap;
            else if (!_supressSitemapErrors)
                throw new Exception($"Unable to deserialize content from {sitemapUrl} to type sitemapindex");
            else
                return new List<tSitemap>();
        }

        private readonly List<tUrl> _sitemapLinks = new List<tUrl>(1000000);
        private async Task GetSitemapLinksInternal(string siteIndex)
        {
            var response = await _downloadFunc(siteIndex);
            if (TryDeserializeXMLStream(response, out sitemapindex? sitemapIndex) && sitemapIndex?.sitemap is not null)
            {
                foreach (tSitemap sitemap in sitemapIndex.sitemap)
                {
                    await GetSitemapLinksInternal(sitemap.loc);
                }
            }
            else
            {
                if (TryDeserializeXMLStream(response, out urlset? urlSet) && urlSet?.url is not null)
                {
                    _sitemapLinks.AddRange(urlSet.url.ToList());
                    RaiseOnProgress($"{_sitemapLinks.Count}");
                }
            }
        }

        private bool TryDeserializeXMLStream<T>(string stringValue, out T? xmlValue)
        {
            stringValue = StripVersionFromString(stringValue);

            using StringReader sr = new StringReader(stringValue);
            return TryDeserializeXMLStream(sr, out xmlValue);
        }

        private static bool TryDeserializeXMLStream<T>(TextReader reader, out T? xmlValue)
        {
            try
            {
                using XmlReader xmlReader = XmlReader.Create(reader, new XmlReaderSettings()
                {
                    ValidationType = ValidationType.None
                });
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                xmlValue = (T?)serializer.Deserialize(xmlReader);
                return xmlValue is not null;
            }
            catch
            {
                xmlValue = default;
                return false;
            }
        }

        private string StripVersionFromString(string val)
        {
            var endChar = val.IndexOf("?>");
            if(endChar != -1)
                return val.Remove(0, endChar + 2);
            return val;
        }
    }
}

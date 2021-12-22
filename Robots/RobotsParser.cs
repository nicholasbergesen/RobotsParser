using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace RobotsSharpParser
{
    public interface IRobots
    {
        Task Load();
        Task Load(string robotsContent);
        IReadOnlyList<Useragent> UserAgents { get; }
        IEnumerable<string> Sitemaps { get; }
        IEnumerable<string> GetAllowedPaths(string userAgent = "*");
        IEnumerable<string> GetDisallowedPaths(string userAgent = "*");
        bool IsPathAllowed(string path, string userAgent = "*");
        bool IsPathDisallowed(string path, string userAgent = "*");
        int GetCrawlDelay(string userAgent = "*");
        Task<IReadOnlyList<tSitemap>> GetSitemapIndexes(string sitemapUrl = "");
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

    public class Robots : IRobots, IDisposable
    {
        private readonly Uri _robotsUri;
        private string? _robots;
        private readonly HttpClient _client;

        public event ProgressEventHandler? OnProgress;
        public delegate void ProgressEventHandler(object sender, ProgressEventArgs e);

        public Robots(Uri websiteUri, string userAgent)
        {
            if(websiteUri is null)
                throw new ArgumentNullException(nameof(websiteUri));

            if (!Uri.TryCreate(websiteUri, "/robots.txt", out Uri? robots))
                throw new ArgumentException($"Unable to append robots.txt to {websiteUri}");

            _robotsUri = robots;
            HttpClientHandler handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            _client = new HttpClient(handler, true);
            _client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            _client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            _client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-ZA"));
            _client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-GB"));
            _client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));
            _client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
            _client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true
            };
            _client.DefaultRequestHeaders.Connection.Add("keep-alive");
            _client.DefaultRequestHeaders.Host = websiteUri.Host;
            _client.DefaultRequestHeaders.Pragma.Add(new NameValueHeaderValue("no-cache"));
        }

        public Robots(string websiteUri, string userAgent)
            : this(new Uri(websiteUri), userAgent) { }

        private void RaiseOnProgress(string progressMessage)
        {
            if (OnProgress is null)
                return;
            OnProgress(this, new ProgressEventArgs(progressMessage));
        }

        private async Task ParseRobots()
        {
            if(_robots is null)
                throw new RobotsNotloadedException();

            _userAgents = new List<Useragent>();
            _sitemaps = new HashSet<string>();

            string? line;
            using (StringReader sr = new StringReader(_robots))
            {
                Useragent currentAgent = new Useragent("*");
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
                    else if (line == string.Empty || line[0] == '#')
                        continue;
                    else
                        throw new Exception($"Unable to parse {line} in robots.txt");
                }
            }
        }

        #region Interface Methods

        public async Task Load()
        {
            _robots = await _client.GetStringAsync(_robotsUri);
            await ParseRobots();
        }

        public async Task Load(string robotsContent)
        {
            _robots = robotsContent;
            await ParseRobots();
        }

        private List<Useragent>? _userAgents;
        public IReadOnlyList<Useragent> UserAgents
        {
            get
            {
                if (_userAgents is null)
                    throw new RobotsNotloadedException();
                return _userAgents;
            }
        }

        private HashSet<string>? _sitemaps;
        public IEnumerable<string> Sitemaps
        {
            get
            {
                if (_sitemaps is null)
                    throw new RobotsNotloadedException();
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

        public async Task<IReadOnlyList<tSitemap>> GetSitemapIndexes(string sitemapUrl = "")
        {
            if(_sitemaps is null)
                throw new RobotsNotloadedException();

            if (string.Empty.Equals(sitemapUrl))
            {
                List<tSitemap> sitemaps = new List<tSitemap>(100000);
                if(_sitemaps.Count > 0)
                {
                    foreach (var sitemap in _sitemaps)
                        sitemaps.AddRange(await GetSitemapsInternal(sitemap));
                }
                return sitemaps;
            }
            else 
            {
                return await GetSitemapsInternal(sitemapUrl);
            }
        }

        public async Task<IReadOnlyList<tUrl>> GetUrls(tSitemap tSitemap)
        {
            if (tSitemap is null)
                throw new ArgumentNullException(nameof(tSitemap), "sitemap requires a value");

            MemoryStream stream = new MemoryStream();
            Stream rawstream = await _client.GetStreamAsync(tSitemap.loc);
            rawstream.CopyTo(stream);

            if (!TryDecompress(stream, out byte[] bytes))
                bytes = stream.ToArray();

            if (TryDeserializeXMLStream(bytes, out urlset? urlSet) && urlSet?.url is not null)
                return urlSet.url;
            else
                throw new Exception($"Unable to deserialize content from {tSitemap.loc} to type urlset");
        }

        #endregion

        private async Task<IReadOnlyList<tSitemap>> GetSitemapsInternal(string sitemapUrl)
        {
            MemoryStream stream = new MemoryStream();
            Stream rawstream = await _client.GetStreamAsync(sitemapUrl);
            rawstream.CopyTo(stream);

            if (!TryDecompress(stream, out byte[] bytes))
                bytes = stream.ToArray();

            if (TryDeserializeXMLStream(bytes, out sitemapindex? sitemapIndex) && sitemapIndex?.sitemap is not null)
                return sitemapIndex.sitemap;
            else
                throw new Exception($"Unable to deserialize content from {sitemapUrl} to type sitemapindex");
        }

        private readonly List<tUrl> _sitemapLinks = new List<tUrl>(1000000);
        private async Task GetSitemapLinksInternal(string siteIndex)
        {
            MemoryStream stream = new MemoryStream();
            Stream rawstream = await _client.GetStreamAsync(siteIndex);
            rawstream.CopyTo(stream);

            if (!TryDecompress(stream, out byte[] bytes))
                bytes = stream.ToArray();

            if (TryDeserializeXMLStream(bytes, out sitemapindex? sitemapIndex) && sitemapIndex?.sitemap is not null)
            {
                foreach (tSitemap sitemap in sitemapIndex.sitemap)
                {
                    await GetSitemapLinksInternal(sitemap.loc);
                }
            }
            else
            {
                if (TryDeserializeXMLStream(bytes, out urlset? urlSet) && urlSet?.url is not null)
                {
                    _sitemapLinks.AddRange(urlSet.url.ToList());
                    RaiseOnProgress($"{_sitemapLinks.Count}");
                }
            }
        }

        private bool TryDeserializeXMLStream<T>(byte[] bytes, out T? xmlValue)
        {
            using (StringReader sr = new StringReader(Encoding.UTF8.GetString(bytes)))
            {
                return TryDeserializeXMLStream(sr, out xmlValue);
            }
        }

        private bool TryDeserializeXMLStream<T>(TextReader reader, out T? xmlValue)
        {
            try
            {
                using (XmlReader xmlReader = XmlReader.Create(reader))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(T));
                    xmlValue = (T?)serializer.Deserialize(xmlReader);
                    return xmlValue is not null;
                }
            }
            catch
            {
                xmlValue = default;
                return false;
            }
        }

        private bool TryDecompress(Stream stream, out byte[] bytes)
        {
            try
            {
                using (MemoryStream decompressedStream = new MemoryStream())
                {
                    stream.Position = 0;
                    using (GZipStream decompressionStream = new GZipStream(stream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedStream);
                        bytes = decompressedStream.ToArray();
                    }
                }
                return true;
            }
            catch
            {
                bytes = new byte[0];
                return false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Robots()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_client != null)
                {
                    _client.Dispose();
                }
            }
        }
    }
}
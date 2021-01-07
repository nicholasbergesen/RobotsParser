using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

namespace RobotsSharpParser
{
    public interface IRobots
    {
        void Load();
        Task LoadAsync();
        void Load(string robotsContent);
        Task LoadAsync(string robotsContent);
        int UserAgentCount();
        IReadOnlyList<Useragent> UserAgents { get; }
        IEnumerable<string> Sitemaps { get; }
        IEnumerable<string> GetAllowedPaths(string userAgent = "*");
        IEnumerable<string> GetDisallowedPaths(string userAgent = "*");
        bool IsPathAllowed(string path, string userAgent = "*");
        bool IsPathDisallowed(string path, string userAgent = "*");
        int GetCrawlDelay(string userAgent = "*");
        IReadOnlyList<tSitemap> GetSitemapIndexes(string sitemapUrl = "");
        Task<IReadOnlyList<tSitemap>> GetSitemapIndexesAsync(string sitemapUrl = "");
        IReadOnlyList<tUrl> GetUrls(tSitemap tSitemap);
        Task<IReadOnlyList<tUrl>> GetUrlsAsync(tSitemap tSitemap);
        bool IgnoreErrors { get; set; }
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
        private string _robots;
        private HttpClient _client;

        /// <summary>
        /// Will ignore errors when attempting to parse sitemap
        /// </summary>
        public bool IgnoreErrors { get; set; } = false;
        public Robots(Uri websiteUri, string userAgent)
        {
            if (Uri.TryCreate(websiteUri, "/robots.txt", out Uri robots))
                _robotsUri = robots;
            else
                throw new ArgumentException($"Unable to append robots.txt to {websiteUri}");

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

        private async Task ParseRobotsAsync()
        {
            _userAgents = new List<Useragent>();
            _sitemaps = new HashSet<string>();

            string line;
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

        public void Load()
        {
            LoadAsync().Wait();
        }

        public async Task LoadAsync()
        {
            _robots = await _client.GetStringAsync(_robotsUri);
            await ParseRobotsAsync();
        }

        public void Load(string robotsContent)
        {
            _robots = robotsContent;
            ParseRobotsAsync().Wait();
        }

        public async Task LoadAsync(string robotsContent)
        {
            _robots = robotsContent;
            await ParseRobotsAsync();
        }

        public int UserAgentCount() => _userAgents.Count;

        private List<Useragent> _userAgents;
        public IReadOnlyList<Useragent> UserAgents
        {
            get
            {
                if (_userAgents == null)
                    throw new RobotsNotloadedException("Please call Load or LoadAsync.");
                return _userAgents;
            }
        }

        private HashSet<string> _sitemaps;
        public IEnumerable<string> Sitemaps
        {
            get
            {
                if (_sitemaps == null)
                    throw new RobotsNotloadedException("Please call Load or LoadAsync.");
                return _sitemaps;
            }
        }

        public IEnumerable<string> GetAllowedPaths(string userAgent = "*") => UserAgents.First(x => x.Name == userAgent).Allowed;
        public IEnumerable<string> GetDisallowedPaths(string userAgent = "*") => UserAgents.First(x => x.Name == userAgent).Disallowed;
        public bool IsPathAllowed(string path, string userAgent = "*") => UserAgents.First(x => x.Name == userAgent).IsAllowed(path);
        public bool IsPathDisallowed(string path, string userAgent = "*") => UserAgents.First(x => x.Name == userAgent).IsDisallowed(path);
        public int GetCrawlDelay(string userAgent = "*") => UserAgents.First(x => x.Name == userAgent).Crawldelay;

        public IReadOnlyList<tSitemap> GetSitemapIndexes(string sitemapUrl = "")
        {
            var task = GetSitemapIndexesAsync(sitemapUrl);
            task.Wait();
            if (task.IsFaulted)
                throw task.Exception;
            else
                return task.Result;
        }

        public async Task<IReadOnlyList<tSitemap>> GetSitemapIndexesAsync(string sitemapUrl = "")
        {

            if (sitemapUrl == string.Empty)
            {
                List<tSitemap> _sitemapInternal = new List<tSitemap>(1000000);
                foreach (var sitemap in _sitemaps)
                    _sitemapInternal.AddRange(await GetSitemapsInternal(sitemap));
                return _sitemapInternal;
            }
            else
                return await GetSitemapsInternal(sitemapUrl);
        }

        public IReadOnlyList<tUrl> GetUrls(tSitemap tSitemap)
        {
            var task = GetUrlsAsync(tSitemap);
            task.Wait();
            if (task.IsFaulted)
                throw task.Exception;
            else
                return task.Result;
        }

        public async Task<IReadOnlyList<tUrl>> GetUrlsAsync(tSitemap tSitemap)
        {
            if (tSitemap == null)
                throw new ArgumentNullException(nameof(tSitemap), "sitemap requires a value");

            var response = await _client.GetAsync(tSitemap.loc);
            if (response.IsSuccessStatusCode)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    await response.Content.CopyToAsync(stream);

                    if (!TryDecompress(stream, out byte[] bytes))
                        bytes = stream.ToArray();

                    if (TryDeserializeXMLStream("urlset", bytes, out urlset urlSet) && urlSet.url != null)
                        return urlSet.url;
                    else if (!IgnoreErrors)
                        throw new Exception($"Unable to deserialize content from {tSitemap.loc} to type urlset");
                    else
                        return new List<tUrl>();
                }
            }
            else
            {
                //add logging for non successful error codes
                return new List<tUrl>();
            }
        }

        #endregion

        private async Task<IReadOnlyList<tSitemap>> GetSitemapsInternal(string sitemapUrl)
        {
            MemoryStream stream = new MemoryStream();
            Stream rawstream = await _client.GetStreamAsync(sitemapUrl);
            rawstream.CopyTo(stream);

            if (!TryDecompress(stream, out byte[] bytes))
                bytes = stream.ToArray();

            if (TryDeserializeXMLStream("sitemapindex", bytes, out sitemapindex sitemapIndex))
                return sitemapIndex.sitemap;
            else if (!IgnoreErrors)
                throw new Exception($"Unable to deserialize content from {sitemapUrl} to type sitemapindex");
            else
                return new List<tSitemap>();
        }

        private bool TryDeserializeXMLStream<T>(string root, byte[] bytes, out T xmlValue)
        {
            using (StringReader sr = new StringReader(Encoding.UTF8.GetString(bytes)))
            {
                return TryDeserializeXMLStream(sr, root, out xmlValue);
            }
        }

        private bool TryDeserializeXMLStream<T>(TextReader reader, string root, out T xmlValue)
        {
            try
            {
                using (NamespaceIgnorantXmlTextReader xmlReader = new NamespaceIgnorantXmlTextReader(reader))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(T));
                    xmlValue = (T)serializer.Deserialize(xmlReader);

                    return xmlValue != null;
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
                bytes = null;
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
                    _client = null;
                }
            }
        }
    }
}

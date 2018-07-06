using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
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
        IReadOnlyList<tUrl> GetSitemapLinks(string sitemapUrl = "");
        Task<IReadOnlyList<tUrl>> GetSitemapLinksAsync(string sitemapUrl = "");
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
        private Uri _robotsUri;
        private string _robots;
        private HttpClient _client = new HttpClient();
        private bool _enableErrorCorrection;

        public event ProgressEventHandler OnPorgress;
        public delegate void ProgressEventHandler(object sender, ProgressEventArgs e);

        public Robots(Uri websiteUri, string userAgent, bool enableErrorCorrection = false)
        {
            if (Uri.TryCreate(websiteUri, "/robots.txt", out Uri robots))
                _robotsUri = robots;
            else
                throw new ArgumentException($"Unable to append robots.txt to {websiteUri}");

            _enableErrorCorrection = enableErrorCorrection;

            _client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            _client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            _client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-ZA"));
            _client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-GB"));
            _client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));
            _client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
            _client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue();
            _client.DefaultRequestHeaders.CacheControl.NoCache = true;
            _client.DefaultRequestHeaders.Connection.Add("keep-alive");
            _client.DefaultRequestHeaders.Host = websiteUri.Host;
            _client.DefaultRequestHeaders.Pragma.Add(new NameValueHeaderValue("no-cache"));
        }

        public Robots(string websiteUri, string userAgent, bool enableErrorCorrection = false)
        {
            if (Uri.TryCreate(websiteUri + "/robots.txt", UriKind.Absolute, out Uri robots))
                _robotsUri = robots;
            else
                throw new ArgumentException($"Unable to append robots.txt to {websiteUri}");

            _enableErrorCorrection = enableErrorCorrection;

            _client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            _client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            _client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-ZA"));
            _client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-GB"));
            _client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));
            _client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
            _client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue();
            _client.DefaultRequestHeaders.CacheControl.NoCache = true;
            _client.DefaultRequestHeaders.Connection.Add("keep-alive");
            _client.DefaultRequestHeaders.Host = robots.Host;
            _client.DefaultRequestHeaders.Pragma.Add(new NameValueHeaderValue("no-cache"));
        }

        private void RaiseOnProgress(string progressMessage)
        {
            if (OnPorgress == null)
                return;
            OnPorgress(this, new ProgressEventArgs(progressMessage));
        }

        public async Task LoadAsync()
        {
            _robots = await _client.GetStringAsync(_robotsUri);
            await ParseRobotsAsync();
        }

        public void Load()
        {
            LoadAsync().Wait();
        }

        public async Task LoadAsync(string robotsContent)
        {
            _robots = robotsContent;
            await ParseRobotsAsync();
        }

        public void Load(string robotsContent)
        {
            _robots = robotsContent;
            ParseRobotsAsync().Wait();
        }

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
        public int UserAgentCount() => _userAgents.Count;
        public int GetCrawlDelay(string userAgent = "*") => UserAgents.First(x => x.Name == userAgent).Crawldelay;

        private List<tUrl> _sitemapLinks = new List<tUrl>(1000000);

        public async Task<IReadOnlyList<tUrl>> GetSitemapLinksAsync(string sitemapUrl = "")
        {
            if (sitemapUrl == string.Empty)
                foreach (var siteIndex in _sitemaps)
                    await GetSitemalLinksInternal(siteIndex);
            else
                await GetSitemalLinksInternal(sitemapUrl);

            return _sitemapLinks;
        }

        private async Task GetSitemalLinksInternal(string siteIndex)
        {
            Stream stream = await GetStreamAsync(siteIndex);
            if (stream == null)
                return;

            if (TryDeserializeXMLStream(stream, out sitemapindex sitemapIndex))
            {
                int sitemapCount = 0;
                foreach (tSitemap sitemap in sitemapIndex.sitemap)
                {
                    await GetSitemalLinksInternal(sitemap.loc);
                    RaiseOnProgress($"{sitemapCount++ / sitemapIndex.sitemap.Length}:##");
                }
            }
            else
            {
                stream.Close();
                stream = await GetStreamAsync(siteIndex);
                if (stream == null)
                    stream = await GetStreamAsync(siteIndex);
                if (stream == null)
                    return;

                if (_enableErrorCorrection)
                    stream = await RemoveMalformedTagsFromStreamAsync(stream);

                if (TryDeserializeXMLStream(stream, out urlset urlSet) && urlSet.url != null)
                    _sitemapLinks.AddRange(urlSet.url.ToList());
            }
        }

        private async Task<Stream> GetStreamAsync(string url)
        {
            try
            {
                Stream stream = await _client.GetStreamAsync(url);
                if (url.EndsWith(".gz"))
                    stream = new GZipStream(stream, CompressionMode.Decompress);
                return stream;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static async Task<Stream> RemoveMalformedTagsFromStreamAsync(Stream stream)
        {
            using (StreamReader sr = new StreamReader(stream))
            {
                string streamContent = await sr.ReadToEndAsync();
                streamContent.Replace("<loc/>", "");
                stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(streamContent));
            }

            return stream;
        }

        public IReadOnlyList<tUrl> GetSitemapLinks(string sitemapUrl = "")
        {
            var task = GetSitemapLinksAsync(sitemapUrl);
            task.Wait();
            if (task.IsFaulted)
                throw task.Exception;
            else
                return task.Result;
        }

        private bool TryDeserializeXMLStream<T>(Stream stream, out T xmlValue)
        {
            try
            {
                using (XmlReader xmlReader = XmlReader.Create(stream))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(T));
                    xmlValue = (T)serializer.Deserialize(xmlReader);
                    return xmlValue != null;
                }
            }
            catch
            {
                xmlValue = default(T);
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

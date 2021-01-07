using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RobotsSharpParser
{
    //This class is used to solve the issue of some sites using http and others using https
    //in their sitemap files.
    public class NamespaceIgnorantXmlTextReader : XmlTextReader
    {
        public NamespaceIgnorantXmlTextReader(System.IO.TextReader reader) : base(reader) { }

        public override string NamespaceURI
        {
            get { return "http://www.sitemaps.org/schemas/sitemap/0.9"; }
        }
    }
}

using System.Xml.Serialization;

namespace SitecoreExtension.SeoUrl.UrlRedirect.Model
{
    [XmlType("add")]
    public class Condition
    {
        [XmlAttribute("input")]
        public string Input { get; set; }

        [XmlAttribute("pattern")]
        public string Pattern { get; set; }

        [XmlAttribute("negate")]
        public bool Negate { get; set; }
    }
}
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SitecoreExtension.SeoUrl.UrlRedirect.Model
{
    [XmlType("redirection")]
    public class Redirection
    {
        public Redirection()
        {
            this.Rules = new List<Rule>();
        }
        [XmlArray("rules")]
        public List<Rule> Rules
        {
            get;
            private set;
        }
    }
}
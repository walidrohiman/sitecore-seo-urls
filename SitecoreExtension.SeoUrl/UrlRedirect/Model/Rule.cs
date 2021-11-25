using System.Collections.Generic;
using System.Xml.Serialization;

namespace SitecoreExtension.SeoUrl.UrlRedirect.Model
{
    [XmlType(TypeName = "rule")]
    public class Rule
    {
        public Rule()
        {
            this.Conditions = new List<Condition>();
        }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlElement("match")]
        public Match Match { get; set; }

        [XmlArray("conditions")]
        public List<Condition> Conditions { get; private set; }

        [XmlElement("action")]
        public Action Action { get; set; }
    }
}
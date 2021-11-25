using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SitecoreExtension.SeoUrl.UrlRedirect.Model
{
    [XmlType("match")]
    public class Match
    {
        [XmlAttribute("url")]
        public string Url { get; set; }

        [XmlIgnore]
        public MatchType MatchType { get; set; }

        [XmlAttribute("matchType")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string MatchTypeAsString
        {
            get { return this.MatchType.ToString(); }
            set
            {
                var matchType = MatchType.Pattern;
                if (!string.IsNullOrWhiteSpace(value))
                    Enum.TryParse(value, true, out matchType);
                this.MatchType = matchType;
            }
        }
    }

    public enum MatchType
    {
        Pattern
    }
}
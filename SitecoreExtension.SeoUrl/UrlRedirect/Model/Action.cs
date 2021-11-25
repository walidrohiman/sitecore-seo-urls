using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SitecoreExtension.SeoUrl.UrlRedirect.Model
{
    [XmlType("action")]
    public class Action
    {
        [XmlIgnore]
        public RoutingMode Type { get; set; }

        [XmlAttribute("type")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string TypeAsString
        {
            get { return this.Type.ToString(); }
            set
            {
                var routineMode = RoutingMode.Redirect;
                Enum.TryParse(value, true, out routineMode);
                this.Type = routineMode;
            }
        }

        [XmlAttribute("url")]
        public string Url { get; set; }

        [XmlIgnore]
        public RedirectionType RedirectType { get; set; }


        [XmlAttribute("redirectType")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string RedirectTypeAsString
        {
            get
            {
                return this.RedirectType.ToString();
            }

            set
            {
                var redirectType = RedirectionType.Permanent;

                if (!string.IsNullOrWhiteSpace(value))
                {
                    Enum.TryParse(value, true, out redirectType);
                }

                this.RedirectType = redirectType;
            }
        }
    }

    public enum RedirectionType
    {
        Temporary,
        Permanent
    }

    public enum RoutingMode
    {
        Redirect
    }
}
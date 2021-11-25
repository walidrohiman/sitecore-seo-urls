using System.Xml.Serialization;
using Sitecore.Data;
using SitecoreExtension.SeoUrl.RuleTypes;

namespace SitecoreExtension.SeoUrl.LinkProvider.Cache
{
    [XmlType(TypeName = "rule")]
    public class LinkManagerRuleItem
    {
        public const string UrlLinkManagerRulePathFieldName = "Path";

        public const string UrlLinkManagerRulePatternFieldName = "Pattern";

        public LinkManagerRuleItem(Sitecore.Data.Items.Item item)
        {
            this.ItemId = item.ID;

            this.Pattern = item[UrlLinkManagerRulePatternFieldName];

            this.Path = item[UrlLinkManagerRulePathFieldName];

            this.MatchType = RuleTypes.MatchType.MatchesThePattern;

            this.Using = RuleTypes.Using.RegularExpressions;
        }

        public ID ItemId { get; }

        public string Pattern { get; }

        public string Path { get; }

        public MatchType? MatchType { get; set; }

        public Using? Using { get; set; }
    }
}
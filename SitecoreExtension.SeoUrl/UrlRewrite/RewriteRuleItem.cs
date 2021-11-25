using System;
using Sitecore.Data;
using Sitecore.Data.Items;
using SitecoreExtension.SeoUrl.RuleTypes;

namespace SitecoreExtension.SeoUrl.UrlRewrite
{
    [Serializable]
    public class RewriteRuleItem
    {
        public RewriteRuleItem(Item item)
        {
            this.ItemId = item.ID;
            this.MatchType = (RuleTypes.MatchType.MatchesThePattern);
            this.Using = RuleTypes.Using.RegularExpressions;
            this.Pattern = item[nameof(Pattern)];
            this.Path = item[nameof(Path)];
        }

        public ID ItemId { get; }

        public string Pattern { get; }

        public string Path { get; }

        public MatchType? MatchType { get; set; }

        public Using? Using { get; set; }
    }
}

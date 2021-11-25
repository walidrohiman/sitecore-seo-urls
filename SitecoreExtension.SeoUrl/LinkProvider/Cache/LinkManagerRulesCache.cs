using System.Collections.Generic;
using System.Linq;
using Sitecore;
using Sitecore.Caching;

namespace SitecoreExtension.SeoUrl.LinkProvider.Cache
{
    public class LinkManagerRulesCache : CustomCache
    {
        public Sitecore.Data.Database Db { get; }
        public string SiteName { get; }
        private const string InboundRulesKey = "LinkManagerRules";

        public LinkManagerRulesCache(Sitecore.Data.Database db, string siteName) : base($"UrlLinkManagerRules[{siteName}_{db.Name}]", StringUtil.ParseSizeString("1MB"))
        {
            this.Db = db;
            this.SiteName = siteName;
        }

        public List<LinkManagerRuleItem> GetInboundRules()
        {
            return this.GetRules<LinkManagerRuleItem>(InboundRulesKey);
        }

        public void SetInboundRules(IEnumerable<LinkManagerRuleItem> inboundRules)
        {
            this.SetRules(inboundRules, InboundRulesKey);
        }

        public List<T> GetRules<T>(string key)
        {
            List<T> returnRules = null;
            var rules = this.GetObject(key) as IEnumerable<T>;
            if (rules != null)
            {
                returnRules = rules.ToList();
            }

            return returnRules;
        }

        public void SetRules<T>(IEnumerable<T> outboundRules, string key)
        {
            this.SetObject(key, outboundRules);
        }
    }
}
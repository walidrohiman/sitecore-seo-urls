using System.Collections.Generic;
using System.Linq;
using Sitecore;
using Sitecore.Caching;
using SitecoreExtension.SeoUrl.UrlRewrite;

namespace SitecoreExtension.SeoUrl.Cache
{
    public class RulesCache : CustomCache
    {
        public Sitecore.Data.Database Db { get; }

        public string SiteName { get; }
        
        public RulesCache(Sitecore.Data.Database db, string siteName) : base($"UrlRewriteRules[{siteName}_{db.Name}]", StringUtil.ParseSizeString("1MB"))
        {
            this.Db = db;
            this.SiteName = siteName;
        }

        public List<UrlRewriteRuleItem> GetInboundRules()
        {
            var inboundRulesKey = $"RewriteRules-{Context.Site.Name}-{Context.Site.Language}";
            return this.GetRules<UrlRewriteRuleItem>(inboundRulesKey);
        }

        public void SetInboundRules(IEnumerable<RewriteRuleItem> inboundRules)
        {
            var inboundRulesKey = $"RewriteRules-{Context.Site.Name}-{Context.Site.Language}";
            this.SetRules(inboundRules, inboundRulesKey);
        }

        public List<T> GetRules<T>(string key)
        {
            List<T> returnRules = null;
            if (this.GetObject(key) is IEnumerable<T> rules)
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
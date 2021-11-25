using System.Collections.Generic;
using System.Linq;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Web;
using SitecoreExtension.SeoUrl.Cache;

namespace SitecoreExtension.SeoUrl.UrlRewrite
{
    public class RulesEngine
    {
        private readonly string _rewriteRulesPath = Settings.GetSetting("rewriteRules");

        public RulesEngine(Database db, SiteInfo site)
        {
            this.Database = db;
            this.Site = site;
        }

        public Database Database { get; }

        public SiteInfo Site { get; }

        public List<UrlRewriteRuleItem> GetInboundRules()
        {
            if (this.Database == null || this.Site == null)
                return null;

            var rulesFolderItems = this.GetRulesFolderItems();

            if (rulesFolderItems == null)
                return null;

            var inboundRules = new List<UrlRewriteRuleItem>();

            inboundRules.AddRange(rulesFolderItems.Select(descendantItem => new UrlRewriteRuleItem(descendantItem)));
            return inboundRules;
        }

        private IEnumerable<Sitecore.Data.Items.Item> GetRulesFolderItems()
        {
            var urlRouting = this.Database.GetItem(_rewriteRulesPath); 

            var rulesFolder = urlRouting.GetChildren(ChildListOptions.SkipSorting);

            return rulesFolder;
        }

        #region Caching

        private RulesCache GetRulesCache()
        {
            return UrlRuleCacheHelper.GetCache(this.Database, this.Site.Name);
        }

        internal List<UrlRewriteRuleItem> GetCachedInboundRules()
        {
            var inboundRules = this.GetInboundRules();

            if (inboundRules != null)
            {
                Log.Info($"Adding {0} rules to Cache [{1}] in {this.Site.Name}_{this.Database.Name}", this);

                var cache = this.GetRulesCache();
                cache.SetInboundRules(inboundRules);
            }
            else
            {
                Log.Info($"No rules in {this.Site.Name}_{this.Database.Name}", this);
            }

            return inboundRules;
        }

        #endregion
    }
}

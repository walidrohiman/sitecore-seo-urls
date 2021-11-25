using System.Collections.Generic;
using System.Linq;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Sites;
using Sitecore.Web;
using SitecoreExtension.SeoUrl.LinkProvider.Cache;

namespace SitecoreExtension.SeoUrl.LinkProvider
{
    public class LinkManagerRulesEngine
    {
        private readonly string _linkSettingsPath = Settings.GetSetting("linkSettingsRules");

        public LinkManagerRulesEngine(Database db, SiteInfo site)
        {
            this.Database = db;
            this.Site = site;
        }


        public Database Database { get; }

        public SiteInfo Site { get; }

        public List<LinkManagerRuleItem> GetInboundRules()
        {
            if (this.Database == null || this.Site == null)
                return null;

            using (new SiteContextSwitcher(SiteContext.GetSite(this.Site.Name)))
            using (new LanguageSwitcher(this.Site.Language))
            {
                var rulesFolderItems = this.GetRulesFolderItems();
                if (rulesFolderItems == null)
                    return null;

                var inboundRules = new List<LinkManagerRuleItem>();
                inboundRules.AddRange(rulesFolderItems.Select(descendantItem => new LinkManagerRuleItem(descendantItem)));

                return inboundRules;
            }
        }

        private IEnumerable<Sitecore.Data.Items.Item> GetRulesFolderItems()
        {
            var urlRouting = this.Database.GetItem(_linkSettingsPath);
            if (urlRouting == null)
                return null;

            var rulesFolder = urlRouting.GetChildren();

            return rulesFolder;
        }

        #region Caching

        private LinkManagerRulesCache GetRulesCache()
        {
            return LinkManagerRulesCacheManager.GetCache(this.Database, this.Site.Name);
        }

        internal List<LinkManagerRuleItem> GetCachedInboundRules()
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
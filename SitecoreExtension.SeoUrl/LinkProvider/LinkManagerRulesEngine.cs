using System;
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

        //internal void RefreshRule(Sitecore.Data.Items.Item item, Sitecore.Data.Items.Item redirectFolderItem)
        //{
        //    this.UpdateRulesCache(item, redirectFolderItem, this.AddRule);
        //}


        //internal void DeleteRuleFromAllCaches(Sitecore.Data.Items.Item item)
        //{
        //    Log.Info($"Deleting {item.ID} - {item.Name} from all caches.", this);

        //    var caches = LinkManagerRulesCacheManager.GetDatabaseRules(item.Database.Name);

        //    foreach (var rulesCache in caches)
        //    {
        //        var rules = rulesCache.GetInboundRules();

        //        var existingInboundRule = rules?.FirstOrDefault(e => e.ItemId == item.ID);

        //        if (existingInboundRule != null)
        //        {
        //            Log.Info($"Rule {item.ID} - {item.Name} found in '{rulesCache.Name}' cache.", this);
        //            rules.Remove(existingInboundRule);

        //            rulesCache.SetInboundRules(rules.OfType<LinkManagerRuleItem>());
        //        }
        //    }
        //}

        //private void UpdateRulesCache(
        //    Sitecore.Data.Items.Item item,
        //    Sitecore.Data.Items.Item redirectFolderItem,
        //    Action<Sitecore.Data.Items.Item, Sitecore.Data.Items.Item, List<LinkManagerRuleItem>> action)
        //{
        //    var cache = this.GetRulesCache();
        //    IEnumerable<LinkManagerRuleItem> baseRules = null;

        //    baseRules = cache.GetInboundRules();
        //    if (baseRules == null)
        //        baseRules = this.GetInboundRules();

        //    if (baseRules != null)
        //    {
        //        var rules = baseRules.ToList();

        //        action(item, redirectFolderItem, rules);

        //        Log.Info($"Updating Rules Cache in {this.Site.Name}_{this.Database.Name}; count:{rules.Count}", this);
        //        cache.SetInboundRules(rules.OfType<LinkManagerRuleItem>());
        //    }
        //}

        //private void AddRule(Sitecore.Data.Items.Item item, Sitecore.Data.Items.Item redirectFolderItem,
        //    List<LinkManagerRuleItem> inboundRules)
        //{

        //    Log.Info($"Adding Rule - item {item.Paths.FullPath} in {this.Database.Name}", this);

        //    var newRule = new LinkManagerRuleItem(item);

        //    this.AddOrRemoveRule(item, redirectFolderItem, inboundRules, newRule);
        //}

        //private void AddOrRemoveRule(
        //    Sitecore.Data.Items.Item item,
        //    Sitecore.Data.Items.Item redirectFolderItem,
        //    List<LinkManagerRuleItem> rules,
        //    LinkManagerRuleItem newRule,
        //    bool enabled = true)
        //{
        //    //in case if enabled/disabled will be implemented for a rule
        //    if (enabled)
        //    {
        //        var existingRule = rules.FirstOrDefault(e => e.ItemId == item.ID);
        //        if (existingRule != null)
        //        {
        //            Log.Info($"Replace Rule - item {item.Paths.FullPath} in {this.Database.Name}", this);

        //            var index = rules.FindIndex(e => e.ItemId == existingRule.ItemId);
        //            rules.RemoveAt(index);
        //            rules.Insert(index, newRule);
        //        }
        //        else
        //        {
        //            Log.Info($"Adding Rule - item {item.Paths.FullPath} in {this.Database.Name}", this);

        //            rules.Add(newRule);
        //        }
        //    }
        //    else
        //    {
        //        this.RemoveRule(item, redirectFolderItem, rules);
        //    }
        //}

        //private void RemoveRule(Sitecore.Data.Items.Item item, Sitecore.Data.Items.Item redirectFolderItem, List<LinkManagerRuleItem> inboundRules)
        //{
        //    Log.Info($"Remove Rule - item {item.Paths.FullPath} in {this.Database.Name}", this);

        //    var existingInboundRule = inboundRules.FirstOrDefault(e => e.ItemId == item.ID);
        //    if (existingInboundRule != null)
        //        inboundRules.Remove(existingInboundRule);
        //}

        #endregion
    }
}
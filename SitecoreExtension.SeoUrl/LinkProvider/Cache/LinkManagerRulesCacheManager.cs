using System.Collections.Generic;
using Sitecore.Diagnostics;

namespace SitecoreExtension.SeoUrl.LinkProvider.Cache
{
    public static class LinkManagerRulesCacheManager
    {
        private static Dictionary<string, LinkManagerRulesCache> Caches = new Dictionary<string, LinkManagerRulesCache>();

        public static LinkManagerRulesCache GetCache(Sitecore.Data.Database db, string siteName)
        {
            Assert.IsNotNull(db, "Database (db) cannot be null.");

            var cacheKey = $"{siteName}_{db.Name}";

            if (Caches.ContainsKey(cacheKey))
            {
                return Caches[cacheKey];
            }

            var cache = new LinkManagerRulesCache(db, siteName);
            Caches.Add(cacheKey, cache);

            return cache;
        }
    }
}
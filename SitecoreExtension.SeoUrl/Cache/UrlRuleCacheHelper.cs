using System.Collections.Generic;
using Sitecore.Diagnostics;

namespace SitecoreExtension.SeoUrl.Cache
{
    public static class UrlRuleCacheHelper
    {
        private static Dictionary<string, RulesCache> Caches = new Dictionary<string, RulesCache>();

        public static RulesCache GetCache(Sitecore.Data.Database db, string siteName)
        {
            Assert.IsNotNull(db, "Database (db) cannot be null.");

            var cacheKey = $"{siteName}_{db.Name}";

            if (Caches.ContainsKey(cacheKey))
            {
                return Caches[cacheKey];
            }

            var cache = new RulesCache(db, siteName);
            Caches.Add(cacheKey, cache);

            return cache;
        }
    }
}
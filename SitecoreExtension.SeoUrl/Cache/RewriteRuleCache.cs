using Sitecore.Caching;

namespace SitecoreExtension.SeoUrl.Cache
{
    public class RewriteRuleCache : CustomCache
    {
        public RewriteRuleCache(string name, long maxSize) : base(name, maxSize)
        {
        }

        public RewriteRuleResourceFile Get(string cacheKey)
        {
            return (RewriteRuleResourceFile)this.GetObject(cacheKey);
        }

        public void Set(string cacheKey, RewriteRuleResourceFile value)
        {
            this.SetObject(cacheKey, value);
        }
    }
}
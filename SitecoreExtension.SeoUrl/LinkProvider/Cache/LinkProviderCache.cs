using Sitecore.Caching;

namespace SitecoreExtension.SeoUrl.LinkProvider.Cache
{
    public class LinkProviderCache : CustomCache
    {
        public LinkProviderCache(string name, long maxSize) : base(name, maxSize)
        {
        }

        public LinkProviderResourceFile Get(string cacheKey)
        {
            return (LinkProviderResourceFile)this.GetObject(cacheKey);
        }

        public void Set(string cacheKey, LinkProviderResourceFile value)
        {
            this.SetObject(cacheKey, value);
        }
    }
}
using Sitecore.Caching;
using Sitecore.Data.Items;

namespace SitecoreExtension.SeoUrl.Cache
{
    public class RewriteRuleResourceFile : ICacheable
    {
        public Item TargetItem { get; set; }

        public long GetDataLength()
        {
            if (this.TargetItem != null)
            {
                return 1;
            }

            return 0;
        }

        public bool Cacheable { get; set; }

        public bool Immutable { get; }

        public event DataLengthChangedDelegate DataLengthChanged;
    }
}
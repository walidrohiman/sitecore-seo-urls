using System.Linq;
using Sitecore.Caching;

namespace SitecoreExtension.SeoUrl.LinkProvider.Cache
{
    public class LinkProviderResourceFile : ICacheable
    {
        public string TargetLink { get; set; }

        public long GetDataLength()
        {
            if (this.TargetLink != null && this.TargetLink.Any())
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
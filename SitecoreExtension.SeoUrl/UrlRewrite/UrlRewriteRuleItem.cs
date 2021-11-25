using Sitecore.Data.Fields;
using Sitecore.Data.Items;

namespace SitecoreExtension.SeoUrl.UrlRewrite
{
    public class UrlRewriteRuleItem : RewriteRuleItem
    {
        public UrlRewriteRuleItem(Item item) : base(item)
        {
            if (item.Fields["Is Display"] != null)
            {
                CheckboxField checkDisplayName = item.Fields["Is Display"];
                this.IsDisplayName = checkDisplayName.Checked;
            }
            else
            {
                this.IsDisplayName = false;
            }
        }

        public bool IsDisplayName { get; }
    }
}

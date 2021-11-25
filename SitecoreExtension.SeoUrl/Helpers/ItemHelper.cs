using Sitecore.Data.Items;

namespace SitecoreExtension.SeoUrl.Helpers
{
    public static class ItemHelper
    {
        public static string GetFieldValue(BaseItem item, string fieldName)
        {
            return item[fieldName] ?? string.Empty;
        }
    }
}

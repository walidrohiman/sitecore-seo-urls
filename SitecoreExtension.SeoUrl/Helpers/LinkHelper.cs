using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Links;
using Sitecore.Links.UrlBuilders;
using Sitecore.Mvc.Extensions;
using Sitecore.Sites;

namespace SitecoreExtension.SeoUrl.Helpers
{
    public static class LinkHelper
    {
        public static bool IgnoreUrlWithPathAndQueryString()
        {
            if (HttpContext.Current != null)
            {
                if (Context.PageMode.IsExperienceEditor || Context.PageMode.IsPreview)
                {
                    return true;
                }

                return LinksToIgnoreByCustomProcessors.Any(ignoreLink =>
                    HttpContext.Current.Request.Url.PathAndQuery.ToUpperInvariant().Contains(ignoreLink) ||
                    HttpContext.Current.Request.Url.AbsolutePath.ToUpperInvariant().Contains(ignoreLink));
            }

            return false;
        }

        public static readonly List<string> LinksToIgnoreByCustomProcessors = (Settings.GetSetting(nameof(LinksToIgnoreByCustomProcessors), string.Empty).ToUpperInvariant().Split(new char[]
        {
            ','
        }, StringSplitOptions.RemoveEmptyEntries)).ToList();

        public static Item GetInternalItem(SiteContext siteContext, Uri url)
        {
            var homePath = siteContext.StartPath;

            if (!homePath.EndsWith("/"))
            {
                homePath += "/";
            }

            var itemPath = MainUtil.DecodeName(url.AbsolutePath);

            if (itemPath.StartsWith(siteContext.VirtualFolder))
            {
                itemPath = itemPath.Remove(0, siteContext.VirtualFolder.Length);
            }

            var item = siteContext.Database.GetItem($"{homePath}{itemPath}");

            return item;
        }

        public static bool IsInternalLink(string link)
        {
            if (!link.IsEmptyOrNull())
            {
                var externalStartPath = "http://|https://".Split('|');
                return !externalStartPath.Any(ext => link.StartsWith(ext, StringComparison.InvariantCultureIgnoreCase));
            }

            return false;
        }

        public static string GetProtocol()
        {
            var siteScheme = SiteContext.Current.Properties["Schema"];

            return siteScheme.IsEmptyOrNull() ? Uri.UriSchemeHttps : siteScheme;
        }

        public static readonly List<string> SitesToIgnoreByCustomProcessors = ((IEnumerable<string>)Settings.GetSetting(nameof(SitesToIgnoreByCustomProcessors), string.Empty).ToUpperInvariant().Split(new char[1]
        {
            ','
        }, StringSplitOptions.RemoveEmptyEntries)).ToList<string>();

        public static string GetItemUrl(Item item, SiteContext siteContext)
        {
            var url = LinkManager.GetItemUrl(item, GetUrlOptions(siteContext));

            if (IsAbsoluteUrl(url))
            {
                var uri = new Uri(url);
                return uri.AbsolutePath;
            }

            return url;
        }

        public static string GetItemUrl(Item item)
        {
            return GetItemUrl(item, SiteContext.Current);
        }

        public static bool IsAbsoluteUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }

        public static ItemUrlBuilderOptions GetUrlOptions(SiteContext siteContext)
        {
            if (siteContext == null)
            {
                siteContext = SiteContext.Current;
            }
            var urlOptions = new ItemUrlBuilderOptions
            {
                AlwaysIncludeServerUrl = false,
                LanguageEmbedding = LanguageEmbedding.Never,
                UseDisplayName = true,
                Site = siteContext,
                EncodeNames = true,
                LowercaseUrls = true

            };
            if (siteContext != null)
            {
                urlOptions.Language = Language.Parse(siteContext.Language);
            }
            return urlOptions;
        }
    }
}

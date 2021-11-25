using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Caching;
using System.Web;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.HttpRequest;
using Sitecore.Sites;
using Sitecore.Web;
using SitecoreExtension.SeoUrl.Helpers;
using CacheItemPriority = System.Runtime.Caching.CacheItemPriority;

namespace SitecoreExtension.SeoUrl.UrlRewrite
{
    public class UrlNormalizer : HttpRequestProcessor
    {
        #region Properties

        private const string CacheKeyPrefix = "Capital-Url-Redirect_{0}_{1}_{2}";

        private Dictionary<string, string> _map;

        private Uri Uri { get; set; }

        #endregion

        #region Methods

        protected bool HasUpperCase(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            foreach (var t in url)
            {
                if (char.IsUpper(t))
                {
                    return true;
                }
            }

            return false;
        }

        public override void Process(HttpRequestArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            Uri = args.HttpContext.Request.Url;

            if (Uri != null)
            {
                var requestedUrl = $"{LinkHelper.GetProtocol()}://{HttpUtility.UrlDecode(Uri.Host)}{HttpUtility.UrlDecode(args.HttpContext.Request.RawUrl)}";

                var rawUrl = WebUtil.RemoveQueryString(requestedUrl);

                if (Context.Site == null ||
                    LinkHelper.SitesToIgnoreByCustomProcessors.Contains(Context.Site.Name.ToLowerInvariant()) ||
                    LinkHelper.IgnoreUrlWithPathAndQueryString() || rawUrl.Contains("asmx") || rawUrl.Contains("svc"))
                {
                    return;
                }

                if (!Context.PageMode.IsNormal)
                {
                    return;
                }

                if (!HasUpperCase(rawUrl))
                {
                    return;
                }

                ObjectCache cache = MemoryCache.Default;

                var config = new CacheItemPolicy
                {
                    AbsoluteExpiration = DateTime.Now.AddHours(1),
                    Priority = CacheItemPriority.Default,
                    SlidingExpiration = TimeSpan.Zero
                };

                Sanitizer(rawUrl, Context.Site, config, cache);
            }
        }

        private void Sanitizer(string source, SiteContext context, CacheItemPolicy config, ObjectCache cache)
        {
            _map = new Dictionary<string, string>();
            bool found = false;

            // Lookup Cache registry
            var lstRawUrlLowerVersionCacheKey = string.Format(CacheKeyPrefix, context.SiteInfo.Name,
                context.SiteInfo.Language, "lstRawUrlLowerVersion");

            string altUrl = null;

            if (cache.Contains(lstRawUrlLowerVersionCacheKey))
            {
                _map = (Dictionary<string, string>)cache.Get(lstRawUrlLowerVersionCacheKey);
                found = _map.TryGetValue(source, out altUrl);
            }

            if (found)
            {
                string appendQuery = string.Concat(altUrl, Uri.Query);
                Redirect(appendQuery);
            }
            else
            {
                if (!string.IsNullOrEmpty(source))
                {
                    string redirectUrl = source.ToLower().Trim();

                    if (!_map.ContainsKey(source))
                    {
                        _map.Add(source, redirectUrl);
                    }

                    if (_map.Count > 0)
                    {
                        cache.Add(new CacheItem(lstRawUrlLowerVersionCacheKey, _map), config);
                    }

                    // Add Query string before redirection
                    string appendQuery = string.Concat(redirectUrl, Uri.Query);
                    Redirect(appendQuery);
                }
            }
        }

        private static void Redirect(string url)
        {
            var context = HttpContext.Current;

            context.Response.Clear();
            context.Response.StatusCode = (int)HttpStatusCode.MovedPermanently;
            context.Response.RedirectLocation = url;
            context.Response.AddHeader("Location", url);
        }

        #endregion
    }
}

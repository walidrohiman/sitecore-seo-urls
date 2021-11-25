using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Sitecore;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.HttpRequest;
using Sitecore.SecurityModel;
using Sitecore.Web;
using SitecoreExtension.SeoUrl.Cache;
using SitecoreExtension.SeoUrl.Helpers;

namespace SitecoreExtension.SeoUrl.UrlRewrite
{
    public class UrlRewriteProcessor : HttpRequestProcessor
    {
        private static RewriteRuleCache _rewriteRuleCache;

        public static RewriteRuleCache RewriteRuleCache
        {
            get
            {
                if (_rewriteRuleCache != null)
                {
                    return _rewriteRuleCache;
                }

                RewriteRuleCache = new RewriteRuleCache($"[Rewrite Rule] {Context.Site.Name}-{Context.Language}", StringUtil.ParseSizeString("2MB"));

                return _rewriteRuleCache;
            }
            set => _rewriteRuleCache = value;
        }

        public override void Process(HttpRequestArgs args)
        {
            var db = Context.Database;

            var url = HttpContext.Current.Request.Url;

            var siteContext = Context.Site;

            try
            {
                var httpContext = args.HttpContext;

                if (httpContext.Request.Url == null)
                {
                    return;
                }

                if (args.HttpContext == null || db == null || Context.Site == null || (LinkHelper.IgnoreUrlWithPathAndQueryString() && !httpContext.Request.Url.LocalPath.Contains("sitecore/api/layout/render/jss")))
                {
                    return;
                }

                var localPath = httpContext.Request.Url.LocalPath;

                var itemFound = false;

                if (localPath.Contains("sitecore/api/layout/render/jss"))
                {
                    localPath = httpContext.Request.QueryString["item"];
                }

                // Check if item is in RewriteRule cache
                var key = $"CustomRewriteRule-{Context.Site.Name}-{Context.Site.Language}-{localPath}";

                if (RewriteRuleCache.Get(key) != null)
                {
                    var targetItem = RewriteRuleCache.Get(key).TargetItem;

                    if (targetItem != null)
                    {
                        Context.Item = targetItem;
                        itemFound = true;
                    }
                }

                if (!itemFound)
                {
                    var rules = GetInboundRules(db, Context.Site.SiteInfo);

                    foreach (var rewriteRule in rules)
                    {
                        if (string.IsNullOrWhiteSpace(rewriteRule.Path) || string.IsNullOrWhiteSpace(rewriteRule.Pattern))
                        {
                            continue;
                        }

                        var path = rewriteRule.Path;

                        var r = new Regex(rewriteRule.Pattern, RegexOptions.IgnoreCase);

                        var m = r.Match(localPath);

                        if (m.Success)
                        {
                            if (m.Groups.Count > 0)
                            {
                                var groupNames = r.GetGroupNames();

                                path = groupNames.Aggregate(path, (current, groupName) => current.Replace($"{{{groupName}}}", m.Groups[groupName].Value));
                            }

                            //check if content has been created with dash
                            var fullPath = $"{Context.Site.RootPath}{Context.Site.SiteInfo.StartItem}{path}";

                            var targetItem = Context.Database.GetItem(fullPath);

                            if (targetItem != null)
                            {
                                Context.Item = targetItem;
                                itemFound = true;

                                RewriteRuleCache.Set(key, new RewriteRuleResourceFile() { TargetItem = targetItem });

                                break;
                            }
                            else
                            {
                                //check if content has been created without dash
                                fullPath = $"{Context.Site.RootPath}{Context.Site.SiteInfo.StartItem}{path.Replace("-", " ")}";

                                targetItem = Context.Database.GetItem(fullPath);

                                if (targetItem != null)
                                {
                                    Context.Item = targetItem;
                                    itemFound = true;

                                    RewriteRuleCache.Set(key, new RewriteRuleResourceFile() { TargetItem = targetItem });

                                    break;
                                }
                            }
                        }
                    }
                }

                if (itemFound)
                {
                    return;
                }

                var internalItem = LinkHelper.GetInternalItem(siteContext, url);

                if (internalItem != null)
                {
                    Context.Item = internalItem;
                    RewriteRuleCache.Set(key, new RewriteRuleResourceFile() { TargetItem = internalItem });
                }
            }
            catch (Exception ex)
            {
                Log.Info($"UrlRewriteProcessor exception: {ex.Message}. StackTrace: {ex.StackTrace}. Inner Exception: {ex.InnerException}", this);
            }
        }

        private List<UrlRewriteRuleItem> GetInboundRules(Database db, SiteInfo site)
        {
            var cache = UrlRuleCacheHelper.GetCache(db, site.Name);
            var inboundRules = cache.GetInboundRules();

            if (inboundRules != null)
            {
                return inboundRules;
            }

            using (new SecurityDisabler())
            {
                var rulesEngine = new RulesEngine(db, site);
                inboundRules = rulesEngine.GetCachedInboundRules();
            }

            return inboundRules;
        }
    }
}

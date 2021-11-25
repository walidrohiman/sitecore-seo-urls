using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Pipelines.HttpRequest;
using Sitecore.SecurityModel;
using Sitecore.StringExtensions;
using SitecoreExtension.SeoUrl.Helpers;
using SitecoreExtension.SeoUrl.UrlRedirect.Context;
using SitecoreExtension.SeoUrl.UrlRedirect.Model;
using SitecoreExtension.SeoUrl.UrlRewrite;
using Action = SitecoreExtension.SeoUrl.UrlRedirect.Model.Action;
using Match = SitecoreExtension.SeoUrl.UrlRedirect.Model.Match;

namespace SitecoreExtension.SeoUrl.UrlRedirect
{
    public class RedirectProcessor : HttpRequestProcessor
    {
        private readonly string _redirectionPath = Settings.GetSetting("redirectionRules");

        private const string RequestedUrlFieldName = "Input URL";

        private const string TemporaryRedirect = "{BE6FF963-C06A-4A03-9BA0-7489A9D7DF2B}";
        
        private static readonly ConcurrentDictionary<string, Redirection> RedirectionCache = new ConcurrentDictionary<string, Redirection>();

        public override void Process(HttpRequestArgs args)
        {
            try
            {
                Assert.ArgumentNotNull(args, "args");

                var httpContext = new HttpContextWrapper(HttpContext.Current);

                if (httpContext.Request.Url == null)
                {
                    return;
                }

                var localPath = httpContext.Request.Url.LocalPath;
                var isWwwDomain = httpContext.Request.Url.ToString().ToLower().Contains("www");
                var isHttps = httpContext.Request.Url.ToString().ToLower().Contains("https");

                var rewriteKey = $"CustomRewriteRule-{Sitecore.Context.Site.Name}-{Sitecore.Context.Site.Language}-{localPath}";

                if (UrlRewriteProcessor.RewriteRuleCache.Get(rewriteKey) != null && isWwwDomain && isHttps)
                {
                    return;
                }

                if (Sitecore.Context.Site == null || LinkHelper.SitesToIgnoreByCustomProcessors.Contains(Sitecore.Context.Site.Name.ToLowerInvariant()) || LinkHelper.IgnoreUrlWithPathAndQueryString() || LinkHelper.IgnoreUrlWithPathAndQueryString()
                    || HttpContext.Current.Request.RawUrl.Contains("asmx"))
                {
                    return;
                }

                RedirectContext currentContext = new RedirectContext();

                var UriScheme = "https";

                currentContext.CurrentDb = Sitecore.Context.Database;
                currentContext.SiteStartPath = Sitecore.Context.Site.RootPath;
                currentContext.Language = Sitecore.Context.Language;

                if (currentContext.CurrentDb != null && HttpContext.Current != null
                                                     && !string.IsNullOrWhiteSpace(currentContext.SiteStartPath))
                {
                    currentContext.ServerVariables = HttpContext.Current.Request.ServerVariables;

                    string tempUrl = string.Format("{0}://{1}{2}",
                        !UriScheme.IsNullOrEmpty() ? UriScheme : Uri.UriSchemeHttps,
                        HttpUtility.UrlDecode(HttpContext.Current.Request.ServerVariables["HTTP_HOST"]),
                        HttpUtility.UrlDecode(HttpContext.Current.Request.RawUrl));

                    UriBuilder tempBuilder = new UriBuilder(tempUrl);
                    currentContext.InputUrl = tempBuilder.Uri;

                    if (DoesSiteHaveRedirectionRules(currentContext))
                    {
                        Dictionary<string, string> routingProperties = this.EvaluateRules(currentContext);
                        if (routingProperties != null)
                        {
                            RouteRequest(routingProperties, args);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.StackTrace, this);
            }
        }


        private Dictionary<string, string> EvaluateRules(RedirectContext currentContext)
        {
            Dictionary<string, string> routingProperties = null;
            routingProperties = EvaluateMatch(routingProperties, currentContext);
            return routingProperties;
        }

        private Dictionary<string, string> EvaluateMatch(Dictionary<string, string> routingProperties, RedirectContext currentContext)
        {
            Redirection redirectionRules = this.GetRedirectionRules(currentContext);
            if (redirectionRules != null && redirectionRules.Rules.Count > 0)
            {
                currentContext.RedirectionRules = redirectionRules.Rules;
                routingProperties = this.GetMatchedPatternDetails(routingProperties, currentContext);
            }
            return routingProperties;
        }

        private Redirection GetRedirectionRules(RedirectContext currentContext)
        {
            Redirection redirection;

            if (string.IsNullOrWhiteSpace(currentContext.SiteStartPath))
            {
                return null;
            }

            redirection = this.GetRulesFromCache(currentContext);

            if (redirection != null && redirection.Rules.Count > 0)
            {
                return redirection;
            }

            this.SetSiteSettingsAndSiteRoot(currentContext);

            if (redirection == null)
            {
                Redirection redirectionRules = new Redirection();

                this.PopulateSitecoreRedirectionRules(currentContext, redirectionRules);

                if (redirectionRules.Rules.Count > 0)
                {
                    redirection = redirectionRules;
                    this.SaveRulesToCache(currentContext, redirection);
                }
            }

            return redirection;
        }

        private Redirection GetRulesFromCache(RedirectContext currentContext)
        {
            Redirection redirection = null;
            string cacheKey = $"{currentContext.SiteStartPath}_{currentContext.Language.Name}_{currentContext.DeviceId}";
            if (RedirectionCache.ContainsKey(cacheKey))
            {
                redirection = RedirectionCache[cacheKey];
            }
            return redirection;
        }

        private void SaveRulesToCache(RedirectContext currentContext, Redirection redirection)
        {
            string cacheKey = $"{currentContext.SiteStartPath}_{currentContext.Language.Name}_{currentContext.DeviceId}";
            if (redirection != null && redirection.Rules.Count > 0)
            {
                RedirectionCache[cacheKey] = redirection;
            }
        }

        private void SetSiteSettingsAndSiteRoot(RedirectContext currentContext)
        {
            if (!string.IsNullOrWhiteSpace(currentContext.SiteStartPath))
            {
                currentContext.SiteRoot = currentContext.SiteRoot ?? this.GetItemWithLogging(Sitecore.Context.Database, currentContext.SiteStartPath, currentContext.Language);
            }
        }

        private void PopulateSitecoreRedirectionRules(RedirectContext currentContext, Redirection redirectionRules)
        {
            IEnumerable<Item> redirects = this.GetRedirects(currentContext);
            if (redirects != null && redirects.Any())
            {
                var rules = from redirectionRule in redirects
                            where redirectionRule != null
                            select
                            new Rule
                            {
                                Name = redirectionRule.ID.ToString(),
                                Match = new Match()
                                {
                                    Url = redirectionRule[RequestedUrlFieldName]
                                },
                                Action = new Action
                                {
                                    TypeAsString = Enum.GetName(typeof(RoutingMode), RoutingMode.Redirect),
                                    RedirectTypeAsString =
                                            (ItemHelper.GetFieldValue(redirectionRule, "Redirection Type") != null
                                             && (ItemHelper.GetFieldValue(redirectionRule, "Redirection Type") == TemporaryRedirect)
                                             || ItemHelper.GetFieldValue(redirectionRule, "Redirection Type") == "Temporary (302)")
                                                ? RedirectionType.Temporary.ToString()
                                                : RedirectionType.Permanent.ToString(),
                                    Url = ItemHelper.GetFieldValue(redirectionRule, "Target URL")
                                }
                            };

                if (rules.Any())
                {
                    redirectionRules.Rules.AddRange(rules);
                }
            }
        }

        private bool DoesSiteHaveRedirectionRules(RedirectContext currentContext)
        {
            if (currentContext == null)
            {
                return false;
            }

            Redirection redirectionRules = GetRedirectionRules(currentContext);
            if (redirectionRules != null && redirectionRules.Rules.Count > 0)
            {
                return true;
            }

            return false;
        }

        private IEnumerable<Item> GetRedirects(RedirectContext currentContext)
        {
            IEnumerable<Item> returnData = null;

            Item redirectFolderRoot = this.GetRulesFolderItem(currentContext);
            if (redirectFolderRoot != null)
            {
                using (new SecurityDisabler())
                {
                    returnData = redirectFolderRoot.GetChildren(ChildListOptions.SkipSorting).Where(i => i.Versions != null && i.Versions.Count > 0);
                }
            }

            return returnData;
        }

        private Item GetRulesFolderItem(RedirectContext currentContext)
        {
            Item subFolderItem = null;
            string redirectionRulesRootPath = _redirectionPath;
            if (!string.IsNullOrEmpty(redirectionRulesRootPath))
            {
                subFolderItem = this.GetItemWithLogging(Sitecore.Context.Database, redirectionRulesRootPath, currentContext.Language);
            }

            return subFolderItem;
        }

        public Item GetItemWithLogging(Database db, string path, Language language)
        {
            Item item = db.GetItem(path, language);
            return item;
        }

        private void RouteRequest(Dictionary<string, string> routingProperties, HttpRequestArgs args)
        {
            if (!string.IsNullOrWhiteSpace(routingProperties["RedirectURL"]))
            {
                var redirectUrl = string.Concat(routingProperties["RedirectURL"], routingProperties["QueryString"]);

                var url = LinkHelper.IsInternalLink(redirectUrl)
                              ? string.Format(
                                  "{0}://{1}{2}",
                                  LinkHelper.GetProtocol(),
                                  HttpUtility.UrlDecode(HttpContext.Current.Request.ServerVariables["HTTP_HOST"]),
                                  redirectUrl)
                              : redirectUrl;

                RedirectionType redirectType;

                if (!string.IsNullOrWhiteSpace(routingProperties["RedirectionType"])
                    && Enum.TryParse(routingProperties["RedirectionType"], out redirectType)
                    && redirectType == RedirectionType.Temporary)
                {
                    try
                    {
                        HttpContext.Current.Response.Redirect(url, false);
                        HttpContext.Current.ApplicationInstance.CompleteRequest();
                    }
                    catch (ThreadAbortException)
                    {
                    }
                    args.AbortPipeline();
                }
                else
                {
                    try
                    {
                        HttpContext.Current.Response.RedirectPermanent(url, false);
                        HttpContext.Current.ApplicationInstance.CompleteRequest();
                    }
                    catch (ThreadAbortException)
                    {
                    }

                    args.AbortPipeline();
                }
            }
        }

        private Dictionary<string, string> GetMatchedPatternDetails(Dictionary<string, string> routingProperties, RedirectContext currentContext)
        {
            foreach (var possibleRedirectPattern in currentContext.RedirectionRules)
            {
                var redirectPath = string.Empty;
                if (possibleRedirectPattern.Match == null || possibleRedirectPattern.Action == null)
                {
                    return routingProperties;
                }
                string matchPattern = possibleRedirectPattern.Match.Url;

                if (!string.IsNullOrWhiteSpace(matchPattern))
                {
                    string requestedPath = HttpUtility.UrlDecode(this.StripLanguage(currentContext.InputUrl.AbsolutePath));
                    string requestedPathAndQuery = HttpUtility.UrlDecode(currentContext.InputUrl.PathAndQuery);
                    string requestedRawUrl = HttpUtility.UrlDecode(currentContext.InputUrl.PathAndQuery);
                    string requestedUrl = HttpUtility.UrlDecode(string.Concat(currentContext.InputUrl.Scheme, "://", currentContext.InputUrl.Host, requestedRawUrl));

                    string requestedRawUrlDomainAppended = HttpUtility.UrlDecode(currentContext.InputUrl.AbsoluteUri);
                    string requestedPathWithCulture = HttpUtility.UrlDecode(currentContext.InputUrl.AbsolutePath);

                    string finalRequestedURL = Regex.IsMatch(requestedPathAndQuery, matchPattern.Trim(), RegexOptions.IgnoreCase)
                                                   ? requestedPathAndQuery
                                                   : Regex.IsMatch(requestedPath, matchPattern.Trim(), RegexOptions.IgnoreCase)
                                                       ? requestedPath
                                                       : Regex.IsMatch(requestedPathWithCulture, matchPattern.Trim(), RegexOptions.IgnoreCase)
                                                           ? requestedPathWithCulture
                                                           : Regex.IsMatch(requestedRawUrl, matchPattern.Trim(), RegexOptions.IgnoreCase)
                                                               ? requestedRawUrl
                                                               : Regex.IsMatch(requestedUrl, matchPattern.Trim(), RegexOptions.IgnoreCase)
                                                                   ? requestedRawUrlDomainAppended
                                                                   : string.Empty;

                    if (!string.IsNullOrWhiteSpace(finalRequestedURL))
                    {
                        redirectPath = Regex.Replace(finalRequestedURL, matchPattern, possibleRedirectPattern.Action.Url, RegexOptions.IgnoreCase);

                        if ((!string.IsNullOrWhiteSpace(currentContext.InputUrl.Query)) && !redirectPath.Contains("?"))
                        {
                            redirectPath = string.Concat(redirectPath, currentContext.InputUrl.Query);
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(redirectPath) && possibleRedirectPattern.Conditions != null
                    && possibleRedirectPattern.Conditions.Count > 0 && currentContext.ServerVariables != null)
                {
                    foreach (var condition in possibleRedirectPattern.Conditions)
                    {
                        if (condition != null && !string.IsNullOrWhiteSpace(condition.Input) && !string.IsNullOrWhiteSpace(condition.Pattern))
                        {
                            string servervariable = condition.Input.Replace("{", string.Empty).Replace("}", string.Empty).Trim();
                            string serverVariable = currentContext.ServerVariables[servervariable];
                            if (!string.IsNullOrWhiteSpace(serverVariable) && !string.IsNullOrWhiteSpace(condition.Pattern))
                            {
                                bool regexMatch = Regex.IsMatch(serverVariable, condition.Pattern);
                                if ((regexMatch && !condition.Negate) || (!regexMatch && condition.Negate))
                                {
                                    continue;
                                }
                                else
                                {
                                    redirectPath = string.Empty;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(redirectPath))
                {
                    continue;
                }

                var pathAndQuery = redirectPath.Split('?');
                string path = pathAndQuery[0];

                if (!string.IsNullOrWhiteSpace(path))
                {
                    string query = pathAndQuery.Length > 1 ? string.Concat("?", pathAndQuery[1]) : string.Empty;
                    routingProperties = new Dictionary<string, string>
                                            {
                                                    { "RedirectURL", path },
                                                    { "QueryString", query },
                                                    { "RedirectionType", possibleRedirectPattern.Action.RedirectType.ToString() },
                                                    { "MatchedRuleName", possibleRedirectPattern.Name }
                                            };
                    break;
                }
            }

            return routingProperties;
        }

        private string StripLanguage(string url)
        {
            string filePath = url;
            if (!string.IsNullOrWhiteSpace(url))
            {
                Language language = ExtractLanguage(url);
                if (language != null)
                {
                    filePath = url.Substring(language.Name.Length + 1);
                    if (!string.IsNullOrEmpty(filePath) && filePath.StartsWith(".", StringComparison.InvariantCulture)) filePath = url;
                }
            }
            return filePath;
        }

        private Language ExtractLanguage(string url)
        {
            string languageName = this.ExtractLanguageName(url);

            if (string.IsNullOrWhiteSpace(languageName))
            {
                return (Language)null;
            }

            var cultureArray = languageName.Split('-');

            if (cultureArray.Any(culture => culture.Length != 2))
            {
                return null;
            }

            Language result;
            if (!Language.TryParse(languageName, out result))
            {
                return (Language)null;
            }

            return result;
        }

        private string ExtractLanguageName(string localPath)
        {
            Assert.ArgumentNotNull((object)localPath, "localPath");

            if (string.IsNullOrEmpty(localPath) || !localPath.StartsWith("/", StringComparison.InvariantCulture)) return (string)null;

            int num = localPath.IndexOfAny(new char[4] { '/', '.', '?', '#' }, 1);

            if (num < 0) num = localPath.Length;

            return localPath.Substring(1, num - 1);
        }
    }
}
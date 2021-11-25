using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.IO;
using Sitecore.Links;
using Sitecore.Links.UrlBuilders;
using Sitecore.SecurityModel;
using Sitecore.Sites;
using Sitecore.StringExtensions;
using Sitecore.Web;
using SitecoreExtension.SeoUrl.LinkProvider.Cache;

namespace SitecoreExtension.SeoUrl.LinkProvider
{
    public class LinkProvider : ItemUrlBuilder
    {
        private const StringComparison InvariantCultureIgnoreCase = StringComparison.InvariantCultureIgnoreCase;

        private const StringComparison OrdinalIgnoreCase = StringComparison.OrdinalIgnoreCase;

        private static LinkProviderCache _linkProviderCache;

        public LinkProvider(DefaultItemUrlBuilderOptions defaultOptions) : base(defaultOptions) {}

        private static LinkProviderCache LinkProviderCache
        {
            get
            {
                if (_linkProviderCache != null)
                {
                    return _linkProviderCache;
                }

                LinkProviderCache = new LinkProviderCache($"[Link Provider] {Context.Site.Name}-{Sitecore.Context.Language}", StringUtil.ParseSizeString("2MB"));

                return _linkProviderCache;
            }

            set
            {
                _linkProviderCache = value;
            }
        }

        public override string Build(Item item, ItemUrlBuilderOptions options)
        {
            Assert.ArgumentNotNull(item, "item");
            Assert.ArgumentNotNull(options, "options");

            var itemUrl = this.CreateLinkBuilder(options).GetItemUrl(item);

            itemUrl = options.LowercaseUrls != null ? itemUrl.ToLowerInvariant() : itemUrl;
           
            if (!item.Paths.IsContentItem)
            {
                return itemUrl;
            }

            SiteInfo siteInfo = SiteContext.Current.SiteInfo;

            if (siteInfo == null)
            {
                return itemUrl;
            }

            var cache = LinkManagerRulesCacheManager.GetCache(item.Database, siteInfo.Name);
            var inboundRules = cache.GetInboundRules();

            if (inboundRules == null)
            {
                using (new SecurityDisabler())
                {
                    var linkManagerRulesEngine = new LinkManagerRulesEngine(item.Database, siteInfo);
                    inboundRules = linkManagerRulesEngine.GetCachedInboundRules();
                }
            }

            if (inboundRules == null)
            {
                return itemUrl;
            }

            var url = itemUrl;

            var key = $"LinkProviderCache-{siteInfo.Name}-{siteInfo.Language}-{itemUrl}";

            if (LinkProviderCache.Get(key) != null)
            {
                var targetLink = LinkProviderCache.Get(key).TargetLink;

                if (targetLink != null)
                {
                    url = targetLink;
                    return url;
                }
            }

            foreach (var linkManagerRule in inboundRules)
            {
                if (string.IsNullOrWhiteSpace(linkManagerRule.Path) || string.IsNullOrWhiteSpace(linkManagerRule.Pattern))
                {
                    continue;
                }

                var pattern = linkManagerRule.Pattern;

                var ruleRegex = new Regex(pattern, RegexOptions.IgnoreCase);

                var match = ruleRegex.Match(itemUrl);
                if (!match.Success || match.Groups.Count <= 0)
                {
                    continue;
                }

                var path = linkManagerRule.Path;

                var groupNames = ruleRegex.GetGroupNames();
                foreach (var groupName in groupNames.Skip(1))
                {
                    path = path.Replace($"{{{groupName}}}", match.Groups[groupName].Value);
                }

                LinkProviderCache.Set(key, new LinkProviderResourceFile { TargetLink = path });

                return path;
            }

            return url;
        }

        protected CustomLinkBuilder CreateLinkBuilder(UrlOptions options)
        {
            return new CustomLinkBuilder(options);
        }

        public class CustomLinkBuilder
        {
            private static readonly object SyncRoot = new object();

            private static Dictionary<SiteKey, SiteInfo> SiteResolvingTable;

            private static List<SiteInfo> Sites;

            private readonly UrlOptions options;

            public CustomLinkBuilder(UrlOptions options)
            {
                Assert.ArgumentNotNull(options, "options");
                this.options = options;
            }

            private delegate SiteKey KeyGetter(SiteInfo siteInfo);

            public string GetItemUrl(Item item)
            {
                Assert.ArgumentNotNull(item, "item");
                return this.BuildItemUrl(item);
            }

            protected static SiteKey BuildKey(string path, string language)
            {
                Assert.ArgumentNotNull(path, "path");
                Assert.ArgumentNotNull(language, "language");
                if (!Settings.Rendering.SiteResolvingMatchCurrentLanguage)
                {
                    language = string.Empty;
                }
                else if (string.IsNullOrEmpty(language) && LanguageManager.DefaultLanguage != null)
                {
                    language = LanguageManager.DefaultLanguage.Name;
                }

                return new SiteKey(path.ToLowerInvariant(), language);
            }

            protected static SiteInfo FindMatchingSite(Dictionary<SiteKey, SiteInfo> resolvingTable, SiteKey key)
            {
                Assert.ArgumentNotNull(resolvingTable, "resolvingTable");
                Assert.ArgumentNotNull(key, "key");

                if (key.Language.Length == 0)
                {
                    return FindMatchingSiteByPath(resolvingTable, key.Path);
                }

                while (!resolvingTable.ContainsKey(key))
                {
                    var length = key.Path.LastIndexOf("/", StringComparison.InvariantCulture);
                    if (length <= 1)
                    {
                        return null;
                    }

                    key = BuildKey(key.Path.Substring(0, length), key.Language);
                }

                return resolvingTable[key];
            }

            protected static SiteInfo FindMatchingSiteByPath(Dictionary<SiteKey, SiteInfo> resolvingTable, string path)
            {
                Assert.ArgumentNotNull(resolvingTable, "resolvingTable");
                Assert.ArgumentNotNull(path, "path");

                while (true)
                {
                    foreach (var siteKey in resolvingTable.Keys)
                    {
                        var siteInfo = resolvingTable[siteKey];
                        if (siteKey.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return siteInfo;
                        }
                    }

                    var length = path.LastIndexOf("/", StringComparison.InvariantCulture);
                    if (length > 1)
                    {
                        path = path.Substring(0, length);
                    }
                    else
                    {
                        break;
                    }
                }

                return null;
            }

            protected virtual string BuildItemUrl(Item item)
            {
                Assert.ArgumentNotNull(item, "item");
                var siteInfo = this.ResolveTargetSite(item);
                var itemPathElement = this.GetItemPathElement(item, siteInfo);
                if (itemPathElement.Length == 0)
                {
                    return string.Empty;
                }

                return this.BuildItemUrl(itemPathElement);
            }

            protected virtual string BuildItemUrl(string itemPath)
            {
                Assert.ArgumentNotNull(itemPath, "itemPath");
                var path = itemPath;

                if (this.options.EncodeNames)
                {
                    path = MainUtil.EncodePath(path, '/');
                }

                return path;
            }

            protected Dictionary<SiteKey, SiteInfo> BuildSiteResolvingTable(List<SiteInfo> sitesList)
            {
                Assert.ArgumentNotNull(sitesList, "sitesList");

                var sitesByKey = new Dictionary<SiteKey, SiteInfo>();
                var keyGetterArray = new KeyGetter[]
                                         {
                                             info =>
                                             BuildKey(
                                                 FileUtil.MakePath(info.RootPath, info.StartItem).ToLowerInvariant(),
                                                 info.Language)
                                         };

                foreach (var keyGetter in keyGetterArray)
                {
                    foreach (var site in sitesList)
                    {
                        if (!this.SiteCantBeResolved(site))
                        {
                            var key = keyGetter(site);
                            if (!sitesByKey.ContainsKey(key))
                            {
                                sitesByKey.Add(key, site);
                            }
                        }
                    }
                }

                return sitesByKey;
            }

            protected virtual string GetItemPathElement(Item item, SiteInfo site)
            {
                var pathType = ItemPathType.Name;
                var itemPath = item.Paths.GetPath(pathType);

                if (site == null)
                {
                    return itemPath;
                }

                var rootPath1 = this.GetRootPath(site, item.Language, item.Database, true);
                if (this.IsDescendantOrSelfOf(itemPath.Trim(), rootPath1.Trim().TrimEnd('/')))
                {
                    itemPath = itemPath.Substring(rootPath1.Length);
                }
                else
                {
                    var rootPath2 = this.GetRootPath(site, item.Language, item.Database, false);
                    if (this.IsDescendantOrSelfOf(itemPath.Trim(), rootPath2.Trim().TrimEnd('/')))
                    {
                        itemPath = itemPath.Substring(rootPath2.Length);
                    }
                }

                var virtualFolder = site.VirtualFolder;
                if (virtualFolder.Length > 0 && !itemPath.StartsWith(virtualFolder, StringComparison.OrdinalIgnoreCase))
                {
                    itemPath = FileUtil.MakePath(virtualFolder, itemPath);
                }

                return itemPath;
            }

            protected virtual string GetRootPath(SiteInfo site, Language language, Database database, bool useStartItem)
            {
                var rootItemPath = useStartItem ? FileUtil.MakePath(site.RootPath, site.StartItem) : site.RootPath;

                if (rootItemPath.Length == 0)
                {
                    return string.Empty;
                }

                var rootItem = ItemManager.GetItem(
                    rootItemPath,
                    language,
                    Sitecore.Data.Version.Latest,
                    database,
                    SecurityCheck.Disable);

                return rootItem == null ? string.Empty : rootItem.Paths.GetPath(ItemPathType.DisplayName);
            }

            protected Dictionary<SiteKey, SiteInfo> GetSiteResolvingTable()
            {
                object sync = null;
                var siteList = SiteContextFactory.Sites;
                if (Sites != siteList)
                {
                    var lockTaken = false;
                    try
                    {
                        Monitor.Enter(sync = SyncRoot, ref lockTaken);
                        if (Sites != siteList)
                        {
                            Sites = siteList;
                            SiteResolvingTable = null;
                        }
                    }
                    finally
                    {
                        if (lockTaken)
                        {
                            Monitor.Exit(sync);
                        }
                    }
                }

                if (SiteResolvingTable == null)
                {
                    var lockTaken = false;
                    try
                    {
                        Monitor.Enter(sync = SyncRoot, ref lockTaken);
                        if (SiteResolvingTable == null)
                        {
                            SiteResolvingTable = this.BuildSiteResolvingTable(Sites);
                        }
                    }
                    finally
                    {
                        if (lockTaken)
                        {
                            Monitor.Exit(sync);
                        }
                    }
                }

                return SiteResolvingTable;
            }

            protected virtual string GetTargetHostName(SiteInfo siteInfo)
            {
                Assert.ArgumentNotNull(siteInfo, "siteInfo");

                if (!siteInfo.TargetHostName.IsNullOrEmpty())
                {
                    return siteInfo.TargetHostName;
                }

                var hostName = siteInfo.HostName;
                if (hostName.IndexOfAny(new[] { '*', '|' }) < 0)
                {
                    return hostName;
                }

                return string.Empty;
            }

            protected virtual bool IsDescendantOrSelfOf(string itemPath, string rootPath)
            {
                Assert.ArgumentNotNull(itemPath, "itemPath");
                Assert.ArgumentNotNull(rootPath, "rootPath");
                return !string.IsNullOrEmpty(rootPath) && itemPath.StartsWith(rootPath, OrdinalIgnoreCase)
                       && (string.Compare(itemPath, rootPath, OrdinalIgnoreCase) == 0
                           || itemPath.StartsWith(rootPath + "/", OrdinalIgnoreCase));
            }

            protected virtual bool MatchCurrentSite(Item item, SiteContext currentSite)
            {
                Assert.ArgumentNotNull(item, "item");
                Assert.ArgumentNotNull(currentSite, "currentSite");
                if (!Settings.Rendering.SiteResolvingMatchCurrentSite
                    || Settings.Rendering.SiteResolvingMatchCurrentLanguage
                    && !item.Language.ToString().Equals(currentSite.Language, InvariantCultureIgnoreCase))
                {
                    return false;
                }

                var fullPath = item.Paths.FullPath;
                var startPath = currentSite.StartPath;
                return fullPath.StartsWith(startPath, InvariantCultureIgnoreCase)
                       && (fullPath.Length <= startPath.Length || fullPath[startPath.Length] == '/');
            }

            public virtual SiteInfo ResolveTargetSite(Item item)
            {
                var currentSite = this.options.Site ?? Context.Site;
                if (this.options.SiteResolving && item.Database.Name != "core"
                    && (this.options.Site == null || Context.Site != null && this.options.Site.Name == Context.Site.Name)
                    && (currentSite == null || !this.MatchCurrentSite(item, currentSite)))
                {
                    var siteResolvingTable = this.GetSiteResolvingTable();
                    var lowerInvariant = item.Paths.FullPath.ToLowerInvariant();
                    var siteInfo = FindMatchingSite(
                        siteResolvingTable,
                        BuildKey(lowerInvariant, item.Language.ToString()))
                                   ?? FindMatchingSiteByPath(siteResolvingTable, lowerInvariant);
                    if (siteInfo != null)
                    {
                        return siteInfo;
                    }
                }

                return currentSite != null ? currentSite.SiteInfo : null;
            }

            protected virtual bool SiteCantBeResolved(SiteInfo siteInfo)
            {
                Assert.ArgumentNotNull(siteInfo, "siteInfo");
                if (!string.IsNullOrEmpty(siteInfo.HostName) && string.IsNullOrEmpty(this.GetTargetHostName(siteInfo)))
                {
                    Log.Warn(
                        "LinkBuilder. Site '{0}' should have defined 'targethostname' property in order to take participation in site resolving process."
                            .FormatWith(siteInfo.Name),
                        typeof(CustomLinkBuilder));
                    return true;
                }

                if (!string.IsNullOrEmpty(this.GetTargetHostName(siteInfo)) && !string.IsNullOrEmpty(siteInfo.RootPath))
                {
                    return string.IsNullOrEmpty(siteInfo.StartItem);
                }

                return true;
            }

            protected class SiteKey
            {
                public SiteKey(string path, string language)
                {
                    Assert.ArgumentNotNull(path, "path");
                    Assert.ArgumentNotNull(language, "language");
                    this.Path = path;
                    this.Language = language;
                }

                public string Language { get; }

                public string Path { get; }
            }
        }
    }
}
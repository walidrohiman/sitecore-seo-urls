using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Sitecore.Data;
using Sitecore.Globalization;
using SitecoreExtension.SeoUrl.UrlRedirect.Model;

namespace SitecoreExtension.SeoUrl.UrlRedirect.Context
{
    public class RedirectContext
    {
        public Sitecore.Data.Items.Item SiteRoot { get; set; }

        public string SiteStartPath { get; set; }

        public Language Language { get; set; }

        public string DeviceId { get; set; }

        public Database CurrentDb { get; set; }

        public Uri InputUrl { get; set; }

        public List<Rule> RedirectionRules { get; set; }

        public NameValueCollection ServerVariables { get; set; }

    }
}
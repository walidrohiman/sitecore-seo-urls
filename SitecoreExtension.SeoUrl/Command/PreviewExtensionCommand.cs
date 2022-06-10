using System.Collections.Specialized;
using Sitecore;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Links;
using Sitecore.Pipelines.HasPresentation;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Sites;
using Sitecore.Web.UI.Sheer;
using SitecoreExtension.SeoUrl.Helpers;

namespace SitecoreExtension.SeoUrl.Command
{
    public class PreviewExtensionCommand : Sitecore.Shell.Framework.Commands.Command
    {
        public override void Execute(CommandContext context)
        {
            Assert.ArgumentNotNull((object)context, nameof(context));
            if (context.Items.Length != 1)
            {
                return;
            }

            var obj = context.Items[0];

            Context.ClientPage.Start((object)this, "Run", new NameValueCollection()
            {
                ["uri"] = obj.Uri.ToString()
            });
        }

        protected void Run(ClientPipelineArgs args)
        {
            var obj = Database.GetItem(ItemUri.Parse(args.Parameters["uri"]));

            var previewSiteContext = LinkManager.GetPreviewSiteContext(obj);

            using (new SiteContextSwitcher(previewSiteContext))
            {
                using (new LanguageSwitcher(previewSiteContext.Language))
                {

                    var itemUrl = LinkHelper.GetItemUrl(Context.Database.GetItem(obj.ID));

                    var url = itemUrl.Equals("/") ? $"https://{Context.Site.TargetHostName}" : $"https://{Context.Site.TargetHostName}{itemUrl}";

                    SheerResponse.Eval("window.open('" + (object)url + "', '_blank');");
                }
            }
        }

        public override CommandState QueryState(CommandContext context)
        {
            var item = context.Items[0];

            if (item == null)
            {
                return CommandState.Hidden;
            }

            return !HasPresentationPipeline.Run(item) ? CommandState.Hidden : base.QueryState(context);
        }
    }
}
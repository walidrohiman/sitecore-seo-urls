using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;
using RazorGenerator.Mvc;
using SitecoreExtension.SeoUrl;

[assembly: WebActivatorEx.PostApplicationStartMethod(typeof(RazorGeneratorMvcStart), "Start")]

namespace SitecoreExtension.SeoUrl {
    public static class RazorGeneratorMvcStart {
        public static void Start() {
            var engine = new PrecompiledMvcEngine(typeof(RazorGeneratorMvcStart).Assembly) {
                UsePhysicalViewsIfNewer = HttpContext.Current.Request.IsLocal
            };

            ViewEngines.Engines.Insert(0, engine);

            // StartPage lookups are done by WebPages. 
            VirtualPathFactoryManager.RegisterVirtualPathFactory(engine);
        }
    }
}

using System.Web.Optimization;
using ExtNET.Web.Helpers.Bundling;


namespace ExtNET.Web
{
    public static class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new StyleBundle("~/Content/css").Include(
                "~/Content/site.css",
                "~/Content/ext.css"));

            var appBundle = new ScriptBundle("~/bundles/js").IncludeDirectory(
                "~/Scripts/app", "*.js", true);

            var excludedDependencies = new[]
            {
                "extjs"
            };

            appBundle.Orderer = new ScriptDependencyOrderer(excludedDependencies);
            bundles.Add(appBundle);

#if !DEBUG
            BundleTable.EnableOptimizations = true;
#endif
        }
    }
}
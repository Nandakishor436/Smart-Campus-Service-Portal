using System;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Smart_Campus_Service_Portal.Models;

namespace Smart_Campus_Service_Portal
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // Automatically initialize MongoDB collections, indexes, and seed demo accounts
            try
            {
                DatabaseSeeder.Seed();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Database seeding exception: {ex.Message}");
            }
        }
    }
}

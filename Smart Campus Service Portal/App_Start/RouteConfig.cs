using System.Web.Mvc;
using System.Web.Routing;

namespace Smart_Campus_Service_Portal
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // 1. API: Authentication routes
            routes.MapRoute(
                name: "ApiAuthRegister",
                url: "api/auth/register",
                defaults: new { controller = "Api", action = "AuthRegister" }
            );

            routes.MapRoute(
                name: "ApiAuthLogin",
                url: "api/auth/login",
                defaults: new { controller = "Api", action = "AuthLogin" }
            );

            routes.MapRoute(
                name: "ApiAuthLogout",
                url: "api/auth/logout",
                defaults: new { controller = "Api", action = "AuthLogout" }
            );

            routes.MapRoute(
                name: "ApiAuthMe",
                url: "api/auth/me",
                defaults: new { controller = "Api", action = "AuthMe" }
            );

            // 2. API: Students
            routes.MapRoute(
                name: "ApiStudents",
                url: "api/students/{id}",
                defaults: new { controller = "Api", action = "StudentsEndpoint", id = UrlParameter.Optional }
            );

            // 3. API: Faculty
            routes.MapRoute(
                name: "ApiFaculty",
                url: "api/faculty/{id}",
                defaults: new { controller = "Api", action = "FacultyEndpoint", id = UrlParameter.Optional }
            );

            // 4. API: Services
            routes.MapRoute(
                name: "ApiServices",
                url: "api/services/{id}",
                defaults: new { controller = "Api", action = "ServicesEndpoint", id = UrlParameter.Optional }
            );

            // 5. API: Service Requests
            routes.MapRoute(
                name: "ApiRequests",
                url: "api/requests/{id}",
                defaults: new { controller = "Api", action = "RequestsEndpoint", id = UrlParameter.Optional }
            );

            // 6. API: Complaints
            routes.MapRoute(
                name: "ApiComplaints",
                url: "api/complaints/{id}",
                defaults: new { controller = "Api", action = "ComplaintsEndpoint", id = UrlParameter.Optional }
            );

            // 7. API: Announcements
            routes.MapRoute(
                name: "ApiAnnouncements",
                url: "api/announcements/{id}",
                defaults: new { controller = "Api", action = "AnnouncementsEndpoint", id = UrlParameter.Optional }
            );

            // 8. API: Feedback
            routes.MapRoute(
                name: "ApiFeedback",
                url: "api/feedback",
                defaults: new { controller = "Api", action = "FeedbackEndpoint" }
            );

            // 9. API: Analytics
            routes.MapRoute(
                name: "ApiAnalytics",
                url: "api/analytics/dashboard",
                defaults: new { controller = "Api", action = "AnalyticsDashboard" }
            );

            // Default MVC Route
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}

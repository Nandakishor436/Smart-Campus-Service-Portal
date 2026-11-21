using System;
using System.Linq;
using System.Web.Mvc;
using MongoDB.Driver;
using Smart_Campus_Service_Portal.Helpers;
using Smart_Campus_Service_Portal.Models;

namespace Smart_Campus_Service_Portal.Controllers
{
    public class HomeController : Controller
    {
        private readonly MongoDbContext _db = MongoDbContext.Instance;

        public ActionResult Index()
        {
            // If user is already authenticated, allow quick navigation to their dashboard
            ViewBag.IsAuthenticated = AuthSessionHelper.IsAuthenticated;
            ViewBag.UserRole = AuthSessionHelper.CurrentUserRole;
            ViewBag.UserName = AuthSessionHelper.CurrentUserFullName;

            // Load live statistics from MongoDB
            ViewBag.TotalStudents = _db.Students.CountDocuments(FilterDefinition<Student>.Empty);
            ViewBag.TotalFaculty = _db.Faculty.CountDocuments(FilterDefinition<FacultyMember>.Empty);
            ViewBag.TotalServices = _db.Services.CountDocuments(s => s.IsActive);
            ViewBag.TotalRequests = _db.ServiceRequests.CountDocuments(FilterDefinition<ServiceRequest>.Empty);
            ViewBag.CompletedRequests = _db.ServiceRequests.CountDocuments(r => r.Status == "Completed");

            // Load featured active services
            var services = _db.Services.Find(s => s.IsActive).Limit(8).ToList();
            ViewBag.Services = services;

            // Load recent campus announcements
            var announcements = _db.Announcements
                .Find(a => a.IsActive && (a.ExpiryDate == null || a.ExpiryDate > DateTime.UtcNow))
                .SortByDescending(a => a.CreatedAt)
                .Limit(4)
                .ToList();
            ViewBag.Announcements = announcements;

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Title = "About Smart Campus Service Portal";
            ViewBag.TotalServices = _db.Services.CountDocuments(s => s.IsActive);
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Title = "Campus Contact Directory";
            return View();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using MongoDB.Driver;
using Smart_Campus_Service_Portal.Helpers;
using Smart_Campus_Service_Portal.Models;
using Smart_Campus_Service_Portal.Models.ViewModels;

namespace Smart_Campus_Service_Portal.Controllers
{
    [AuthorizeRole("Student")]
    public class StudentController : Controller
    {
        private readonly MongoDbContext _db = MongoDbContext.Instance;

        // GET: Student/Index (Dashboard)
        public ActionResult Index()
        {
            var user = AuthSessionHelper.GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Account");

            var student = _db.Students.Find(s => s.UserId == user.Id).FirstOrDefault();

            var requests = _db.ServiceRequests.Find(r => r.StudentId == user.Id).ToList();

            var vm = new StudentDashboardViewModel
            {
                User = user,
                Student = student,
                TotalRequests = requests.Count,
                PendingRequests = requests.Count(r => r.Status == "Pending"),
                InProgressRequests = requests.Count(r => r.Status == "In Progress"),
                ApprovedRequests = requests.Count(r => r.Status == "Approved"),
                CompletedRequests = requests.Count(r => r.Status == "Completed"),
                RejectedRequests = requests.Count(r => r.Status == "Rejected"),
                TotalComplaints = (int)_db.Complaints.CountDocuments(c => c.StudentId == user.Id),
                ResolvedComplaints = (int)_db.Complaints.CountDocuments(c => c.StudentId == user.Id && c.Status == "Resolved"),
                RecentRequests = requests.OrderByDescending(r => r.SubmissionDate).Take(5).ToList(),
                FeaturedServices = _db.Services.Find(s => s.IsActive).Limit(6).ToList(),
                RecentAnnouncements = _db.Announcements
                    .Find(a => a.IsActive && (a.TargetRole == "All" || a.TargetRole == "Student"))
                    .SortByDescending(a => a.CreatedAt)
                    .Limit(4)
                    .ToList(),
                Notifications = _db.Notifications
                    .Find(n => n.UserId == user.Id)
                    .SortByDescending(n => n.CreatedAt)
                    .Limit(5)
                    .ToList()
            };

            return View(vm);
        }

        // GET: Student/Services
        public ActionResult Services(string category, string search)
        {
            var filterBuilder = Builders<CampusService>.Filter;
            var filter = filterBuilder.Eq(s => s.IsActive, true);

            if (!string.IsNullOrEmpty(category) && category != "All")
            {
                filter &= filterBuilder.Eq(s => s.Category, category);
            }

            if (!string.IsNullOrEmpty(search))
            {
                filter &= (filterBuilder.Regex(s => s.Name, new MongoDB.Bson.BsonRegularExpression(search, "i")) |
                           filterBuilder.Regex(s => s.Description, new MongoDB.Bson.BsonRegularExpression(search, "i")));
            }

            var services = _db.Services.Find(filter).ToList();
            ViewBag.SelectedCategory = category ?? "All";
            ViewBag.SearchTerm = search;
            ViewBag.Categories = _db.Services.Distinct<string>("Category", Builders<CampusService>.Filter.Eq(s => s.IsActive, true)).ToList();

            return View(services);
        }

        // GET: Student/ApplyService/id
        [HttpGet]
        public ActionResult ApplyService(string id)
        {
            var service = _db.Services.Find(s => s.Id == id || s.ServiceCode == id).FirstOrDefault();
            if (service == null || !service.IsActive)
            {
                TempData["ErrorMessage"] = "Service not found or currently unavailable.";
                return RedirectToAction("Services");
            }

            var vm = new ServiceRequestSubmitViewModel
            {
                ServiceId = service.Id,
                ServiceName = service.Name,
                ServiceCategory = service.Category,
                Description = service.Description,
                Priority = "Medium"
            };

            ViewBag.Service = service;
            return View(vm);
        }

        // POST: Student/ApplyService
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApplyService(ServiceRequestSubmitViewModel model, FormCollection form)
        {
            var service = _db.Services.Find(s => s.Id == model.ServiceId).FirstOrDefault();
            if (service == null)
            {
                TempData["ErrorMessage"] = "Service not found.";
                return RedirectToAction("Services");
            }

            var user = AuthSessionHelper.GetCurrentUser();
            var student = _db.Students.Find(s => s.UserId == user.Id).FirstOrDefault();

            var formDetails = new Dictionary<string, string>();
            foreach (var field in service.FormFields)
            {
                var val = form[field.Name];
                if (field.IsRequired && string.IsNullOrWhiteSpace(val))
                {
                    ModelState.AddModelError("", $"{field.Label} is required.");
                }
                formDetails[field.Label ?? field.Name] = val ?? "";
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Service = service;
                return View(model);
            }

            var reqNumber = _db.GetNextRequestNumber();
            var request = new ServiceRequest
            {
                RequestNumber = reqNumber,
                ServiceId = service.Id,
                ServiceName = service.Name,
                ServiceCategory = service.Category,
                StudentId = user.Id,
                StudentName = user.FullName,
                StudentEmail = user.Email,
                StudentRollNo = student?.RollNumber ?? "N/A",
                Department = student?.Department ?? "General",
                Priority = model.Priority ?? "Medium",
                Status = "Pending",
                SubmissionDate = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                FormDetails = formDetails,
                Remarks = "Request submitted by student. Awaiting faculty/admin review.",
                StatusHistory = new List<StatusHistoryItem>
                {
                    new StatusHistoryItem
                    {
                        Status = "Pending",
                        ChangedBy = user.FullName,
                        Role = "Student",
                        Timestamp = DateTime.UtcNow,
                        Comment = "Service request submitted through student portal."
                    }
                }
            };

            _db.ServiceRequests.InsertOne(request);

            // Add notification to student
            _db.Notifications.InsertOne(new Notification
            {
                UserId = user.Id,
                Title = $"Request Submitted: {service.Name}",
                Message = $"Your request {reqNumber} has been received and is currently under review.",
                Type = "info",
                Link = $"/Student/RequestDetails/{request.Id}",
                CreatedAt = DateTime.UtcNow
            });

            TempData["SuccessMessage"] = $"Your request has been submitted successfully! Tracking ID: {reqNumber}";
            return RedirectToAction("RequestDetails", new { id = request.Id });
        }

        // GET: Student/MyRequests
        public ActionResult MyRequests(string status, string search)
        {
            var user = AuthSessionHelper.GetCurrentUser();
            var filterBuilder = Builders<ServiceRequest>.Filter;
            var filter = filterBuilder.Eq(r => r.StudentId, user.Id);

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                filter &= filterBuilder.Eq(r => r.Status, status);
            }

            if (!string.IsNullOrEmpty(search))
            {
                filter &= (filterBuilder.Regex(r => r.RequestNumber, new MongoDB.Bson.BsonRegularExpression(search, "i")) |
                           filterBuilder.Regex(r => r.ServiceName, new MongoDB.Bson.BsonRegularExpression(search, "i")));
            }

            var requests = _db.ServiceRequests.Find(filter).SortByDescending(r => r.SubmissionDate).ToList();

            ViewBag.SelectedStatus = status ?? "All";
            ViewBag.SearchTerm = search;
            return View(requests);
        }

        // GET: Student/RequestDetails/id
        public ActionResult RequestDetails(string id)
        {
            var user = AuthSessionHelper.GetCurrentUser();
            var request = _db.FindRequestByIdOrNumber(id);
            if (request == null || request.StudentId != user.Id)
            {
                TempData["ErrorMessage"] = "Request not found.";
                return RedirectToAction("MyRequests");
            }

            var service = _db.FindServiceByIdOrCode(request.ServiceId);

            var vm = new ServiceRequestDetailViewModel
            {
                Request = request,
                Service = service,
                CanUpdateStatus = false,
                CanAssign = false
            };

            return View(vm);
        }

        // GET: Student/Voucher/id
        public ActionResult Voucher(string id)
        {
            var user = AuthSessionHelper.GetCurrentUser();
            var request = _db.FindRequestByIdOrNumber(id);
            if (request == null || request.StudentId != user.Id)
            {
                TempData["ErrorMessage"] = "Request not found.";
                return RedirectToAction("MyRequests");
            }

            var student = _db.Students.Find(s => s.UserId == user.Id).FirstOrDefault();
            var service = _db.FindServiceByIdOrCode(request.ServiceId);

            ViewBag.Request = request;
            ViewBag.Student = student;
            ViewBag.Service = service;
            ViewBag.User = user;

            return View();
        }

        // GET: Student/Complaints
        public ActionResult Complaints()
        {
            var user = AuthSessionHelper.GetCurrentUser();
            var complaints = _db.Complaints.Find(c => c.StudentId == user.Id).SortByDescending(c => c.CreatedAt).ToList();

            ViewBag.Complaints = complaints;
            return View(new ComplaintSubmitViewModel());
        }

        // POST: Student/SubmitComplaint
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitComplaint(ComplaintSubmitViewModel model)
        {
            var user = AuthSessionHelper.GetCurrentUser();
            var student = _db.Students.Find(s => s.UserId == user.Id).FirstOrDefault();

            if (!ModelState.IsValid)
            {
                var complaints = _db.Complaints.Find(c => c.StudentId == user.Id).SortByDescending(c => c.CreatedAt).ToList();
                ViewBag.Complaints = complaints;
                return View("Complaints", model);
            }

            var cmpNumber = _db.GetNextComplaintNumber();
            var complaint = new Complaint
            {
                ComplaintNumber = cmpNumber,
                StudentId = user.Id,
                StudentName = user.FullName,
                StudentRollNo = student?.RollNumber ?? "N/A",
                Department = student?.Department ?? "General",
                Category = model.Category,
                Subject = model.Subject.Trim(),
                Description = model.Description.Trim(),
                Location = model.Location.Trim(),
                Priority = model.Priority ?? "Medium",
                Status = "Open",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Complaints.InsertOne(complaint);

            _db.Notifications.InsertOne(new Notification
            {
                UserId = user.Id,
                Title = $"Complaint Logged: {cmpNumber}",
                Message = $"Your complaint '{model.Subject}' has been recorded. Our support team will review it shortly.",
                Type = "warning",
                Link = $"/Student/ComplaintDetails/{complaint.Id}",
                CreatedAt = DateTime.UtcNow
            });

            TempData["SuccessMessage"] = $"Complaint lodged successfully! Tracking ID: {cmpNumber}";
            return RedirectToAction("Complaints");
        }

        // GET: Student/ComplaintDetails/id
        public ActionResult ComplaintDetails(string id)
        {
            var user = AuthSessionHelper.GetCurrentUser();
            var complaint = _db.FindComplaintByIdOrNumber(id);
            if (complaint == null || complaint.StudentId != user.Id)
            {
                TempData["ErrorMessage"] = "Complaint ticket not found.";
                return RedirectToAction("Complaints");
            }

            return View(complaint);
        }

        // GET: Student/Feedback
        public ActionResult Feedback()
        {
            var user = AuthSessionHelper.GetCurrentUser();
            var feedbackList = _db.Feedback.Find(f => f.StudentId == user.Id).SortByDescending(f => f.CreatedAt).ToList();

            ViewBag.FeedbackList = feedbackList;
            ViewBag.Services = _db.Services.Find(s => s.IsActive).ToList();

            return View(new FeedbackSubmitViewModel());
        }

        // POST: Student/SubmitFeedback
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitFeedback(FeedbackSubmitViewModel model)
        {
            var user = AuthSessionHelper.GetCurrentUser();
            var service = _db.Services.Find(s => s.Id == model.ServiceId).FirstOrDefault();

            if (service == null)
            {
                ModelState.AddModelError("ServiceId", "Please select a valid campus service.");
            }

            if (!ModelState.IsValid)
            {
                var feedbackList = _db.Feedback.Find(f => f.StudentId == user.Id).SortByDescending(f => f.CreatedAt).ToList();
                ViewBag.FeedbackList = feedbackList;
                ViewBag.Services = _db.Services.Find(s => s.IsActive).ToList();
                return View("Feedback", model);
            }

            var feedback = new CampusFeedback
            {
                StudentId = user.Id,
                StudentName = user.FullName,
                ServiceId = service.Id,
                ServiceName = service.Name,
                Category = service.Category,
                Rating = model.Rating,
                Comments = model.Comments.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _db.Feedback.InsertOne(feedback);

            TempData["SuccessMessage"] = "Thank you! Your feedback has been recorded and will help improve campus services.";
            return RedirectToAction("Feedback");
        }

        // GET: Student/Announcements
        public ActionResult Announcements()
        {
            var list = _db.Announcements
                .Find(a => a.IsActive && (a.TargetRole == "All" || a.TargetRole == "Student"))
                .SortByDescending(a => a.CreatedAt)
                .ToList();

            return View(list);
        }

        // GET: Student/Notifications
        public ActionResult Notifications()
        {
            var user = AuthSessionHelper.GetCurrentUser();
            var list = _db.Notifications.Find(n => n.UserId == user.Id).SortByDescending(n => n.CreatedAt).ToList();

            return View(list);
        }

        // POST: Student/MarkNotificationRead
        [HttpPost]
        public ActionResult MarkNotificationRead(string id)
        {
            var user = AuthSessionHelper.GetCurrentUser();
            _db.Notifications.UpdateOne(
                n => n.Id == id && n.UserId == user.Id,
                Builders<Notification>.Update.Set(n => n.IsRead, true)
            );

            return Json(new { success = true });
        }
    }
}

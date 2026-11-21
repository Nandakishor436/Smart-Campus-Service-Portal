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
    [AuthorizeRole("Faculty")]
    public class FacultyController : Controller
    {
        private readonly MongoDbContext _db = MongoDbContext.Instance;

        // GET: Faculty/Index (Dashboard)
        public ActionResult Index()
        {
            var user = AuthSessionHelper.GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Account");

            var faculty = _db.Faculty.Find(f => f.UserId == user.Id).FirstOrDefault();

            // Match requests assigned to this faculty OR belonging to their department
            var dept = faculty?.Department ?? "";
            var reqFilter = Builders<ServiceRequest>.Filter.Or(
                Builders<ServiceRequest>.Filter.Eq(r => r.AssignedToId, user.Id),
                Builders<ServiceRequest>.Filter.Eq(r => r.Department, dept)
            );

            var requests = _db.ServiceRequests.Find(reqFilter).ToList();

            var cmpFilter = Builders<Complaint>.Filter.Or(
                Builders<Complaint>.Filter.Eq(c => c.AssignedToId, user.Id),
                Builders<Complaint>.Filter.Eq(c => c.Department, dept)
            );

            var complaints = _db.Complaints.Find(cmpFilter).ToList();

            var vm = new FacultyDashboardViewModel
            {
                User = user,
                Faculty = faculty,
                TotalAssignedRequests = requests.Count,
                PendingApprovalRequests = requests.Count(r => r.Status == "Pending"),
                InProgressRequests = requests.Count(r => r.Status == "In Progress"),
                CompletedRequests = requests.Count(r => r.Status == "Completed"),
                AssignedComplaints = complaints.Count(c => c.Status != "Resolved" && c.Status != "Closed"),
                ResolvedComplaints = complaints.Count(c => c.Status == "Resolved" || c.Status == "Closed"),
                AssignedRequests = requests.OrderByDescending(r => r.SubmissionDate).Take(6).ToList(),
                AssignedComplaintsList = complaints.OrderByDescending(c => c.CreatedAt).Take(5).ToList(),
                Announcements = _db.Announcements
                    .Find(a => a.IsActive && (a.TargetRole == "All" || a.TargetRole == "Faculty"))
                    .SortByDescending(a => a.CreatedAt)
                    .Limit(4)
                    .ToList(),
                RecentFeedback = _db.Feedback.Find(FilterDefinition<CampusFeedback>.Empty).SortByDescending(f => f.CreatedAt).Limit(4).ToList()
            };

            return View(vm);
        }

        // GET: Faculty/AssignedRequests
        public ActionResult AssignedRequests(string status, string search)
        {
            var user = AuthSessionHelper.GetCurrentUser();
            var faculty = _db.Faculty.Find(f => f.UserId == user.Id).FirstOrDefault();
            var dept = faculty?.Department ?? "";

            var filterBuilder = Builders<ServiceRequest>.Filter;
            var filter = filterBuilder.Or(
                filterBuilder.Eq(r => r.AssignedToId, user.Id),
                filterBuilder.Eq(r => r.Department, dept)
            );

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                filter &= filterBuilder.Eq(r => r.Status, status);
            }

            if (!string.IsNullOrEmpty(search))
            {
                filter &= (filterBuilder.Regex(r => r.RequestNumber, new MongoDB.Bson.BsonRegularExpression(search, "i")) |
                           filterBuilder.Regex(r => r.StudentName, new MongoDB.Bson.BsonRegularExpression(search, "i")) |
                           filterBuilder.Regex(r => r.ServiceName, new MongoDB.Bson.BsonRegularExpression(search, "i")));
            }

            var list = _db.ServiceRequests.Find(filter).SortByDescending(r => r.SubmissionDate).ToList();

            ViewBag.SelectedStatus = status ?? "All";
            ViewBag.SearchTerm = search;
            return View(list);
        }

        // GET: Faculty/RequestDetails/id
        public ActionResult RequestDetails(string id)
        {
            var request = _db.FindRequestByIdOrNumber(id);
            if (request == null)
            {
                TempData["ErrorMessage"] = "Request not found.";
                return RedirectToAction("AssignedRequests");
            }

            var service = _db.FindServiceByIdOrCode(request.ServiceId);

            var vm = new ServiceRequestDetailViewModel
            {
                Request = request,
                Service = service,
                CanUpdateStatus = true,
                CanAssign = false
            };

            return View(vm);
        }

        // POST: Faculty/UpdateRequestStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateRequestStatus(string requestId, string status, string comment)
        {
            var user = AuthSessionHelper.GetCurrentUser();
            var request = _db.ServiceRequests.Find(r => r.Id == requestId || r.RequestNumber == requestId).FirstOrDefault();
            if (request == null)
            {
                TempData["ErrorMessage"] = "Request not found.";
                return RedirectToAction("AssignedRequests");
            }

            var validStatuses = new[] { "Pending", "In Progress", "Approved", "Rejected", "Completed" };
            if (!validStatuses.Contains(status))
            {
                TempData["ErrorMessage"] = "Invalid status specified.";
                return RedirectToAction("RequestDetails", new { id = request.Id });
            }

            var historyItem = new StatusHistoryItem
            {
                Status = status,
                ChangedBy = user.FullName,
                Role = "Faculty",
                Timestamp = DateTime.UtcNow,
                Comment = string.IsNullOrWhiteSpace(comment) ? $"Status updated to {status} by faculty." : comment.Trim()
            };

            var update = Builders<ServiceRequest>.Update
                .Set(r => r.Status, status)
                .Set(r => r.AssignedToId, user.Id)
                .Set(r => r.AssignedToName, user.FullName)
                .Set(r => r.AssignedRole, "Faculty")
                .Set(r => r.Remarks, comment?.Trim() ?? request.Remarks)
                .Set(r => r.UpdatedAt, DateTime.UtcNow)
                .Push(r => r.StatusHistory, historyItem);

            _db.ServiceRequests.UpdateOne(r => r.Id == request.Id, update);

            // Notify student
            if (!string.IsNullOrEmpty(request.StudentId))
            {
                string alertType = (status == "Approved" || status == "Completed") ? "success" : (status == "Rejected" ? "danger" : "info");
                _db.Notifications.InsertOne(new Notification
                {
                    UserId = request.StudentId,
                    Title = $"Request {request.RequestNumber} Updated: {status}",
                    Message = $"{user.FullName} updated your request status to '{status}'. Notes: {comment}",
                    Type = alertType,
                    Link = $"/Student/RequestDetails/{request.Id}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            TempData["SuccessMessage"] = $"Request status updated to '{status}' successfully.";
            return RedirectToAction("RequestDetails", new { id = request.Id });
        }

        // GET: Faculty/Complaints
        public ActionResult Complaints(string status, string search)
        {
            var user = AuthSessionHelper.GetCurrentUser();
            var faculty = _db.Faculty.Find(f => f.UserId == user.Id).FirstOrDefault();
            var dept = faculty?.Department ?? "";

            var filterBuilder = Builders<Complaint>.Filter;
            var filter = filterBuilder.Or(
                filterBuilder.Eq(c => c.AssignedToId, user.Id),
                filterBuilder.Eq(c => c.Department, dept)
            );

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                filter &= filterBuilder.Eq(c => c.Status, status);
            }

            if (!string.IsNullOrEmpty(search))
            {
                filter &= (filterBuilder.Regex(c => c.ComplaintNumber, new MongoDB.Bson.BsonRegularExpression(search, "i")) |
                           filterBuilder.Regex(c => c.StudentName, new MongoDB.Bson.BsonRegularExpression(search, "i")) |
                           filterBuilder.Regex(c => c.Subject, new MongoDB.Bson.BsonRegularExpression(search, "i")));
            }

            var list = _db.Complaints.Find(filter).SortByDescending(c => c.CreatedAt).ToList();

            ViewBag.SelectedStatus = status ?? "All";
            ViewBag.SearchTerm = search;
            return View(list);
        }

        // GET: Faculty/ComplaintDetails/id
        public ActionResult ComplaintDetails(string id)
        {
            var complaint = _db.FindComplaintByIdOrNumber(id);
            if (complaint == null)
            {
                TempData["ErrorMessage"] = "Complaint not found.";
                return RedirectToAction("Complaints");
            }

            return View(complaint);
        }

        // POST: Faculty/RespondComplaint
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RespondComplaint(string complaintId, string responseMessage, string status)
        {
            var user = AuthSessionHelper.GetCurrentUser();
            var complaint = _db.Complaints.Find(c => c.Id == complaintId || c.ComplaintNumber == complaintId).FirstOrDefault();
            if (complaint == null)
            {
                TempData["ErrorMessage"] = "Complaint not found.";
                return RedirectToAction("Complaints");
            }

            if (string.IsNullOrWhiteSpace(responseMessage))
            {
                TempData["ErrorMessage"] = "Response message cannot be empty.";
                return RedirectToAction("ComplaintDetails", new { id = complaint.Id });
            }

            var responseItem = new ComplaintResponseItem
            {
                ResponderId = user.Id,
                ResponderName = user.FullName,
                Role = "Faculty",
                Message = responseMessage.Trim(),
                Timestamp = DateTime.UtcNow
            };

            var updateBuilder = Builders<Complaint>.Update
                .Push(c => c.Responses, responseItem)
                .Set(c => c.OfficialResponse, responseMessage.Trim())
                .Set(c => c.AssignedToId, user.Id)
                .Set(c => c.AssignedToName, user.FullName)
                .Set(c => c.UpdatedAt, DateTime.UtcNow);

            if (!string.IsNullOrEmpty(status))
            {
                updateBuilder = updateBuilder.Set(c => c.Status, status);
                if (status == "Resolved" || status == "Closed")
                {
                    updateBuilder = updateBuilder.Set(c => c.ResolutionDate, DateTime.UtcNow);
                }
            }

            _db.Complaints.UpdateOne(c => c.Id == complaint.Id, updateBuilder);

            // Notify student
            if (!string.IsNullOrEmpty(complaint.StudentId))
            {
                _db.Notifications.InsertOne(new Notification
                {
                    UserId = complaint.StudentId,
                    Title = $"Faculty Response: Complaint {complaint.ComplaintNumber}",
                    Message = $"{user.FullName} responded to your complaint: \"{responseMessage.Trim()}\"",
                    Type = status == "Resolved" ? "success" : "info",
                    Link = $"/Student/ComplaintDetails/{complaint.Id}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            TempData["SuccessMessage"] = "Response recorded successfully.";
            return RedirectToAction("ComplaintDetails", new { id = complaint.Id });
        }

        // GET: Faculty/Feedback
        public ActionResult Feedback()
        {
            var user = AuthSessionHelper.GetCurrentUser();
            var faculty = _db.Faculty.Find(f => f.UserId == user.Id).FirstOrDefault();
            var dept = faculty?.Department ?? "";

            var list = _db.Feedback.Find(FilterDefinition<CampusFeedback>.Empty).SortByDescending(f => f.CreatedAt).ToList();
            return View(list);
        }

        // GET: Faculty/Services
        public ActionResult Services()
        {
            var user = AuthSessionHelper.GetCurrentUser();
            var faculty = _db.Faculty.Find(f => f.UserId == user.Id).FirstOrDefault();

            var services = _db.Services.Find(s => s.IsActive).ToList();
            return View(services);
        }
    }
}

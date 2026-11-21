using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using MongoDB.Driver;
using Smart_Campus_Service_Portal.Helpers;
using Smart_Campus_Service_Portal.Models;
using Smart_Campus_Service_Portal.Models.ViewModels;

namespace Smart_Campus_Service_Portal.Controllers
{
    [AuthorizeRole("Admin")]
    public class AdminController : Controller
    {
        private readonly MongoDbContext _db = MongoDbContext.Instance;

        // GET: Admin/Index (Dashboard)
        public ActionResult Index()
        {
            var requests = _db.ServiceRequests.Find(FilterDefinition<ServiceRequest>.Empty).ToList();
            var complaints = _db.Complaints.Find(FilterDefinition<Complaint>.Empty).ToList();
            var feedbacks = _db.Feedback.Find(FilterDefinition<CampusFeedback>.Empty).ToList();

            var vm = new AdminDashboardViewModel
            {
                TotalStudents = (int)_db.Students.CountDocuments(FilterDefinition<Student>.Empty),
                TotalFaculty = (int)_db.Faculty.CountDocuments(FilterDefinition<FacultyMember>.Empty),
                TotalServices = (int)_db.Services.CountDocuments(FilterDefinition<CampusService>.Empty),
                TotalRequests = requests.Count,
                PendingRequests = requests.Count(r => r.Status == "Pending"),
                InProgressRequests = requests.Count(r => r.Status == "In Progress"),
                ApprovedRequests = requests.Count(r => r.Status == "Approved"),
                CompletedRequests = requests.Count(r => r.Status == "Completed"),
                RejectedRequests = requests.Count(r => r.Status == "Rejected"),
                TotalComplaints = complaints.Count,
                OpenComplaints = complaints.Count(c => c.Status == "Open" || c.Status == "Under Review"),
                ResolvedComplaints = complaints.Count(c => c.Status == "Resolved" || c.Status == "Closed"),
                TotalFeedback = feedbacks.Count,
                AverageFeedbackRating = feedbacks.Any() ? Math.Round(feedbacks.Average(f => f.Rating), 1) : 5.0,
                RecentRequests = requests.OrderByDescending(r => r.SubmissionDate).Take(6).ToList(),
                RecentComplaints = complaints.OrderByDescending(c => c.CreatedAt).Take(5).ToList()
            };

            // Chart Data: Requests by Status
            vm.RequestsByStatus = requests
                .GroupBy(r => r.Status ?? "Pending")
                .ToDictionary(g => g.Key, g => g.Count());

            // Chart Data: Requests by Category
            vm.RequestsByCategory = requests
                .GroupBy(r => r.ServiceCategory ?? "Other")
                .ToDictionary(g => g.Key, g => g.Count());

            // Chart Data: Complaints by Category
            vm.ComplaintsByCategory = complaints
                .GroupBy(c => c.Category ?? "Other")
                .ToDictionary(g => g.Key, g => g.Count());

            // Chart Data: Monthly Trends (last 6 months)
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = DateTime.UtcNow.AddMonths(-i);
                var monthName = monthDate.ToString("MMM yyyy");
                var count = requests.Count(r => r.SubmissionDate.Year == monthDate.Year && r.SubmissionDate.Month == monthDate.Month);
                vm.MonthlyRequestTrends[monthName] = count;
            }

            return View(vm);
        }

        // GET: Admin/Users
        public ActionResult Users(string role, string search)
        {
            var filterBuilder = Builders<User>.Filter;
            var filter = filterBuilder.Empty;

            if (!string.IsNullOrEmpty(role) && role != "All")
            {
                filter &= filterBuilder.Eq(u => u.Role, role);
            }

            if (!string.IsNullOrEmpty(search))
            {
                filter &= (filterBuilder.Regex(u => u.FullName, new MongoDB.Bson.BsonRegularExpression(search, "i")) |
                           filterBuilder.Regex(u => u.Email, new MongoDB.Bson.BsonRegularExpression(search, "i")) |
                           filterBuilder.Regex(u => u.Username, new MongoDB.Bson.BsonRegularExpression(search, "i")));
            }

            var users = _db.Users.Find(filter).SortByDescending(u => u.CreatedAt).ToList();

            var userList = new List<UserListItemViewModel>();
            foreach (var u in users)
            {
                var item = new UserListItemViewModel
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    FullName = u.FullName,
                    Role = u.Role,
                    Phone = u.Phone,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                };

                if (u.Role == "Student")
                {
                    var s = _db.Students.Find(x => x.UserId == u.Id).FirstOrDefault();
                    item.Identifier = s?.RollNumber ?? "N/A";
                    item.Department = s?.Department ?? "N/A";
                }
                else if (u.Role == "Faculty")
                {
                    var f = _db.Faculty.Find(x => x.UserId == u.Id).FirstOrDefault();
                    item.Identifier = f?.FacultyId ?? "N/A";
                    item.Department = f?.Department ?? "N/A";
                }
                else
                {
                    item.Identifier = "ADMIN";
                    item.Department = "Administration";
                }

                userList.Add(item);
            }

            ViewBag.SelectedRole = role ?? "All";
            ViewBag.SearchTerm = search;
            return View(userList);
        }

        // POST: Admin/ToggleUserStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleUserStatus(string id)
        {
            var user = _db.Users.Find(u => u.Id == id).FirstOrDefault();
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Users");
            }

            if (user.Email == AuthSessionHelper.CurrentUserEmail)
            {
                TempData["ErrorMessage"] = "You cannot deactivate your own administrative account.";
                return RedirectToAction("Users");
            }

            var update = Builders<User>.Update
                .Set(u => u.IsActive, !user.IsActive)
                .Set(u => u.UpdatedAt, DateTime.UtcNow);

            _db.Users.UpdateOne(u => u.Id == id, update);

            TempData["SuccessMessage"] = $"User '{user.FullName}' has been {(user.IsActive ? "deactivated" : "activated")}.";
            return RedirectToAction("Users");
        }

        // POST: Admin/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateUser(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill in all required fields.";
                return RedirectToAction("Users");
            }

            var existing = _db.Users.Find(u => u.Email.ToLower() == model.Email.ToLower().Trim()).FirstOrDefault();
            if (existing != null)
            {
                TempData["ErrorMessage"] = "An account with this email address already exists.";
                return RedirectToAction("Users");
            }

            var user = new User
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.ToLower().Trim(),
                Username = model.Username.Trim(),
                PasswordHash = PasswordHasher.HashPassword(model.Password),
                Role = model.Role,
                Phone = model.Phone?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Users.InsertOne(user);

            if (model.Role == "Student")
            {
                _db.Students.InsertOne(new Student
                {
                    UserId = user.Id,
                    RollNumber = string.IsNullOrWhiteSpace(model.Identifier) ? $"STU-{DateTime.UtcNow.Ticks % 10000:D4}" : model.Identifier.Trim().ToUpper(),
                    Department = model.Department ?? "Computer Science",
                    Degree = "B.Tech",
                    Semester = 1,
                    AcademicYear = $"{DateTime.UtcNow.Year}-{DateTime.UtcNow.Year + 1}",
                    CreatedAt = DateTime.UtcNow
                });
            }
            else if (model.Role == "Faculty")
            {
                _db.Faculty.InsertOne(new FacultyMember
                {
                    UserId = user.Id,
                    FacultyId = string.IsNullOrWhiteSpace(model.Identifier) ? $"FAC-{DateTime.UtcNow.Ticks % 10000:D4}" : model.Identifier.Trim().ToUpper(),
                    Department = model.Department ?? "Computer Science",
                    Designation = model.Designation ?? "Assistant Professor",
                    CreatedAt = DateTime.UtcNow
                });
            }

            TempData["SuccessMessage"] = $"New {model.Role} user '{user.FullName}' created successfully.";
            return RedirectToAction("Users");
        }

        // GET: Admin/Students
        public ActionResult Students(string department, string search)
        {
            var filterBuilder = Builders<Student>.Filter;
            var filter = filterBuilder.Empty;

            if (!string.IsNullOrEmpty(department) && department != "All")
            {
                filter &= filterBuilder.Eq(s => s.Department, department);
            }

            if (!string.IsNullOrEmpty(search))
            {
                filter &= (filterBuilder.Regex(s => s.RollNumber, new MongoDB.Bson.BsonRegularExpression(search, "i")) |
                           filterBuilder.Regex(s => s.Degree, new MongoDB.Bson.BsonRegularExpression(search, "i")));
            }

            var students = _db.Students.Find(filter).ToList();

            var users = _db.Users.Find(u => u.Role == "Student").ToList().ToDictionary(u => u.Id);
            ViewBag.UsersMap = users;
            ViewBag.SelectedDepartment = department ?? "All";
            ViewBag.SearchTerm = search;
            ViewBag.Departments = _db.Students.Distinct<string>("Department", FilterDefinition<Student>.Empty).ToList();

            return View(students);
        }

        // GET: Admin/Faculty
        public ActionResult Faculty(string department, string search)
        {
            var filterBuilder = Builders<FacultyMember>.Filter;
            var filter = filterBuilder.Empty;

            if (!string.IsNullOrEmpty(department) && department != "All")
            {
                filter &= filterBuilder.Eq(f => f.Department, department);
            }

            if (!string.IsNullOrEmpty(search))
            {
                filter &= (filterBuilder.Regex(f => f.FacultyId, new MongoDB.Bson.BsonRegularExpression(search, "i")) |
                           filterBuilder.Regex(f => f.Designation, new MongoDB.Bson.BsonRegularExpression(search, "i")));
            }

            var facultyList = _db.Faculty.Find(filter).ToList();
            var users = _db.Users.Find(u => u.Role == "Faculty").ToList().ToDictionary(u => u.Id);

            ViewBag.UsersMap = users;
            ViewBag.SelectedDepartment = department ?? "All";
            ViewBag.SearchTerm = search;
            ViewBag.Departments = _db.Faculty.Distinct<string>("Department", FilterDefinition<FacultyMember>.Empty).ToList();

            return View(facultyList);
        }

        // GET: Admin/Services
        public ActionResult Services(string search)
        {
            var filterBuilder = Builders<CampusService>.Filter;
            var filter = filterBuilder.Empty;

            if (!string.IsNullOrEmpty(search))
            {
                filter &= (filterBuilder.Regex(s => s.Name, new MongoDB.Bson.BsonRegularExpression(search, "i")) |
                           filterBuilder.Regex(s => s.Category, new MongoDB.Bson.BsonRegularExpression(search, "i")));
            }

            var services = _db.Services.Find(filter).SortBy(s => s.Name).ToList();
            ViewBag.SearchTerm = search;
            return View(services);
        }

        // POST: Admin/CreateService
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateService(CampusServiceManageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill in all service details.";
                return RedirectToAction("Services");
            }

            var existing = _db.Services.Find(s => s.ServiceCode.ToUpper() == model.ServiceCode.ToUpper().Trim()).FirstOrDefault();
            if (existing != null)
            {
                TempData["ErrorMessage"] = "A service with this Service Code already exists.";
                return RedirectToAction("Services");
            }

            var service = new CampusService
            {
                ServiceCode = model.ServiceCode.ToUpper().Trim(),
                Name = model.Name.Trim(),
                Category = model.Category.Trim(),
                Description = model.Description.Trim(),
                Icon = string.IsNullOrWhiteSpace(model.Icon) ? "fa-cogs" : model.Icon.Trim(),
                EstimatedDays = model.EstimatedDays > 0 ? model.EstimatedDays : 3,
                Department = model.Department.Trim(),
                IsActive = model.IsActive,
                FormFields = new List<ServiceFormField>
                {
                    new ServiceFormField { Name = "Details", Label = "Specific Details / Requirements", FieldType = "textarea", IsRequired = true, Placeholder = "Enter details for your request" },
                    new ServiceFormField { Name = "PreferredDate", Label = "Preferred Date", FieldType = "date", IsRequired = false }
                },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Services.InsertOne(service);

            TempData["SuccessMessage"] = $"Service '{service.Name}' created successfully.";
            return RedirectToAction("Services");
        }

        // POST: Admin/EditService
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditService(CampusServiceManageViewModel model)
        {
            var service = _db.Services.Find(s => s.Id == model.Id).FirstOrDefault();
            if (service == null)
            {
                TempData["ErrorMessage"] = "Service not found.";
                return RedirectToAction("Services");
            }

            var update = Builders<CampusService>.Update
                .Set(s => s.Name, model.Name.Trim())
                .Set(s => s.Category, model.Category.Trim())
                .Set(s => s.Description, model.Description.Trim())
                .Set(s => s.Icon, string.IsNullOrWhiteSpace(model.Icon) ? service.Icon : model.Icon.Trim())
                .Set(s => s.EstimatedDays, model.EstimatedDays)
                .Set(s => s.Department, model.Department.Trim())
                .Set(s => s.IsActive, model.IsActive)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            _db.Services.UpdateOne(s => s.Id == model.Id, update);

            TempData["SuccessMessage"] = $"Service '{model.Name}' updated successfully.";
            return RedirectToAction("Services");
        }

        // POST: Admin/DeleteService
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteService(string id)
        {
            var service = _db.Services.Find(s => s.Id == id).FirstOrDefault();
            if (service == null)
            {
                TempData["ErrorMessage"] = "Service not found.";
                return RedirectToAction("Services");
            }

            _db.Services.DeleteOne(s => s.Id == id);

            TempData["SuccessMessage"] = $"Service '{service.Name}' deleted successfully.";
            return RedirectToAction("Services");
        }

        // POST: Admin/ToggleServiceStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleServiceStatus(string id)
        {
            var service = _db.Services.Find(s => s.Id == id).FirstOrDefault();
            if (service == null)
            {
                TempData["ErrorMessage"] = "Service not found.";
                return RedirectToAction("Services");
            }

            var update = Builders<CampusService>.Update
                .Set(s => s.IsActive, !service.IsActive)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            _db.Services.UpdateOne(s => s.Id == id, update);

            TempData["SuccessMessage"] = $"Service '{service.Name}' is now {(service.IsActive ? "deactivated" : "active")}.";
            return RedirectToAction("Services");
        }

        // GET: Admin/Requests
        public ActionResult Requests(string status, string category, string search)
        {
            var filterBuilder = Builders<ServiceRequest>.Filter;
            var filter = filterBuilder.Empty;

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                filter &= filterBuilder.Eq(r => r.Status, status);
            }

            if (!string.IsNullOrEmpty(category) && category != "All")
            {
                filter &= filterBuilder.Eq(r => r.ServiceCategory, category);
            }

            if (!string.IsNullOrEmpty(search))
            {
                filter &= (filterBuilder.Regex(r => r.RequestNumber, new MongoDB.Bson.BsonRegularExpression(search, "i")) |
                           filterBuilder.Regex(r => r.StudentName, new MongoDB.Bson.BsonRegularExpression(search, "i")) |
                           filterBuilder.Regex(r => r.ServiceName, new MongoDB.Bson.BsonRegularExpression(search, "i")));
            }

            var requests = _db.ServiceRequests.Find(filter).SortByDescending(r => r.SubmissionDate).ToList();

            ViewBag.SelectedStatus = status ?? "All";
            ViewBag.SelectedCategory = category ?? "All";
            ViewBag.SearchTerm = search;
            ViewBag.Categories = _db.Services.Distinct<string>("Category", FilterDefinition<CampusService>.Empty).ToList();
            ViewBag.FacultyList = _db.Faculty.Find(FilterDefinition<FacultyMember>.Empty).ToList();
            ViewBag.FacultyUsers = _db.Users.Find(u => u.Role == "Faculty").ToList().ToDictionary(u => u.Id);

            return View(requests);
        }

        // POST: Admin/AssignRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AssignRequest(string requestId, string facultyId)
        {
            var request = _db.ServiceRequests.Find(r => r.Id == requestId).FirstOrDefault();
            if (request == null)
            {
                TempData["ErrorMessage"] = "Request not found.";
                return RedirectToAction("Requests");
            }

            var facultyUser = _db.Users.Find(u => u.Id == facultyId).FirstOrDefault();
            if (facultyUser == null)
            {
                TempData["ErrorMessage"] = "Selected faculty member not found.";
                return RedirectToAction("Requests");
            }

            var historyItem = new StatusHistoryItem
            {
                Status = request.Status,
                ChangedBy = AuthSessionHelper.CurrentUserFullName,
                Role = "Admin",
                Timestamp = DateTime.UtcNow,
                Comment = $"Assigned request to {facultyUser.FullName}."
            };

            var update = Builders<ServiceRequest>.Update
                .Set(r => r.AssignedToId, facultyUser.Id)
                .Set(r => r.AssignedToName, facultyUser.FullName)
                .Set(r => r.AssignedRole, "Faculty")
                .Set(r => r.UpdatedAt, DateTime.UtcNow)
                .Push(r => r.StatusHistory, historyItem);

            _db.ServiceRequests.UpdateOne(r => r.Id == requestId, update);

            // Notify faculty
            _db.Notifications.InsertOne(new Notification
            {
                UserId = facultyUser.Id,
                Title = $"New Request Assigned: {request.RequestNumber}",
                Message = $"Admin assigned {request.ServiceName} request from {request.StudentName} to you.",
                Type = "info",
                Link = $"/Faculty/RequestDetails/{request.Id}",
                CreatedAt = DateTime.UtcNow
            });

            TempData["SuccessMessage"] = $"Request {request.RequestNumber} assigned to {facultyUser.FullName}.";
            return RedirectToAction("Requests");
        }

        // POST: Admin/UpdateRequestStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateRequestStatus(string requestId, string status, string remarks)
        {
            var request = _db.ServiceRequests.Find(r => r.Id == requestId).FirstOrDefault();
            if (request == null)
            {
                TempData["ErrorMessage"] = "Request not found.";
                return RedirectToAction("Requests");
            }

            var historyItem = new StatusHistoryItem
            {
                Status = status,
                ChangedBy = AuthSessionHelper.CurrentUserFullName,
                Role = "Admin",
                Timestamp = DateTime.UtcNow,
                Comment = string.IsNullOrWhiteSpace(remarks) ? $"Status updated to {status} by administrator." : remarks.Trim()
            };

            var update = Builders<ServiceRequest>.Update
                .Set(r => r.Status, status)
                .Set(r => r.Remarks, remarks?.Trim() ?? request.Remarks)
                .Set(r => r.UpdatedAt, DateTime.UtcNow)
                .Push(r => r.StatusHistory, historyItem);

            _db.ServiceRequests.UpdateOne(r => r.Id == requestId, update);

            // Notify student
            if (!string.IsNullOrEmpty(request.StudentId))
            {
                _db.Notifications.InsertOne(new Notification
                {
                    UserId = request.StudentId,
                    Title = $"Request {request.RequestNumber} Status: {status}",
                    Message = $"Your request status was updated to '{status}' by Administration. Remarks: {remarks}",
                    Type = (status == "Approved" || status == "Completed") ? "success" : "info",
                    Link = $"/Student/RequestDetails/{request.Id}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            TempData["SuccessMessage"] = $"Request {request.RequestNumber} status updated to '{status}'.";
            return RedirectToAction("Requests");
        }

        // GET: Admin/Complaints
        public ActionResult Complaints(string status, string category, string search)
        {
            var filterBuilder = Builders<Complaint>.Filter;
            var filter = filterBuilder.Empty;

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                filter &= filterBuilder.Eq(c => c.Status, status);
            }

            if (!string.IsNullOrEmpty(category) && category != "All")
            {
                filter &= filterBuilder.Eq(c => c.Category, category);
            }

            if (!string.IsNullOrEmpty(search))
            {
                filter &= (filterBuilder.Regex(c => c.ComplaintNumber, new MongoDB.Bson.BsonRegularExpression(search, "i")) |
                           filterBuilder.Regex(c => c.StudentName, new MongoDB.Bson.BsonRegularExpression(search, "i")) |
                           filterBuilder.Regex(c => c.Subject, new MongoDB.Bson.BsonRegularExpression(search, "i")));
            }

            var complaints = _db.Complaints.Find(filter).SortByDescending(c => c.CreatedAt).ToList();

            ViewBag.SelectedStatus = status ?? "All";
            ViewBag.SelectedCategory = category ?? "All";
            ViewBag.SearchTerm = search;
            ViewBag.FacultyUsers = _db.Users.Find(u => u.Role == "Faculty").ToList();

            return View(complaints);
        }

        // POST: Admin/AssignComplaint
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AssignComplaint(string complaintId, string facultyId)
        {
            var complaint = _db.Complaints.Find(c => c.Id == complaintId).FirstOrDefault();
            if (complaint == null)
            {
                TempData["ErrorMessage"] = "Complaint not found.";
                return RedirectToAction("Complaints");
            }

            var facultyUser = _db.Users.Find(u => u.Id == facultyId).FirstOrDefault();
            if (facultyUser == null)
            {
                TempData["ErrorMessage"] = "Faculty member not found.";
                return RedirectToAction("Complaints");
            }

            var update = Builders<Complaint>.Update
                .Set(c => c.AssignedToId, facultyUser.Id)
                .Set(c => c.AssignedToName, facultyUser.FullName)
                .Set(c => c.Status, complaint.Status == "Open" ? "Under Review" : complaint.Status)
                .Set(c => c.UpdatedAt, DateTime.UtcNow);

            _db.Complaints.UpdateOne(c => c.Id == complaintId, update);

            TempData["SuccessMessage"] = $"Complaint {complaint.ComplaintNumber} assigned to {facultyUser.FullName}.";
            return RedirectToAction("Complaints");
        }

        // POST: Admin/ResolveComplaint
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResolveComplaint(string complaintId, string officialResponse, string status)
        {
            var complaint = _db.Complaints.Find(c => c.Id == complaintId).FirstOrDefault();
            if (complaint == null)
            {
                TempData["ErrorMessage"] = "Complaint not found.";
                return RedirectToAction("Complaints");
            }

            var responseItem = new ComplaintResponseItem
            {
                ResponderId = AuthSessionHelper.CurrentUserId,
                ResponderName = AuthSessionHelper.CurrentUserFullName,
                Role = "Admin",
                Message = officialResponse.Trim(),
                Timestamp = DateTime.UtcNow
            };

            var finalStatus = string.IsNullOrWhiteSpace(status) ? "Resolved" : status;

            var updateBuilder = Builders<Complaint>.Update
                .Push(c => c.Responses, responseItem)
                .Set(c => c.OfficialResponse, officialResponse.Trim())
                .Set(c => c.Status, finalStatus)
                .Set(c => c.UpdatedAt, DateTime.UtcNow);

            if (finalStatus == "Resolved" || finalStatus == "Closed")
            {
                updateBuilder = updateBuilder.Set(c => c.ResolutionDate, DateTime.UtcNow);
            }

            _db.Complaints.UpdateOne(c => c.Id == complaintId, updateBuilder);

            // Notify student
            if (!string.IsNullOrEmpty(complaint.StudentId))
            {
                _db.Notifications.InsertOne(new Notification
                {
                    UserId = complaint.StudentId,
                    Title = $"Complaint {complaint.ComplaintNumber} {finalStatus}",
                    Message = $"Campus Administration responded: \"{officialResponse.Trim()}\"",
                    Type = "success",
                    Link = $"/Student/ComplaintDetails/{complaint.Id}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            TempData["SuccessMessage"] = $"Complaint {complaint.ComplaintNumber} marked as {finalStatus}.";
            return RedirectToAction("Complaints");
        }

        // GET: Admin/Announcements
        public ActionResult Announcements()
        {
            var announcements = _db.Announcements.Find(FilterDefinition<Announcement>.Empty).SortByDescending(a => a.CreatedAt).ToList();
            return View(announcements);
        }

        // POST: Admin/CreateAnnouncement
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateAnnouncement(AnnouncementFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please provide all required announcement details.";
                return RedirectToAction("Announcements");
            }

            var announcement = new Announcement
            {
                Title = model.Title.Trim(),
                Content = model.Content.Trim(),
                Category = model.Category.Trim(),
                TargetRole = model.TargetRole,
                Priority = model.Priority,
                AuthorName = AuthSessionHelper.CurrentUserFullName,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow,
                ExpiryDate = model.ExpiryDate
            };

            _db.Announcements.InsertOne(announcement);

            TempData["SuccessMessage"] = "Announcement published successfully.";
            return RedirectToAction("Announcements");
        }

        // POST: Admin/EditAnnouncement
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditAnnouncement(AnnouncementFormViewModel model)
        {
            var update = Builders<Announcement>.Update
                .Set(a => a.Title, model.Title.Trim())
                .Set(a => a.Content, model.Content.Trim())
                .Set(a => a.Category, model.Category.Trim())
                .Set(a => a.TargetRole, model.TargetRole)
                .Set(a => a.Priority, model.Priority)
                .Set(a => a.IsActive, model.IsActive)
                .Set(a => a.ExpiryDate, model.ExpiryDate);

            _db.Announcements.UpdateOne(a => a.Id == model.Id, update);

            TempData["SuccessMessage"] = "Announcement updated successfully.";
            return RedirectToAction("Announcements");
        }

        // POST: Admin/DeleteAnnouncement
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteAnnouncement(string id)
        {
            _db.Announcements.DeleteOne(a => a.Id == id);
            TempData["SuccessMessage"] = "Announcement removed successfully.";
            return RedirectToAction("Announcements");
        }

        // GET: Admin/Feedback
        public ActionResult Feedback(string search)
        {
            var filter = Builders<CampusFeedback>.Filter.Empty;
            if (!string.IsNullOrEmpty(search))
            {
                filter &= (Builders<CampusFeedback>.Filter.Regex(f => f.ServiceName, new MongoDB.Bson.BsonRegularExpression(search, "i")) |
                           Builders<CampusFeedback>.Filter.Regex(f => f.Comments, new MongoDB.Bson.BsonRegularExpression(search, "i")));
            }

            var feedbackList = _db.Feedback.Find(filter).SortByDescending(f => f.CreatedAt).ToList();
            ViewBag.SearchTerm = search;
            ViewBag.AverageRating = feedbackList.Any() ? Math.Round(feedbackList.Average(f => f.Rating), 1) : 5.0;

            return View(feedbackList);
        }

        // GET: Admin/Reports
        public ActionResult Reports(ReportsFilterViewModel filter)
        {
            var reqFilter = Builders<ServiceRequest>.Filter.Empty;

            if (filter.StartDate.HasValue)
            {
                reqFilter &= Builders<ServiceRequest>.Filter.Gte(r => r.SubmissionDate, filter.StartDate.Value);
            }
            if (filter.EndDate.HasValue)
            {
                reqFilter &= Builders<ServiceRequest>.Filter.Lte(r => r.SubmissionDate, filter.EndDate.Value.AddDays(1));
            }
            if (!string.IsNullOrEmpty(filter.ServiceCategory) && filter.ServiceCategory != "All")
            {
                reqFilter &= Builders<ServiceRequest>.Filter.Eq(r => r.ServiceCategory, filter.ServiceCategory);
            }
            if (!string.IsNullOrEmpty(filter.Status) && filter.Status != "All")
            {
                reqFilter &= Builders<ServiceRequest>.Filter.Eq(r => r.Status, filter.Status);
            }

            filter.FilteredRequests = _db.ServiceRequests.Find(reqFilter).SortByDescending(r => r.SubmissionDate).ToList();
            filter.FilteredComplaints = _db.Complaints.Find(FilterDefinition<Complaint>.Empty).SortByDescending(c => c.CreatedAt).ToList();

            ViewBag.Categories = _db.Services.Distinct<string>("Category", FilterDefinition<CampusService>.Empty).ToList();

            return View(filter);
        }

        // GET: Admin/ExportRequestsCsv
        public ActionResult ExportRequestsCsv()
        {
            var requests = _db.ServiceRequests.Find(FilterDefinition<ServiceRequest>.Empty).SortByDescending(r => r.SubmissionDate).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Request Number,Service Name,Category,Student Roll No,Student Name,Department,Priority,Status,Submission Date,Assigned To");

            foreach (var r in requests)
            {
                sb.AppendLine($"\"{r.RequestNumber}\",\"{r.ServiceName}\",\"{r.ServiceCategory}\",\"{r.StudentRollNo}\",\"{r.StudentName}\",\"{r.Department}\",\"{r.Priority}\",\"{r.Status}\",\"{r.SubmissionDate:yyyy-MM-dd HH:mm}\",\"{r.AssignedToName ?? "Unassigned"}\"");
            }

            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"Campus_Service_Requests_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
        }
    }
}

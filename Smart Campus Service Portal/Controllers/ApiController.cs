using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using MongoDB.Driver;
using Newtonsoft.Json;
using Smart_Campus_Service_Portal.Helpers;
using Smart_Campus_Service_Portal.Models;
using Smart_Campus_Service_Portal.Models.ViewModels;

namespace Smart_Campus_Service_Portal.Controllers
{
    public class ApiController : Controller
    {
        private readonly MongoDbContext _db = MongoDbContext.Instance;

        // Helper to parse JSON body from Request.InputStream
        private T ParseJsonBody<T>()
        {
            try
            {
                Request.InputStream.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(Request.InputStream))
                {
                    var body = reader.ReadToEnd();
                    if (string.IsNullOrWhiteSpace(body)) return default(T);
                    return JsonConvert.DeserializeObject<T>(body);
                }
            }
            catch
            {
                return default(T);
            }
        }

        private JsonResult JsonOk<T>(T data, string message = "Success", int statusCode = 200)
        {
            Response.StatusCode = statusCode;
            return Json(ApiResponse<T>.Ok(data, message), JsonRequestBehavior.AllowGet);
        }

        private JsonResult JsonError(string message, int statusCode = 400, List<string> errors = null)
        {
            Response.StatusCode = statusCode;
            return Json(ApiResponse<object>.Fail(message, errors), JsonRequestBehavior.AllowGet);
        }

        private User GetAuthenticatedUser()
        {
            var authHeader = Request.Headers["Authorization"];
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring(7).Trim();
                var user = AuthSessionHelper.ValidateApiToken(token);
                if (user != null) return user;
            }

            if (AuthSessionHelper.IsAuthenticated)
            {
                return AuthSessionHelper.GetCurrentUser();
            }

            return null;
        }

        // ==========================================
        // 1. AUTHENTICATION APIS
        // ==========================================

        // POST /api/auth/register
        [HttpPost]
        public ActionResult AuthRegister()
        {
            try
            {
                var model = ParseJsonBody<RegisterViewModel>();
                if (model == null || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
                {
                    return JsonError("Invalid registration payload. Email, Password, and FullName are required.", 400);
                }

                var existing = _db.Users.Find(u => u.Email.ToLower() == model.Email.ToLower().Trim()).FirstOrDefault();
                if (existing != null)
                {
                    return JsonError("An account with this email already exists.", 409);
                }

                var user = new User
                {
                    FullName = model.FullName.Trim(),
                    Email = model.Email.ToLower().Trim(),
                    Username = string.IsNullOrWhiteSpace(model.Username) ? model.Email.Split('@')[0] : model.Username.Trim(),
                    PasswordHash = PasswordHasher.HashPassword(model.Password),
                    Role = "Student",
                    Phone = model.Phone,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.Users.InsertOne(user);

                var student = new Student
                {
                    UserId = user.Id,
                    RollNumber = string.IsNullOrWhiteSpace(model.RollNumber) ? $"CS-{DateTime.UtcNow.Ticks % 10000:D4}" : model.RollNumber.ToUpper().Trim(),
                    Department = model.Department ?? "Computer Science",
                    Degree = model.Degree ?? "B.Tech",
                    Semester = model.Semester > 0 ? model.Semester : 1,
                    AcademicYear = $"{DateTime.UtcNow.Year}-{DateTime.UtcNow.Year + 1}",
                    Address = model.Address,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Students.InsertOne(student);

                var token = AuthSessionHelper.GenerateApiToken(user);
                AuthSessionHelper.SignIn(user);

                return JsonOk(new
                {
                    user = new { user.Id, user.FullName, user.Email, user.Role, user.Username },
                    student = new { student.RollNumber, student.Department, student.Degree, student.Semester },
                    token
                }, "Registration successful.", 201);
            }
            catch (Exception ex)
            {
                return JsonError($"Registration failed: {ex.Message}", 500);
            }
        }

        // POST /api/auth/login
        [HttpPost]
        public ActionResult AuthLogin()
        {
            try
            {
                var model = ParseJsonBody<LoginViewModel>();
                if (model == null || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
                {
                    return JsonError("Email and Password are required.", 400);
                }

                var user = _db.Users.Find(u => u.Email.ToLower() == model.Email.ToLower().Trim()).FirstOrDefault();
                if (user == null || !PasswordHasher.VerifyPassword(model.Password, user.PasswordHash))
                {
                    return JsonError("Invalid email or password.", 401);
                }

                if (!user.IsActive)
                {
                    return JsonError("Account deactivated. Contact administrator.", 403);
                }

                var token = AuthSessionHelper.GenerateApiToken(user);
                AuthSessionHelper.SignIn(user);

                return JsonOk(new
                {
                    user = new { user.Id, user.FullName, user.Email, user.Role, user.Username },
                    token
                }, "Login successful.");
            }
            catch (Exception ex)
            {
                return JsonError($"Login error: {ex.Message}", 500);
            }
        }

        // POST /api/auth/logout
        [HttpPost]
        public ActionResult AuthLogout()
        {
            AuthSessionHelper.SignOut();
            return JsonOk(new { }, "Logged out successfully.");
        }

        // GET /api/auth/me
        [HttpGet]
        public ActionResult AuthMe()
        {
            var user = GetAuthenticatedUser();
            if (user == null)
            {
                return JsonError("Unauthorized. Please provide a valid Bearer token or login.", 401);
            }

            object profileData = null;
            if (user.Role == "Student")
            {
                profileData = _db.Students.Find(s => s.UserId == user.Id).FirstOrDefault();
            }
            else if (user.Role == "Faculty")
            {
                profileData = _db.Faculty.Find(f => f.UserId == user.Id).FirstOrDefault();
            }

            return JsonOk(new
            {
                user = new { user.Id, user.FullName, user.Email, user.Role, user.Username, user.Phone, user.CreatedAt },
                profile = profileData
            }, "Profile retrieved successfully.");
        }

        // ==========================================
        // 2. STUDENT APIS
        // ==========================================

        // GET /api/students or GET /api/students/:id or PUT /api/students/:id
        public ActionResult StudentsEndpoint(string id)
        {
            var method = Request.HttpMethod.ToUpperInvariant();

            if (method == "GET")
            {
                if (string.IsNullOrEmpty(id))
                {
                    // GET /api/students
                    var list = _db.Students.Find(FilterDefinition<Student>.Empty).ToList();
                    var userIds = list.Select(s => s.UserId).ToList();
                    var users = _db.Users.Find(u => userIds.Contains(u.Id)).ToList().ToDictionary(u => u.Id);

                    var result = list.Select(s => new
                    {
                        s.Id,
                        s.UserId,
                        s.RollNumber,
                        s.Department,
                        s.Degree,
                        s.Semester,
                        s.AcademicYear,
                        s.Address,
                        s.EmergencyContact,
                        FullName = users.ContainsKey(s.UserId) ? users[s.UserId].FullName : "N/A",
                        Email = users.ContainsKey(s.UserId) ? users[s.UserId].Email : "N/A"
                    });

                    return JsonOk(result);
                }
                else
                {
                    // GET /api/students/:id
                    var student = _db.Students.Find(s => s.Id == id || s.UserId == id || s.RollNumber == id).FirstOrDefault();
                    if (student == null) return JsonError("Student not found.", 404);

                    var user = _db.Users.Find(u => u.Id == student.UserId).FirstOrDefault();
                    return JsonOk(new
                    {
                        student,
                        user = user != null ? new { user.FullName, user.Email, user.Phone, user.Username } : null
                    });
                }
            }
            else if (method == "PUT")
            {
                // PUT /api/students/:id
                var authUser = GetAuthenticatedUser();
                if (authUser == null) return JsonError("Unauthorized.", 401);

                var student = _db.Students.Find(s => s.Id == id || s.UserId == id || s.RollNumber == id).FirstOrDefault();
                if (student == null) return JsonError("Student not found.", 404);

                if (authUser.Role != "Admin" && authUser.Id != student.UserId)
                {
                    return JsonError("Forbidden. You cannot update another student's profile.", 403);
                }

                var updateModel = ParseJsonBody<Student>();
                if (updateModel == null) return JsonError("Invalid payload.", 400);

                var update = Builders<Student>.Update
                    .Set(s => s.Department, updateModel.Department ?? student.Department)
                    .Set(s => s.Degree, updateModel.Degree ?? student.Degree)
                    .Set(s => s.Semester, updateModel.Semester > 0 ? updateModel.Semester : student.Semester)
                    .Set(s => s.Address, updateModel.Address ?? student.Address)
                    .Set(s => s.EmergencyContact, updateModel.EmergencyContact ?? student.EmergencyContact);

                _db.Students.UpdateOne(s => s.Id == student.Id, update);

                var updated = _db.Students.Find(s => s.Id == student.Id).FirstOrDefault();
                return JsonOk(updated, "Student updated successfully.");
            }

            return JsonError("Method not allowed.", 405);
        }

        // ==========================================
        // 3. FACULTY APIS
        // ==========================================

        // GET /api/faculty or GET /api/faculty/:id
        [HttpGet]
        public ActionResult FacultyEndpoint(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                var list = _db.Faculty.Find(FilterDefinition<FacultyMember>.Empty).ToList();
                var userIds = list.Select(f => f.UserId).ToList();
                var users = _db.Users.Find(u => userIds.Contains(u.Id)).ToList().ToDictionary(u => u.Id);

                var result = list.Select(f => new
                {
                    f.Id,
                    f.UserId,
                    f.FacultyId,
                    f.Department,
                    f.Designation,
                    f.Specialization,
                    f.OfficeRoom,
                    FullName = users.ContainsKey(f.UserId) ? users[f.UserId].FullName : "N/A",
                    Email = users.ContainsKey(f.UserId) ? users[f.UserId].Email : "N/A",
                    Phone = users.ContainsKey(f.UserId) ? users[f.UserId].Phone : "N/A"
                });

                return JsonOk(result);
            }
            else
            {
                var faculty = _db.Faculty.Find(f => f.Id == id || f.UserId == id || f.FacultyId == id).FirstOrDefault();
                if (faculty == null) return JsonError("Faculty member not found.", 404);

                var user = _db.Users.Find(u => u.Id == faculty.UserId).FirstOrDefault();
                return JsonOk(new
                {
                    faculty,
                    user = user != null ? new { user.FullName, user.Email, user.Phone, user.Username } : null
                });
            }
        }

        // ==========================================
        // 4. SERVICE APIS
        // ==========================================

        // GET /api/services, POST /api/services, PUT /api/services/:id, DELETE /api/services/:id
        public ActionResult ServicesEndpoint(string id)
        {
            var method = Request.HttpMethod.ToUpperInvariant();

            if (method == "GET")
            {
                if (string.IsNullOrEmpty(id))
                {
                    var services = _db.Services.Find(FilterDefinition<CampusService>.Empty).ToList();
                    return JsonOk(services);
                }
                else
                {
                    var service = _db.Services.Find(s => s.Id == id || s.ServiceCode == id).FirstOrDefault();
                    if (service == null) return JsonError("Service not found.", 404);
                    return JsonOk(service);
                }
            }
            else if (method == "POST")
            {
                var user = GetAuthenticatedUser();
                if (user == null || user.Role != "Admin") return JsonError("Admin authorization required.", 403);

                var model = ParseJsonBody<CampusService>();
                if (model == null || string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.ServiceCode))
                {
                    return JsonError("Service Code and Name are required.", 400);
                }

                model.CreatedAt = DateTime.UtcNow;
                model.UpdatedAt = DateTime.UtcNow;
                _db.Services.InsertOne(model);

                return JsonOk(model, "Service created successfully.", 201);
            }
            else if (method == "PUT")
            {
                var user = GetAuthenticatedUser();
                if (user == null || user.Role != "Admin") return JsonError("Admin authorization required.", 403);

                var existing = _db.Services.Find(s => s.Id == id || s.ServiceCode == id).FirstOrDefault();
                if (existing == null) return JsonError("Service not found.", 404);

                var updateModel = ParseJsonBody<CampusService>();
                if (updateModel == null) return JsonError("Invalid payload.", 400);

                var update = Builders<CampusService>.Update
                    .Set(s => s.Name, updateModel.Name ?? existing.Name)
                    .Set(s => s.Category, updateModel.Category ?? existing.Category)
                    .Set(s => s.Description, updateModel.Description ?? existing.Description)
                    .Set(s => s.Icon, updateModel.Icon ?? existing.Icon)
                    .Set(s => s.Department, updateModel.Department ?? existing.Department)
                    .Set(s => s.EstimatedDays, updateModel.EstimatedDays > 0 ? updateModel.EstimatedDays : existing.EstimatedDays)
                    .Set(s => s.IsActive, updateModel.IsActive)
                    .Set(s => s.UpdatedAt, DateTime.UtcNow);

                _db.Services.UpdateOne(s => s.Id == existing.Id, update);
                var updated = _db.Services.Find(s => s.Id == existing.Id).FirstOrDefault();
                return JsonOk(updated, "Service updated successfully.");
            }
            else if (method == "DELETE")
            {
                var user = GetAuthenticatedUser();
                if (user == null || user.Role != "Admin") return JsonError("Admin authorization required.", 403);

                var result = _db.Services.DeleteOne(s => s.Id == id || s.ServiceCode == id);
                if (result.DeletedCount == 0) return JsonError("Service not found.", 404);

                return JsonOk(new { id }, "Service deleted successfully.");
            }

            return JsonError("Method not allowed.", 405);
        }

        // ==========================================
        // 5. SERVICE REQUEST APIS
        // ==========================================

        // POST /api/requests, GET /api/requests, GET /api/requests/:id, PUT /api/requests/:id, DELETE /api/requests/:id
        public ActionResult RequestsEndpoint(string id)
        {
            var method = Request.HttpMethod.ToUpperInvariant();

            if (method == "GET")
            {
                if (string.IsNullOrEmpty(id))
                {
                    var status = Request.QueryString["status"];
                    var filter = Builders<ServiceRequest>.Filter.Empty;
                    if (!string.IsNullOrEmpty(status))
                    {
                        filter &= Builders<ServiceRequest>.Filter.Eq(r => r.Status, status);
                    }

                    var list = _db.ServiceRequests.Find(filter).SortByDescending(r => r.SubmissionDate).ToList();
                    return JsonOk(list);
                }
                else
                {
                    var request = _db.FindRequestByIdOrNumber(id);
                    if (request == null) return JsonError("Service request not found.", 404);
                    return JsonOk(request);
                }
            }
            else if (method == "POST")
            {
                var user = GetAuthenticatedUser();
                if (user == null) return JsonError("Unauthorized.", 401);

                var model = ParseJsonBody<ServiceRequest>();
                if (model == null || string.IsNullOrWhiteSpace(model.ServiceId))
                {
                    return JsonError("ServiceId is required.", 400);
                }

                var service = _db.FindServiceByIdOrCode(model.ServiceId);
                if (service == null) return JsonError("Service not found.", 404);

                var student = _db.Students.Find(s => s.UserId == user.Id).FirstOrDefault();

                var reqNumber = _db.GetNextRequestNumber();
                var req = new ServiceRequest
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
                    Priority = string.IsNullOrWhiteSpace(model.Priority) ? "Medium" : model.Priority,
                    Status = "Pending",
                    SubmissionDate = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    FormDetails = model.FormDetails ?? new Dictionary<string, string>(),
                    Remarks = model.Remarks ?? "Submitted via REST API",
                    StatusHistory = new List<StatusHistoryItem>
                    {
                        new StatusHistoryItem
                        {
                            Status = "Pending",
                            ChangedBy = user.FullName,
                            Role = user.Role,
                            Timestamp = DateTime.UtcNow,
                            Comment = "Service request created via REST API."
                        }
                    }
                };

                _db.ServiceRequests.InsertOne(req);
                return JsonOk(req, "Service request submitted successfully.", 201);
            }
            else if (method == "PUT")
            {
                var user = GetAuthenticatedUser();
                if (user == null) return JsonError("Unauthorized.", 401);

                var req = _db.FindRequestByIdOrNumber(id);
                if (req == null) return JsonError("Service request not found.", 404);

                // Only Admin or Faculty can change status/assign
                if (user.Role != "Admin" && user.Role != "Faculty")
                {
                    return JsonError("Forbidden. Only Faculty and Admins can update service request status.", 403);
                }

                var updateModel = ParseJsonBody<ServiceRequest>();
                if (updateModel == null) return JsonError("Invalid update payload.", 400);

                var newStatus = updateModel.Status ?? req.Status;
                var comment = updateModel.Remarks ?? $"Status updated to {newStatus}";

                var historyItem = new StatusHistoryItem
                {
                    Status = newStatus,
                    ChangedBy = user.FullName,
                    Role = user.Role,
                    Timestamp = DateTime.UtcNow,
                    Comment = comment
                };

                var update = Builders<ServiceRequest>.Update
                    .Set(r => r.Status, newStatus)
                    .Set(r => r.Priority, updateModel.Priority ?? req.Priority)
                    .Set(r => r.Remarks, comment)
                    .Set(r => r.AssignedToId, updateModel.AssignedToId ?? req.AssignedToId)
                    .Set(r => r.AssignedToName, updateModel.AssignedToName ?? req.AssignedToName)
                    .Set(r => r.UpdatedAt, DateTime.UtcNow)
                    .Push(r => r.StatusHistory, historyItem);

                _db.ServiceRequests.UpdateOne(r => r.Id == req.Id, update);
                var updated = _db.ServiceRequests.Find(r => r.Id == req.Id).FirstOrDefault();

                return JsonOk(updated, "Service request updated successfully.");
            }
            else if (method == "DELETE")
            {
                var user = GetAuthenticatedUser();
                if (user == null || user.Role != "Admin") return JsonError("Admin authorization required.", 403);

                var reqToDelete = _db.FindRequestByIdOrNumber(id);
                if (reqToDelete == null) return JsonError("Request not found.", 404);

                var res = _db.ServiceRequests.DeleteOne(r => r.Id == reqToDelete.Id);
                return JsonOk(new { id }, "Request deleted successfully.");
            }

            return JsonError("Method not allowed.", 405);
        }

        // ==========================================
        // 6. COMPLAINT APIS
        // ==========================================

        // POST /api/complaints, GET /api/complaints, PUT /api/complaints/:id
        public ActionResult ComplaintsEndpoint(string id)
        {
            var method = Request.HttpMethod.ToUpperInvariant();

            if (method == "GET")
            {
                if (string.IsNullOrEmpty(id))
                {
                    var list = _db.Complaints.Find(FilterDefinition<Complaint>.Empty).SortByDescending(c => c.CreatedAt).ToList();
                    return JsonOk(list);
                }
                else
                {
                    var complaint = _db.FindComplaintByIdOrNumber(id);
                    if (complaint == null) return JsonError("Complaint not found.", 404);
                    return JsonOk(complaint);
                }
            }
            else if (method == "POST")
            {
                var user = GetAuthenticatedUser();
                if (user == null) return JsonError("Unauthorized.", 401);

                var model = ParseJsonBody<Complaint>();
                if (model == null || string.IsNullOrWhiteSpace(model.Subject) || string.IsNullOrWhiteSpace(model.Description))
                {
                    return JsonError("Subject and Description are required.", 400);
                }

                var student = _db.Students.Find(s => s.UserId == user.Id).FirstOrDefault();
                var cmpNumber = _db.GetNextComplaintNumber();

                var complaint = new Complaint
                {
                    ComplaintNumber = cmpNumber,
                    StudentId = user.Id,
                    StudentName = user.FullName,
                    StudentRollNo = student?.RollNumber ?? "N/A",
                    Department = student?.Department ?? "General",
                    Category = model.Category ?? "General",
                    Subject = model.Subject.Trim(),
                    Description = model.Description.Trim(),
                    Location = model.Location ?? "Campus",
                    Priority = string.IsNullOrWhiteSpace(model.Priority) ? "Medium" : model.Priority,
                    Status = "Open",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.Complaints.InsertOne(complaint);
                return JsonOk(complaint, "Complaint registered successfully.", 201);
            }
            else if (method == "PUT")
            {
                var user = GetAuthenticatedUser();
                if (user == null) return JsonError("Unauthorized.", 401);

                var complaint = _db.FindComplaintByIdOrNumber(id);
                if (complaint == null) return JsonError("Complaint not found.", 404);

                var updateModel = ParseJsonBody<Complaint>();
                if (updateModel == null) return JsonError("Invalid payload.", 400);

                var updateBuilder = Builders<Complaint>.Update
                    .Set(c => c.Status, updateModel.Status ?? complaint.Status)
                    .Set(c => c.OfficialResponse, updateModel.OfficialResponse ?? complaint.OfficialResponse)
                    .Set(c => c.AssignedToId, updateModel.AssignedToId ?? complaint.AssignedToId)
                    .Set(c => c.AssignedToName, updateModel.AssignedToName ?? complaint.AssignedToName)
                    .Set(c => c.UpdatedAt, DateTime.UtcNow);

                if (!string.IsNullOrWhiteSpace(updateModel.OfficialResponse))
                {
                    updateBuilder = updateBuilder.Push(c => c.Responses, new ComplaintResponseItem
                    {
                        ResponderId = user.Id,
                        ResponderName = user.FullName,
                        Role = user.Role,
                        Message = updateModel.OfficialResponse.Trim(),
                        Timestamp = DateTime.UtcNow
                    });
                }

                if (updateModel.Status == "Resolved" || updateModel.Status == "Closed")
                {
                    updateBuilder = updateBuilder.Set(c => c.ResolutionDate, DateTime.UtcNow);
                }

                _db.Complaints.UpdateOne(c => c.Id == complaint.Id, updateBuilder);
                var updated = _db.Complaints.Find(c => c.Id == complaint.Id).FirstOrDefault();

                return JsonOk(updated, "Complaint updated successfully.");
            }

            return JsonError("Method not allowed.", 405);
        }

        // ==========================================
        // 7. ANNOUNCEMENT APIS
        // ==========================================

        // GET /api/announcements, POST /api/announcements, PUT /api/announcements/:id, DELETE /api/announcements/:id
        public ActionResult AnnouncementsEndpoint(string id)
        {
            var method = Request.HttpMethod.ToUpperInvariant();

            if (method == "GET")
            {
                if (string.IsNullOrEmpty(id))
                {
                    var list = _db.Announcements.Find(a => a.IsActive).SortByDescending(a => a.CreatedAt).ToList();
                    return JsonOk(list);
                }
                else
                {
                    var item = _db.Announcements.Find(a => a.Id == id).FirstOrDefault();
                    if (item == null) return JsonError("Announcement not found.", 404);
                    return JsonOk(item);
                }
            }
            else if (method == "POST")
            {
                var user = GetAuthenticatedUser();
                if (user == null || user.Role != "Admin") return JsonError("Admin authorization required.", 403);

                var model = ParseJsonBody<Announcement>();
                if (model == null || string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Content))
                {
                    return JsonError("Title and Content are required.", 400);
                }

                model.AuthorName = user.FullName;
                model.CreatedAt = DateTime.UtcNow;
                _db.Announcements.InsertOne(model);

                return JsonOk(model, "Announcement created successfully.", 201);
            }
            else if (method == "PUT")
            {
                var user = GetAuthenticatedUser();
                if (user == null || user.Role != "Admin") return JsonError("Admin authorization required.", 403);

                var item = _db.Announcements.Find(a => a.Id == id).FirstOrDefault();
                if (item == null) return JsonError("Announcement not found.", 404);

                var model = ParseJsonBody<Announcement>();
                if (model == null) return JsonError("Invalid payload.", 400);

                var update = Builders<Announcement>.Update
                    .Set(a => a.Title, model.Title ?? item.Title)
                    .Set(a => a.Content, model.Content ?? item.Content)
                    .Set(a => a.Category, model.Category ?? item.Category)
                    .Set(a => a.TargetRole, model.TargetRole ?? item.TargetRole)
                    .Set(a => a.Priority, model.Priority ?? item.Priority)
                    .Set(a => a.IsActive, model.IsActive);

                _db.Announcements.UpdateOne(a => a.Id == item.Id, update);
                var updated = _db.Announcements.Find(a => a.Id == item.Id).FirstOrDefault();

                return JsonOk(updated, "Announcement updated successfully.");
            }
            else if (method == "DELETE")
            {
                var user = GetAuthenticatedUser();
                if (user == null || user.Role != "Admin") return JsonError("Admin authorization required.", 403);

                var res = _db.Announcements.DeleteOne(a => a.Id == id);
                if (res.DeletedCount == 0) return JsonError("Announcement not found.", 404);

                return JsonOk(new { id }, "Announcement deleted successfully.");
            }

            return JsonError("Method not allowed.", 405);
        }

        // ==========================================
        // 8. FEEDBACK APIS
        // ==========================================

        // POST /api/feedback, GET /api/feedback
        public ActionResult FeedbackEndpoint()
        {
            var method = Request.HttpMethod.ToUpperInvariant();

            if (method == "GET")
            {
                var list = _db.Feedback.Find(FilterDefinition<CampusFeedback>.Empty).SortByDescending(f => f.CreatedAt).ToList();
                return JsonOk(list);
            }
            else if (method == "POST")
            {
                var user = GetAuthenticatedUser();
                if (user == null) return JsonError("Unauthorized.", 401);

                var model = ParseJsonBody<CampusFeedback>();
                if (model == null || string.IsNullOrWhiteSpace(model.ServiceId))
                {
                    return JsonError("ServiceId and Rating are required.", 400);
                }

                var service = _db.Services.Find(s => s.Id == model.ServiceId || s.ServiceCode == model.ServiceId).FirstOrDefault();

                var feedback = new CampusFeedback
                {
                    StudentId = user.Id,
                    StudentName = user.FullName,
                    ServiceId = service?.Id ?? model.ServiceId,
                    ServiceName = service?.Name ?? model.ServiceName ?? "General Campus Service",
                    Category = service?.Category ?? model.Category ?? "General",
                    Rating = model.Rating > 0 ? model.Rating : 5,
                    Comments = model.Comments?.Trim() ?? "",
                    CreatedAt = DateTime.UtcNow
                };

                _db.Feedback.InsertOne(feedback);
                return JsonOk(feedback, "Feedback recorded successfully.", 201);
            }

            return JsonError("Method not allowed.", 405);
        }

        // ==========================================
        // 9. ANALYTICS API
        // ==========================================

        // GET /api/analytics/dashboard
        [HttpGet]
        public ActionResult AnalyticsDashboard()
        {
            var requests = _db.ServiceRequests.Find(FilterDefinition<ServiceRequest>.Empty).ToList();
            var complaints = _db.Complaints.Find(FilterDefinition<Complaint>.Empty).ToList();
            var feedbacks = _db.Feedback.Find(FilterDefinition<CampusFeedback>.Empty).ToList();

            var data = new
            {
                totalStudents = _db.Students.CountDocuments(FilterDefinition<Student>.Empty),
                totalFaculty = _db.Faculty.CountDocuments(FilterDefinition<FacultyMember>.Empty),
                totalServices = _db.Services.CountDocuments(FilterDefinition<CampusService>.Empty),
                totalRequests = requests.Count,
                pendingRequests = requests.Count(r => r.Status == "Pending"),
                inProgressRequests = requests.Count(r => r.Status == "In Progress"),
                approvedRequests = requests.Count(r => r.Status == "Approved"),
                completedRequests = requests.Count(r => r.Status == "Completed"),
                rejectedRequests = requests.Count(r => r.Status == "Rejected"),
                totalComplaints = complaints.Count,
                resolvedComplaints = complaints.Count(c => c.Status == "Resolved" || c.Status == "Closed"),
                averageRating = feedbacks.Any() ? Math.Round(feedbacks.Average(f => f.Rating), 1) : 5.0,
                requestsByStatus = requests.GroupBy(r => r.Status ?? "Pending").ToDictionary(g => g.Key, g => g.Count()),
                requestsByCategory = requests.GroupBy(r => r.ServiceCategory ?? "Other").ToDictionary(g => g.Key, g => g.Count())
            };

            return JsonOk(data);
        }
    }
}

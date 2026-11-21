using System;
using System.Web.Mvc;
using MongoDB.Driver;
using Smart_Campus_Service_Portal.Helpers;
using Smart_Campus_Service_Portal.Models;
using Smart_Campus_Service_Portal.Models.ViewModels;

namespace Smart_Campus_Service_Portal.Controllers
{
    public class AccountController : Controller
    {
        private readonly MongoDbContext _db = MongoDbContext.Instance;

        // GET: Account/Login
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            if (AuthSessionHelper.IsAuthenticated)
            {
                return RedirectToDashboard(AuthSessionHelper.CurrentUserRole);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = _db.Users.Find(u => u.Email.ToLower() == model.Email.ToLower().Trim()).FirstOrDefault();
            if (user == null || !PasswordHasher.VerifyPassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError("", "Your account has been deactivated. Please contact campus administration.");
                return View(model);
            }

            AuthSessionHelper.SignIn(user, model.RememberMe);

            TempData["SuccessMessage"] = $"Welcome back, {user.FullName}!";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToDashboard(user.Role);
        }

        // GET: Account/Register (Student)
        [HttpGet]
        public ActionResult Register()
        {
            if (AuthSessionHelper.IsAuthenticated)
            {
                return RedirectToDashboard(AuthSessionHelper.CurrentUserRole);
            }
            return View(new RegisterViewModel());
        }

        // POST: Account/Register (Student)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check duplicate email
            var existingUser = _db.Users.Find(u => u.Email.ToLower() == model.Email.ToLower().Trim()).FirstOrDefault();
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email already exists.");
                return View(model);
            }

            // Check duplicate Roll Number
            var existingStudent = _db.Students.Find(s => s.RollNumber.ToUpper() == model.RollNumber.ToUpper().Trim()).FirstOrDefault();
            if (existingStudent != null)
            {
                ModelState.AddModelError("RollNumber", "A student with this Roll Number is already registered.");
                return View(model);
            }

            var user = new User
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.ToLower().Trim(),
                Username = model.Username.Trim(),
                PasswordHash = PasswordHasher.HashPassword(model.Password),
                Role = "Student",
                Phone = model.Phone?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Users.InsertOne(user);

            var student = new Student
            {
                UserId = user.Id,
                RollNumber = model.RollNumber.ToUpper().Trim(),
                Department = model.Department,
                Degree = model.Degree,
                Semester = model.Semester,
                AcademicYear = $"{DateTime.UtcNow.Year}-{DateTime.UtcNow.Year + 1}",
                Address = model.Address?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _db.Students.InsertOne(student);

            // Add welcome notification
            _db.Notifications.InsertOne(new Notification
            {
                UserId = user.Id,
                Title = "Welcome to Smart Campus Service Portal!",
                Message = "Your student account is active. You can now request campus services, view announcements, and track complaints.",
                Type = "success",
                Link = "/Student",
                CreatedAt = DateTime.UtcNow
            });

            AuthSessionHelper.SignIn(user);
            TempData["SuccessMessage"] = "Registration successful! Welcome to the Smart Campus Service Portal.";
            return RedirectToAction("Index", "Student");
        }

        // GET: Account/FacultyRegister
        [HttpGet]
        public ActionResult FacultyRegister()
        {
            if (AuthSessionHelper.IsAuthenticated && !AuthSessionHelper.IsInRole("Admin"))
            {
                return RedirectToDashboard(AuthSessionHelper.CurrentUserRole);
            }
            return View(new FacultyRegisterViewModel());
        }

        // POST: Account/FacultyRegister
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult FacultyRegister(FacultyRegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = _db.Users.Find(u => u.Email.ToLower() == model.Email.ToLower().Trim()).FirstOrDefault();
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email already exists.");
                return View(model);
            }

            var existingFaculty = _db.Faculty.Find(f => f.FacultyId.ToUpper() == model.FacultyId.ToUpper().Trim()).FirstOrDefault();
            if (existingFaculty != null)
            {
                ModelState.AddModelError("FacultyId", "A faculty member with this Employee ID is already registered.");
                return View(model);
            }

            var user = new User
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.ToLower().Trim(),
                Username = model.Username.Trim(),
                PasswordHash = PasswordHasher.HashPassword(model.Password),
                Role = "Faculty",
                Phone = model.Phone?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Users.InsertOne(user);

            var faculty = new FacultyMember
            {
                UserId = user.Id,
                FacultyId = model.FacultyId.ToUpper().Trim(),
                Department = model.Department,
                Designation = model.Designation,
                Specialization = model.Specialization?.Trim(),
                OfficeRoom = model.OfficeRoom?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _db.Faculty.InsertOne(faculty);

            if (!AuthSessionHelper.IsAuthenticated)
            {
                AuthSessionHelper.SignIn(user);
                TempData["SuccessMessage"] = "Faculty registration completed successfully! Welcome aboard.";
                return RedirectToAction("Index", "Faculty");
            }
            else
            {
                TempData["SuccessMessage"] = $"Faculty member '{user.FullName}' registered successfully.";
                return RedirectToAction("Faculty", "Admin");
            }
        }

        // GET: Account/ForgotPassword
        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = _db.Users.Find(u => u.Email.ToLower() == model.Email.ToLower().Trim()).FirstOrDefault();
            if (user == null)
            {
                ModelState.AddModelError("Email", "No registered account found with this email address.");
                return View(model);
            }

            var update = Builders<User>.Update
                .Set(u => u.PasswordHash, PasswordHasher.HashPassword(model.NewPassword))
                .Set(u => u.UpdatedAt, DateTime.UtcNow);

            _db.Users.UpdateOne(u => u.Id == user.Id, update);

            TempData["SuccessMessage"] = "Password has been successfully reset. Please log in with your new credentials.";
            return RedirectToAction("Login");
        }

        // GET: Account/Profile
        [HttpGet]
        [AuthorizeRole("Student", "Faculty", "Admin")]
        public new ActionResult Profile()
        {
            var user = AuthSessionHelper.GetCurrentUser();
            if (user == null) return RedirectToAction("Login");

            var model = new ProfileViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Username = user.Username,
                Role = user.Role,
                Phone = user.Phone
            };

            if (user.Role == "Student")
            {
                var student = _db.Students.Find(s => s.UserId == user.Id).FirstOrDefault();
                if (student != null)
                {
                    model.Identifier = student.RollNumber;
                    model.Department = student.Department;
                    model.Degree = student.Degree;
                    model.Semester = student.Semester;
                    model.Address = student.Address;
                    model.EmergencyContact = student.EmergencyContact;
                }
            }
            else if (user.Role == "Faculty")
            {
                var faculty = _db.Faculty.Find(f => f.UserId == user.Id).FirstOrDefault();
                if (faculty != null)
                {
                    model.Identifier = faculty.FacultyId;
                    model.Department = faculty.Department;
                    model.Designation = faculty.Designation;
                    model.OfficeRoom = faculty.OfficeRoom;
                }
            }
            else
            {
                model.Identifier = "ADMIN-001";
                model.Department = "Campus Administration";
                model.Designation = "Director / Super Administrator";
            }

            return View(model);
        }

        // POST: Account/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole("Student", "Faculty", "Admin")]
        public new ActionResult Profile(ProfileViewModel model)
        {
            var user = AuthSessionHelper.GetCurrentUser();
            if (user == null) return RedirectToAction("Login");

            var userUpdate = Builders<User>.Update
                .Set(u => u.FullName, model.FullName?.Trim())
                .Set(u => u.Phone, model.Phone?.Trim())
                .Set(u => u.UpdatedAt, DateTime.UtcNow);

            _db.Users.UpdateOne(u => u.Id == user.Id, userUpdate);

            if (user.Role == "Student")
            {
                var studentUpdate = Builders<Student>.Update
                    .Set(s => s.Address, model.Address?.Trim())
                    .Set(s => s.EmergencyContact, model.EmergencyContact?.Trim());
                _db.Students.UpdateOne(s => s.UserId == user.Id, studentUpdate);
            }
            else if (user.Role == "Faculty")
            {
                var facultyUpdate = Builders<FacultyMember>.Update
                    .Set(f => f.OfficeRoom, model.OfficeRoom?.Trim());
                _db.Faculty.UpdateOne(f => f.UserId == user.Id, facultyUpdate);
            }

            TempData["SuccessMessage"] = "Profile details updated successfully.";
            return RedirectToAction("Profile");
        }

        // GET: Account/ChangePassword
        [HttpGet]
        [AuthorizeRole("Student", "Faculty", "Admin")]
        public ActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole("Student", "Faculty", "Admin")]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = AuthSessionHelper.GetCurrentUser();
            if (user == null) return RedirectToAction("Login");

            if (!PasswordHasher.VerifyPassword(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "Incorrect current password.");
                return View(model);
            }

            var update = Builders<User>.Update
                .Set(u => u.PasswordHash, PasswordHasher.HashPassword(model.NewPassword))
                .Set(u => u.UpdatedAt, DateTime.UtcNow);

            _db.Users.UpdateOne(u => u.Id == user.Id, update);

            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToAction("Profile");
        }

        // GET/POST: Account/Logout
        public ActionResult Logout()
        {
            AuthSessionHelper.SignOut();
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        private ActionResult RedirectToDashboard(string role)
        {
            if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Admin");
            if (string.Equals(role, "Faculty", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Faculty");

            return RedirectToAction("Index", "Student");
        }
    }
}

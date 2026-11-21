using System.ComponentModel.DataAnnotations;

namespace Smart_Campus_Service_Portal.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Roll Number / Student ID is required.")]
        [Display(Name = "Student Roll Number")]
        public string RollNumber { get; set; }

        [Required(ErrorMessage = "Department is required.")]
        public string Department { get; set; }

        [Required(ErrorMessage = "Degree program is required.")]
        public string Degree { get; set; } = "B.Tech Computer Science";

        [Required(ErrorMessage = "Semester is required.")]
        [Range(1, 10, ErrorMessage = "Semester must be between 1 and 10.")]
        public int Semester { get; set; } = 1;

        [Phone(ErrorMessage = "Invalid phone number.")]
        public string Phone { get; set; }

        public string Address { get; set; }
    }

    public class FacultyRegisterViewModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Faculty Employee ID is required.")]
        public string FacultyId { get; set; }

        [Required(ErrorMessage = "Department is required.")]
        public string Department { get; set; }

        [Required(ErrorMessage = "Designation is required.")]
        public string Designation { get; set; } // Professor, Associate Professor, Assistant Professor, HOD

        public string Specialization { get; set; }

        [Phone]
        public string Phone { get; set; }

        public string OfficeRoom { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Registered email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class ProfileViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public string Phone { get; set; }
        public string Department { get; set; }
        public string Identifier { get; set; } // Roll Number or Faculty ID
        public string Degree { get; set; }
        public int? Semester { get; set; }
        public string Designation { get; set; }
        public string OfficeRoom { get; set; }
        public string Address { get; set; }
        public string EmergencyContact { get; set; }
    }
}

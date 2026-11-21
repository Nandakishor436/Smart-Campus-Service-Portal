using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Smart_Campus_Service_Portal.Models.ViewModels
{
    public class UserListItemViewModel
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string Department { get; set; }
        public string Identifier { get; set; } // Roll Number or Faculty ID
        public string Phone { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        [Required]
        public string Role { get; set; } // Admin, Faculty, Student

        public string Department { get; set; }
        public string Identifier { get; set; } // Roll Number or Faculty ID
        public string Designation { get; set; }
        public string Phone { get; set; }
    }

    public class AnnouncementFormViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Content is required.")]
        public string Content { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        public string Category { get; set; } // Academic, Event, Administrative, Holiday, Examination, Urgent

        public string TargetRole { get; set; } = "All"; // All, Student, Faculty

        public string Priority { get; set; } = "Medium"; // Low, Medium, High, Urgent

        public bool IsActive { get; set; } = true;

        public DateTime? ExpiryDate { get; set; }
    }

    public class ReportsFilterViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string ServiceCategory { get; set; }
        public string Status { get; set; }
        public string Department { get; set; }
        public List<ServiceRequest> FilteredRequests { get; set; } = new List<ServiceRequest>();
        public List<Complaint> FilteredComplaints { get; set; } = new List<Complaint>();
    }
}

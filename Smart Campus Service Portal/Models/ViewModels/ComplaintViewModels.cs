using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Smart_Campus_Service_Portal.Models.ViewModels
{
    public class ComplaintSubmitViewModel
    {
        [Required(ErrorMessage = "Category is required.")]
        public string Category { get; set; }

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Subject must be between 5 and 200 characters.")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Detailed description is required.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Location is required (e.g. Block A, Room 302, Hostel 2).")]
        public string Location { get; set; }

        [Required]
        public string Priority { get; set; } = "Medium";
    }

    public class ComplaintDetailViewModel
    {
        public Complaint Complaint { get; set; }
        public bool CanRespond { get; set; }
        public bool CanAssign { get; set; }
        public List<FacultyMember> AvailableFaculty { get; set; } = new List<FacultyMember>();
    }

    public class FeedbackSubmitViewModel
    {
        [Required]
        public string ServiceId { get; set; }

        public string ServiceName { get; set; }

        public string Category { get; set; }

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; } = 5;

        [Required(ErrorMessage = "Comments are required.")]
        public string Comments { get; set; }
    }
}

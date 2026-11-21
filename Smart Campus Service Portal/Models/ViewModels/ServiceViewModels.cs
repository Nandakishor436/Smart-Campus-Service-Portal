using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Smart_Campus_Service_Portal.Models.ViewModels
{
    public class ServiceRequestSubmitViewModel
    {
        [Required]
        public string ServiceId { get; set; }

        public string ServiceName { get; set; }
        public string ServiceCategory { get; set; }
        public string Description { get; set; }

        [Required]
        public string Priority { get; set; } = "Medium";

        public Dictionary<string, string> FormDetails { get; set; } = new Dictionary<string, string>();
    }

    public class ServiceRequestDetailViewModel
    {
        public ServiceRequest Request { get; set; }
        public CampusService Service { get; set; }
        public bool CanUpdateStatus { get; set; }
        public bool CanAssign { get; set; }
        public List<FacultyMember> AvailableFaculty { get; set; } = new List<FacultyMember>();
    }

    public class CampusServiceManageViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Service Code is required.")]
        public string ServiceCode { get; set; }

        [Required(ErrorMessage = "Service Name is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        public string Category { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; }

        public string Icon { get; set; } = "fa-cogs";

        public int EstimatedDays { get; set; } = 3;

        [Required(ErrorMessage = "Department is required.")]
        public string Department { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

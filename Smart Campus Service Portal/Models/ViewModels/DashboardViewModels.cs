using System.Collections.Generic;

namespace Smart_Campus_Service_Portal.Models.ViewModels
{
    public class StudentDashboardViewModel
    {
        public User User { get; set; }
        public Student Student { get; set; }
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int InProgressRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int CompletedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public int TotalComplaints { get; set; }
        public int ResolvedComplaints { get; set; }
        public List<ServiceRequest> RecentRequests { get; set; } = new List<ServiceRequest>();
        public List<CampusService> FeaturedServices { get; set; } = new List<CampusService>();
        public List<Announcement> RecentAnnouncements { get; set; } = new List<Announcement>();
        public List<Notification> Notifications { get; set; } = new List<Notification>();
    }

    public class FacultyDashboardViewModel
    {
        public User User { get; set; }
        public FacultyMember Faculty { get; set; }
        public int TotalAssignedRequests { get; set; }
        public int PendingApprovalRequests { get; set; }
        public int InProgressRequests { get; set; }
        public int CompletedRequests { get; set; }
        public int AssignedComplaints { get; set; }
        public int ResolvedComplaints { get; set; }
        public List<ServiceRequest> AssignedRequests { get; set; } = new List<ServiceRequest>();
        public List<Complaint> AssignedComplaintsList { get; set; } = new List<Complaint>();
        public List<Announcement> Announcements { get; set; } = new List<Announcement>();
        public List<CampusFeedback> RecentFeedback { get; set; } = new List<CampusFeedback>();
    }

    public class AdminDashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalFaculty { get; set; }
        public int TotalServices { get; set; }
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int InProgressRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int CompletedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public int TotalComplaints { get; set; }
        public int OpenComplaints { get; set; }
        public int ResolvedComplaints { get; set; }
        public double AverageFeedbackRating { get; set; }
        public int TotalFeedback { get; set; }

        public List<ServiceRequest> RecentRequests { get; set; } = new List<ServiceRequest>();
        public List<Complaint> RecentComplaints { get; set; } = new List<Complaint>();

        // Chart Data
        public Dictionary<string, int> RequestsByStatus { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> RequestsByCategory { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> ComplaintsByCategory { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> MonthlyRequestTrends { get; set; } = new Dictionary<string, int>();
    }
}

using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Smart_Campus_Service_Portal.Models
{
    public class ServiceRequest
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string RequestNumber { get; set; }

        public string ServiceId { get; set; }

        public string ServiceName { get; set; }

        public string ServiceCategory { get; set; }

        public string StudentId { get; set; }

        public string StudentName { get; set; }

        public string StudentEmail { get; set; }

        public string StudentRollNo { get; set; }

        public string Department { get; set; }

        public string Priority { get; set; } = "Medium"; // Low, Medium, High, Urgent

        /// <summary>
        /// Pending, In Progress, Approved, Rejected, Completed
        /// </summary>
        public string Status { get; set; } = "Pending";

        public string AssignedToId { get; set; }

        public string AssignedToName { get; set; }

        public string AssignedRole { get; set; }

        public Dictionary<string, string> FormDetails { get; set; } = new Dictionary<string, string>();

        public string Remarks { get; set; }

        public List<StatusHistoryItem> StatusHistory { get; set; } = new List<StatusHistoryItem>();

        public DateTime SubmissionDate { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class StatusHistoryItem
    {
        public string Status { get; set; }
        public string ChangedBy { get; set; }
        public string Role { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Comment { get; set; }
    }
}

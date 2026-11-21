using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Smart_Campus_Service_Portal.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Username { get; set; }

        public string Email { get; set; }

        public string PasswordHash { get; set; }

        public string FullName { get; set; }

        /// <summary>
        /// Roles: "Admin", "Faculty", "Student"
        /// </summary>
        public string Role { get; set; }

        public string Phone { get; set; }

        public string ProfilePicture { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

using System;
using System.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Smart_Campus_Service_Portal.Models
{
    public class MongoDbContext
    {
        private static readonly Lazy<MongoDbContext> _lazy = new Lazy<MongoDbContext>(() => new MongoDbContext());
        public static MongoDbContext Instance => _lazy.Value;

        private readonly IMongoDatabase _database;

        public MongoDbContext()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["MongoDbConnection"]?.ConnectionString ?? "mongodb://localhost:27017";
            var databaseName = ConfigurationManager.ConnectionStrings["MongoDbDatabase"]?.ConnectionString ?? "SmartCampusDb";

            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);

            EnsureIndexes();
        }

        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
        public IMongoCollection<Student> Students => _database.GetCollection<Student>("Students");
        public IMongoCollection<FacultyMember> Faculty => _database.GetCollection<FacultyMember>("Faculty");
        public IMongoCollection<CampusService> Services => _database.GetCollection<CampusService>("Services");
        public IMongoCollection<ServiceRequest> ServiceRequests => _database.GetCollection<ServiceRequest>("ServiceRequests");
        public IMongoCollection<Complaint> Complaints => _database.GetCollection<Complaint>("Complaints");
        public IMongoCollection<Announcement> Announcements => _database.GetCollection<Announcement>("Announcements");
        public IMongoCollection<Notification> Notifications => _database.GetCollection<Notification>("Notifications");
        public IMongoCollection<CampusFeedback> Feedback => _database.GetCollection<CampusFeedback>("Feedback");

        private void EnsureIndexes()
        {
            try
            {
                // Unique Email for Users
                var emailIndexKeys = Builders<User>.IndexKeys.Ascending(u => u.Email);
                var emailIndexOptions = new CreateIndexOptions { Unique = true };
                Users.Indexes.CreateOne(new CreateIndexModel<User>(emailIndexKeys, emailIndexOptions));

                // Unique RollNumber for Students
                var rollIndexKeys = Builders<Student>.IndexKeys.Ascending(s => s.RollNumber);
                var rollIndexOptions = new CreateIndexOptions { Unique = true };
                Students.Indexes.CreateOne(new CreateIndexModel<Student>(rollIndexKeys, rollIndexOptions));
            }
            catch
            {
                // Index already exists or non-fatal
            }
        }

        public string GetNextRequestNumber()
        {
            var count = ServiceRequests.CountDocuments(FilterDefinition<ServiceRequest>.Empty) + 1;
            return $"REQ-{DateTime.UtcNow.Year}-{count:D4}";
        }

        public string GetNextComplaintNumber()
        {
            var count = Complaints.CountDocuments(FilterDefinition<Complaint>.Empty) + 1;
            return $"CMP-{DateTime.UtcNow.Year}-{count:D4}";
        }

        public ServiceRequest FindRequestByIdOrNumber(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (ObjectId.TryParse(id, out _))
            {
                var byId = ServiceRequests.Find(r => r.Id == id).FirstOrDefault();
                if (byId != null) return byId;
            }
            return ServiceRequests.Find(r => r.RequestNumber == id).FirstOrDefault();
        }

        public Complaint FindComplaintByIdOrNumber(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (ObjectId.TryParse(id, out _))
            {
                var byId = Complaints.Find(c => c.Id == id).FirstOrDefault();
                if (byId != null) return byId;
            }
            return Complaints.Find(c => c.ComplaintNumber == id).FirstOrDefault();
        }

        public CampusService FindServiceByIdOrCode(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (ObjectId.TryParse(id, out _))
            {
                var byId = Services.Find(s => s.Id == id).FirstOrDefault();
                if (byId != null) return byId;
            }
            return Services.Find(s => s.ServiceCode == id).FirstOrDefault();
        }
    }
}

using System.Xml;

namespace RevisionPlanner.Models
{
    public class User
    {
        public int UserId { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string EmailId { get; set; } = string.Empty;

        public string EmailPassword { get; set; } = string.Empty;

        public int Year { get; set; }

        // Navigation properties (optional but recommended)
        public ICollection<Subject>? Subjects { get; set; }
        public ICollection<Timetable>? Timetables { get; set; }
    }
}

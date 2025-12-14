namespace RevisionPlanner.Models
{
    public class Timetable
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public DateTime TimeTableDate { get; set; }

        public int? SlotNumber { get; set; }        // Optional
        public int? SubjectId { get; set; }          // Optional
        public string? Status { get; set; }          // Optional (Planned, Completed, Incomplete)

        // Navigation properties
        public User? User { get; set; }
        public Subject? Subject { get; set; }
    }
}

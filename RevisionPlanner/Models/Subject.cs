using RevisionPlanner.Enums;

namespace RevisionPlanner.Models
{
    public class Subject
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string SubjectName { get; set; } = string.Empty;

        public DifficultyLevel Difficulty { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public ICollection<Resource>? Resources { get; set; }
    }
}

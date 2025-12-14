using RevisionPlanner.Enums;
using System.ComponentModel.DataAnnotations;

namespace RevisionPlanner.Models.ViewModels
{
    public class CreateTimetableViewModel
    {
        public List<SubjectRow> Subjects { get; set; } = new();
    }

    public class SubjectRow
    {
        public int Id { get; set; } // 0 for new subjects

        [Required]
        public string SubjectName { get; set; } = string.Empty;

        [Required]
        public DifficultyLevel Difficulty { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ExamDate { get; set; }
    }
}

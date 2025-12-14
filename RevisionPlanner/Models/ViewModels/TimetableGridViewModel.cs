using RevisionPlanner.Models;
using System.Collections.Generic;

namespace RevisionPlanner.Models.ViewModels
{
    public class TimetableGridViewModel
    {
        public List<Subject> Subjects { get; set; } = new();
        public List<TimetableCell> Cells { get; set; } = new();
        public int SlotsPerDay { get; set; } = 8;
    }

    public class TimetableCell
    {
        public int TimetableId { get; set; }
        public DayOfWeek Day { get; set; }
        public int SlotNumber { get; set; }
        public int? SubjectId { get; set; }
        public string? Status { get; set; }
    }
}

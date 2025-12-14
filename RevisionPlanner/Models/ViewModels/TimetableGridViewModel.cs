using RevisionPlanner.Models;
using System;
using System.Collections.Generic;

namespace RevisionPlanner.Models.ViewModels
{
    public class TimetableGridViewModel
    {
        public List<Subject> Subjects { get; set; } = new();
        public List<TimetableCell> Cells { get; set; } = new();

        public int SlotsPerDay { get; set; } = 8;

        public DateTime WeekStart { get; set; }  // Monday
        public DateTime WeekEnd => WeekStart.AddDays(6);
        public DateTime SelectedDate { get; set; } // the date chosen from calendar
    }

    public class TimetableCell
    {
        public int TimetableId { get; set; }

        public DateTime Date { get; set; }

        public int SlotNumber { get; set; }

        public int? SubjectId { get; set; }

        public string? Status { get; set; } // Completed / Incomplete / null
    }
}

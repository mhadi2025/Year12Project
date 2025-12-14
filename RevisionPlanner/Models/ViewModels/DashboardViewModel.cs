using RevisionPlanner.Models;
using System;
using System.Collections.Generic;

namespace RevisionPlanner.Models.ViewModels
{
    public class DashboardViewModel
    {
        public DateTime WeekStart { get; set; }   // Monday
        public DateTime WeekEnd => WeekStart.AddDays(6);

        public List<Subject> Top5PrioritiesThisWeek { get; set; } = new();
        public List<Subject> ExamsNext7Days { get; set; } = new();

        public List<SubjectCompletionRow> CompletionBySubject { get; set; } = new();
    }

    public class SubjectCompletionRow
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;

        public int ScheduledSlots { get; set; }
        public int CompletedSlots { get; set; }
        public int IncompleteSlots { get; set; }

        public int CompletionPercent
        {
            get
            {
                if (ScheduledSlots <= 0) return 0;
                return (int)Math.Round((CompletedSlots * 100.0) / ScheduledSlots);
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RevisionPlanner.Data;
using RevisionPlanner.Models.ViewModels;
using RevisionPlanner.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RevisionPlanner.Controllers
{
    public class DashboardController : Controller
    {
        private readonly RevisionPlannerDbContext _context;

        public DashboardController(RevisionPlannerDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(DateTime? date)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var selected = (date ?? DateTime.Today).Date;
            var weekStart = GetWeekStartMonday(selected);
            var weekEnd = weekStart.AddDays(6);

            var subjects = await _context.Subjects
                .Where(s => s.UserId == userId.Value)
                .OrderBy(s => s.SubjectName)
                .ToListAsync();

            // Exams next 7 days (including today)
            var examsNext7 = subjects
                .Where(s => s.ExamDate.HasValue)
                .Where(s => s.ExamDate!.Value.Date >= DateTime.Today.Date && s.ExamDate!.Value.Date <= DateTime.Today.Date.AddDays(7))
                .OrderBy(s => s.ExamDate)
                .ToList();

            // Top 5 priorities this week (uses your RevisionPriority rules)
            // We prioritise among:
            // - subjects with exam within 7 days
            // - plus subjects scheduled in the current week (even if no exam date)
            var weekSubjectIds = await _context.Timetables
                .Where(t => t.UserId == userId.Value
                            && t.TimeTableDate.Date >= weekStart.Date
                            && t.TimeTableDate.Date <= weekEnd.Date
                            && t.SubjectId != null)
                .Select(t => t.SubjectId!.Value)
                .Distinct()
                .ToListAsync();

            var candidateSet = subjects
                .Where(s =>
                    (s.ExamDate.HasValue && s.ExamDate.Value.Date >= DateTime.Today.Date && s.ExamDate.Value.Date <= weekEnd.Date.AddDays(7))
                    || weekSubjectIds.Contains(s.Id))
                .ToList();

            var ordered = candidateSet
                .OrderBy(s => s, Comparer<RevisionPlanner.Models.Subject>.Create((a, b) =>
                    RevisionPriority.Compare(a, b, DateTime.Today)))
                .Take(5)
                .ToList();

            // Completion % by subject for this week (based on timetable statuses)
            var timetableWeek = await _context.Timetables
                .Where(t => t.UserId == userId.Value
                            && t.TimeTableDate.Date >= weekStart.Date
                            && t.TimeTableDate.Date <= weekEnd.Date
                            && t.SubjectId != null)
                .ToListAsync();

            var completionRows = timetableWeek
                .GroupBy(t => t.SubjectId!.Value)
                .Select(g =>
                {
                    var subj = subjects.FirstOrDefault(s => s.Id == g.Key);
                    var scheduled = g.Count();
                    var completed = g.Count(x => string.Equals(x.Status, "Completed", StringComparison.OrdinalIgnoreCase));
                    var incomplete = g.Count(x => string.Equals(x.Status, "Incomplete", StringComparison.OrdinalIgnoreCase));

                    return new SubjectCompletionRow
                    {
                        SubjectId = g.Key,
                        SubjectName = subj?.SubjectName ?? $"Subject #{g.Key}",
                        ScheduledSlots = scheduled,
                        CompletedSlots = completed,
                        IncompleteSlots = incomplete
                    };
                })
                .OrderByDescending(r => r.ScheduledSlots)
                .ThenBy(r => r.SubjectName)
                .ToList();

            var vm = new DashboardViewModel
            {
                WeekStart = weekStart,
                Top5PrioritiesThisWeek = ordered,
                ExamsNext7Days = examsNext7,
                CompletionBySubject = completionRows
            };

            return View(vm);
        }

        private static DateTime GetWeekStartMonday(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }
    }
}

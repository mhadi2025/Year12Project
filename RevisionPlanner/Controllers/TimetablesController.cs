using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RevisionPlanner.Data;
using RevisionPlanner.Enums;
using RevisionPlanner.Models;
using RevisionPlanner.Models.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RevisionPlanner.Controllers
{
    public class TimetablesController : Controller
    {
        private readonly RevisionPlannerDbContext _context;

        public TimetablesController(RevisionPlannerDbContext context)
        {
            _context = context;
        }

        // =======================
        // View / Edit Timetable (Weekly)
        // =======================
        // /Timetables/Index?date=2025-12-14
        public async Task<IActionResult> Index(DateTime? date)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var selected = (date ?? DateTime.Today).Date;

            // Week starts Monday
            var weekStart = GetWeekStartMonday(selected);
            var weekEnd = weekStart.AddDays(6);

            // Load subjects (for drag panel)
            var subjects = await _context.Subjects
                .Where(s => s.UserId == userId.Value)
                .OrderBy(s => s.SubjectName)
                .ToListAsync();

            // Ensure timetable rows exist for this week (7 days x slots)
            const int slotsPerDay = 8;

            for (var d = weekStart; d <= weekEnd; d = d.AddDays(1))
            {
                for (int slot = 1; slot <= slotsPerDay; slot++)
                {
                    var exists = await _context.Timetables.AnyAsync(t =>
                        t.UserId == userId.Value &&
                        t.TimeTableDate.Date == d.Date &&
                        t.SlotNumber == slot);

                    if (!exists)
                    {
                        _context.Timetables.Add(new Timetable
                        {
                            UserId = userId.Value,
                            TimeTableDate = d.Date,
                            SlotNumber = slot,
                            SubjectId = null,
                            Status = null
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Load the week's cells
            var cells = await _context.Timetables
                .Where(t => t.UserId == userId.Value
                            && t.TimeTableDate.Date >= weekStart.Date
                            && t.TimeTableDate.Date <= weekEnd.Date
                            && t.SlotNumber != null)
                .OrderBy(t => t.TimeTableDate)
                .ThenBy(t => t.SlotNumber)
                .Select(t => new TimetableCell
                {
                    TimetableId = t.Id,
                    Date = t.TimeTableDate.Date,
                    SlotNumber = t.SlotNumber ?? 0,
                    SubjectId = t.SubjectId,
                    Status = t.Status
                })
                .ToListAsync();

            var vm = new TimetableGridViewModel
            {
                Subjects = subjects,
                Cells = cells,
                SlotsPerDay = slotsPerDay,
                WeekStart = weekStart.Date,
                SelectedDate = selected
            };

            return View(vm);
        }

        // =======================
        // Save Timetable Grid
        // =======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveGrid(TimetableGridViewModel vm)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            // load subjects once for exam-date validation
            var subjectMap = await _context.Subjects
                .Where(s => s.UserId == userId.Value)
                .ToDictionaryAsync(s => s.Id, s => s);

            foreach (var cell in vm.Cells)
            {
                var row = await _context.Timetables
                    .FirstOrDefaultAsync(t => t.Id == cell.TimetableId && t.UserId == userId.Value);

                if (row == null) continue;

                // ✅ Server-side rule: cannot schedule after exam date
                if (cell.SubjectId.HasValue && subjectMap.TryGetValue(cell.SubjectId.Value, out var subj))
                {
                    if (subj.ExamDate.HasValue)
                    {
                        var exam = subj.ExamDate.Value.Date;
                        var cellDate = row.TimeTableDate.Date;

                        if (cellDate > exam)
                        {
                            ModelState.AddModelError(string.Empty,
                                $"Cannot schedule '{subj.SubjectName}' on {cellDate:dd MMM yyyy} because its exam date is {exam:dd MMM yyyy}.");

                            // Do NOT save this invalid assignment. Keep existing DB value.
                            continue;
                        }
                    }
                }

                row.SubjectId = cell.SubjectId;
                row.Status = cell.Status; // Completed / Incomplete / null
            }

            if (!ModelState.IsValid)
            {
                // Return to same week view
                TempData["ErrorMessage"] = "Some changes were not saved due to exam date rules. Please check the messages and try again.";
                return RedirectToAction("Index", new { date = vm.WeekStart.Date.ToString("yyyy-MM-dd") });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Timetable saved successfully.";
            return RedirectToAction("Index", new { date = vm.WeekStart.Date.ToString("yyyy-MM-dd") });
        }

        // =======================
        // CREATE TIMETABLE (GET)
        // =======================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var existingSubjects = await _context.Subjects
                .Where(s => s.UserId == userId.Value)
                .OrderBy(s => s.SubjectName)
                .ToListAsync();

            // used subjects (to disable delete button)
            var usedSubjectIds = await _context.Timetables
                .Where(t => t.UserId == userId.Value && t.SubjectId != null)
                .Select(t => t.SubjectId!.Value)
                .Distinct()
                .ToListAsync();

            ViewBag.UsedSubjectIds = usedSubjectIds;

            var vm = new CreateTimetableViewModel
            {
                Subjects = existingSubjects.Select(s => new SubjectRow
                {
                    Id = s.Id,
                    SubjectName = s.SubjectName,
                    Difficulty = s.Difficulty,
                    ExamDate = s.ExamDate
                }).ToList()
            };

            if (!vm.Subjects.Any())
            {
                vm.Subjects.Add(new SubjectRow
                {
                    Difficulty = DifficultyLevel.Easy,
                    ExamDate = null
                });
            }

            return View(vm);
        }

        // =======================
        // CREATE TIMETABLE (POST)
        // =======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTimetableViewModel vm)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            vm.Subjects = vm.Subjects
                .Where(s => !string.IsNullOrWhiteSpace(s.SubjectName))
                .ToList();

            if (!ModelState.IsValid)
                return View(vm);

            var existingSubjects = await _context.Subjects
                .Where(s => s.UserId == userId.Value)
                .ToListAsync();

            var submittedIds = vm.Subjects
                .Where(s => s.Id != 0)
                .Select(s => s.Id)
                .ToHashSet();

            var toDelete = existingSubjects
                .Where(s => !submittedIds.Contains(s.Id))
                .ToList();

            // Do not allow delete if referenced in timetable
            if (toDelete.Any())
            {
                var toDeleteIds = toDelete.Select(s => s.Id).ToList();

                var referencedIds = await _context.Timetables
                    .Where(t => t.UserId == userId.Value
                                && t.SubjectId != null
                                && toDeleteIds.Contains(t.SubjectId.Value))
                    .Select(t => t.SubjectId!.Value)
                    .Distinct()
                    .ToListAsync();

                if (referencedIds.Any())
                {
                    var referencedNames = toDelete
                        .Where(s => referencedIds.Contains(s.Id))
                        .Select(s => s.SubjectName);

                    ModelState.AddModelError(string.Empty,
                        "You cannot delete these subjects because they are used in your timetable: "
                        + string.Join(", ", referencedNames)
                        + ". Remove them from the timetable first.");

                    return View(vm);
                }

                _context.Subjects.RemoveRange(toDelete);
            }

            // ✅ Rule 7: if exam date is added/changed, check if the subject is already scheduled beyond that date
            foreach (var row in vm.Subjects)
            {
                if (row.Id != 0)
                {
                    var existing = existingSubjects.First(s => s.Id == row.Id);

                    var oldExam = existing.ExamDate?.Date;
                    var newExam = row.ExamDate?.Date;

                    // if exam date changed OR added
                    if (newExam.HasValue && oldExam != newExam)
                    {
                        var hasBeyond = await _context.Timetables.AnyAsync(t =>
                            t.UserId == userId.Value &&
                            t.SubjectId == existing.Id &&
                            t.TimeTableDate.Date > newExam.Value);

                        if (hasBeyond)
                        {
                            ModelState.AddModelError(string.Empty,
                                $"Cannot set exam date for '{existing.SubjectName}' to {newExam.Value:dd MMM yyyy} because it is already scheduled after that date. Please update the timetable first.");

                            return View(vm);
                        }
                    }
                }
            }

            // Upsert
            foreach (var row in vm.Subjects)
            {
                if (row.Id == 0)
                {
                    _context.Subjects.Add(new Subject
                    {
                        UserId = userId.Value,
                        SubjectName = row.SubjectName.Trim(),
                        Difficulty = row.Difficulty,
                        ExamDate = row.ExamDate
                    });
                }
                else
                {
                    var subject = existingSubjects.First(s => s.Id == row.Id);
                    subject.SubjectName = row.SubjectName.Trim();
                    subject.Difficulty = row.Difficulty;
                    subject.ExamDate = row.ExamDate;
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Create", "Timetables");
        }

        private static DateTime GetWeekStartMonday(DateTime date)
        {
            // Monday = 1, Sunday = 0
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }
    }
}

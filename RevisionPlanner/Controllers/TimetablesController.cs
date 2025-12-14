using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        // View / Edit Timetable
        // =======================
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var subjects = await _context.Subjects
                .Where(s => s.UserId == userId.Value)
                .OrderBy(s => s.SubjectName)
                .ToListAsync();

            var cells = await _context.Timetables
                .Where(t => t.UserId == userId.Value && t.SlotNumber != null)
                .OrderBy(t => t.TimeTableDay)
                .ThenBy(t => t.SlotNumber)
                .Select(t => new RevisionPlanner.Models.ViewModels.TimetableCell
                {
                    TimetableId = t.Id,
                    Day = t.TimeTableDay,
                    SlotNumber = t.SlotNumber ?? 0,
                    SubjectId = t.SubjectId,
                    Status = t.Status
                })
                .ToListAsync();

            var vm = new RevisionPlanner.Models.ViewModels.TimetableGridViewModel
            {
                Subjects = subjects,
                Cells = cells,
                SlotsPerDay = 8
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveGrid(RevisionPlanner.Models.ViewModels.TimetableGridViewModel vm)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            foreach (var cell in vm.Cells)
            {
                var row = await _context.Timetables.FirstOrDefaultAsync(t => t.Id == cell.TimetableId && t.UserId == userId.Value);
                if (row != null)
                {
                    row.SubjectId = cell.SubjectId;
                    row.Status = cell.Status;
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Timetable saved successfully.";
            return RedirectToAction("Index");
        }


        // =======================
        // CREATE TIMETABLE
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

            var vm = new CreateTimetableViewModel
            {
                Subjects = existingSubjects.Select(s => new SubjectRow
                {
                    Id = s.Id,
                    SubjectName = s.SubjectName,
                    Difficulty = s.Difficulty
                }).ToList()
            };

            if (!vm.Subjects.Any())
            {
                vm.Subjects.Add(new SubjectRow
                {
                    Difficulty = DifficultyLevel.Easy
                });
            }

            return View(vm);
        }

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

            if (toDelete.Any())
                _context.Subjects.RemoveRange(toDelete);

            foreach (var row in vm.Subjects)
            {
                if (row.Id == 0)
                {
                    _context.Subjects.Add(new Subject
                    {
                        UserId = userId.Value,
                        SubjectName = row.SubjectName.Trim(),
                        Difficulty = row.Difficulty
                    });
                }
                else
                {
                    var subject = existingSubjects.First(s => s.Id == row.Id);
                    subject.SubjectName = row.SubjectName.Trim();
                    subject.Difficulty = row.Difficulty;
                }
            }

            await _context.SaveChangesAsync();

            // =======================
            // Initialise Timetable Grid (ONCE)
            // =======================
            const int slotsPerDay = 8;

            var days = new[]
            {
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday,
                DayOfWeek.Saturday,
                DayOfWeek.Sunday
            };

            var hasSlots = await _context.Timetables
                .AnyAsync(t => t.UserId == userId.Value);

            if (!hasSlots)
            {
                foreach (var day in days)
                {
                    for (int slot = 1; slot <= slotsPerDay; slot++)
                    {
                        _context.Timetables.Add(new Timetable
                        {
                            UserId = userId.Value,
                            TimeTableDay = day,
                            SlotNumber = slot,
                            SubjectId = null,
                            Status = null
                        });
                    }
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Timetables");
        }

        private bool TimetableExists(int id)
        {
            return _context.Timetables.Any(e => e.Id == id);
        }
    }
}

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
                .Select(t => new TimetableCell
                {
                    TimetableId = t.Id,
                    Day = t.TimeTableDay,
                    SlotNumber = t.SlotNumber ?? 0,
                    SubjectId = t.SubjectId,
                    Status = t.Status
                })
                .ToListAsync();

            var vm = new TimetableGridViewModel
            {
                Subjects = subjects,
                Cells = cells,
                SlotsPerDay = 8
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

            foreach (var cell in vm.Cells)
            {
                var row = await _context.Timetables
                    .FirstOrDefaultAsync(t => t.Id == cell.TimetableId && t.UserId == userId.Value);

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
                    ExamDate = s.ExamDate   // ✅ NEW
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

            foreach (var row in vm.Subjects)
            {
                if (row.Id == 0)
                {
                    _context.Subjects.Add(new Subject
                    {
                        UserId = userId.Value,
                        SubjectName = row.SubjectName.Trim(),
                        Difficulty = row.Difficulty,
                        ExamDate = row.ExamDate // ✅ NEW
                    });
                }
                else
                {
                    var subject = existingSubjects.First(s => s.Id == row.Id);
                    subject.SubjectName = row.SubjectName.Trim();
                    subject.Difficulty = row.Difficulty;
                    subject.ExamDate = row.ExamDate; // ✅ NEW
                }
            }

            await _context.SaveChangesAsync();

            // Initialise timetable once
            const int slotsPerDay = 8;
            var days = Enum.GetValues<DayOfWeek>();

            if (!await _context.Timetables.AnyAsync(t => t.UserId == userId.Value))
            {
                foreach (var day in days)
                {
                    for (int slot = 1; slot <= slotsPerDay; slot++)
                    {
                        _context.Timetables.Add(new Timetable
                        {
                            UserId = userId.Value,
                            TimeTableDay = day,
                            SlotNumber = slot
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Timetables");
        }
    }
}

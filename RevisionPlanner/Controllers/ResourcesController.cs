using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RevisionPlanner.Data;
using RevisionPlanner.Models;

namespace RevisionPlanner.Controllers
{
    public class ResourcesController : Controller
    {
        private readonly RevisionPlannerDbContext _context;

        public ResourcesController(RevisionPlannerDbContext context)
        {
            _context = context;
        }

        private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");

        // GET: Resources
        public async Task<IActionResult> Index()
        {
            if (CurrentUserId == null)
                return RedirectToAction("Login", "Account");

            var resources = await _context.Resources
                .Include(r => r.Subject)
                .Where(r => r.Subject != null && r.Subject.UserId == CurrentUserId.Value)
                .OrderBy(r => r.Subject!.SubjectName)
                .ThenBy(r => r.Title)
                .ToListAsync();

            return View(resources);
        }

        // GET: Resources/Create
        public async Task<IActionResult> Create()
        {
            if (CurrentUserId == null)
                return RedirectToAction("Login", "Account");

            await PopulateSubjectsDropdown();
            return View(new Resource());
        }

        // POST: Resources/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SubjectId,Title,Url")] Resource resource)
        {
            if (CurrentUserId == null)
                return RedirectToAction("Login", "Account");

            // Validate subject belongs to current user
            var subjectOk = await _context.Subjects.AnyAsync(s => s.Id == resource.SubjectId && s.UserId == CurrentUserId.Value);
            if (!subjectOk)
                ModelState.AddModelError(nameof(Resource.SubjectId), "Invalid subject selection.");

            if (!IsValidHttpUrl(resource.Url))
                ModelState.AddModelError(nameof(Resource.Url), "Please enter a valid URL starting with http:// or https://");

            if (ModelState.IsValid)
            {
                resource.Title = (resource.Title ?? "").Trim();
                resource.Url = (resource.Url ?? "").Trim();

                _context.Add(resource);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Resource added successfully.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateSubjectsDropdown(resource.SubjectId);
            return View(resource);
        }

        // GET: Resources/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (CurrentUserId == null)
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var resource = await _context.Resources
                .Include(r => r.Subject)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (resource == null) return NotFound();

            // Ensure it belongs to current user
            if (resource.Subject == null || resource.Subject.UserId != CurrentUserId.Value)
                return Forbid();

            await PopulateSubjectsDropdown(resource.SubjectId);
            return View(resource);
        }

        // POST: Resources/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SubjectId,Title,Url")] Resource resource)
        {
            if (CurrentUserId == null)
                return RedirectToAction("Login", "Account");

            if (id != resource.Id) return NotFound();

            // Load existing to check ownership
            var existing = await _context.Resources
                .Include(r => r.Subject)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (existing == null) return NotFound();
            if (existing.Subject == null || existing.Subject.UserId != CurrentUserId.Value) return Forbid();

            // Validate subject belongs to current user
            var subjectOk = await _context.Subjects.AnyAsync(s => s.Id == resource.SubjectId && s.UserId == CurrentUserId.Value);
            if (!subjectOk)
                ModelState.AddModelError(nameof(Resource.SubjectId), "Invalid subject selection.");

            if (!IsValidHttpUrl(resource.Url))
                ModelState.AddModelError(nameof(Resource.Url), "Please enter a valid URL starting with http:// or https://");

            if (ModelState.IsValid)
            {
                existing.SubjectId = resource.SubjectId;
                existing.Title = (resource.Title ?? "").Trim();
                existing.Url = (resource.Url ?? "").Trim();

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Resource updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateSubjectsDropdown(resource.SubjectId);
            return View(resource);
        }

        // GET: Resources/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (CurrentUserId == null)
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var resource = await _context.Resources
                .Include(r => r.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (resource == null) return NotFound();

            if (resource.Subject == null || resource.Subject.UserId != CurrentUserId.Value)
                return Forbid();

            return View(resource);
        }

        // POST: Resources/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (CurrentUserId == null)
                return RedirectToAction("Login", "Account");

            var resource = await _context.Resources
                .Include(r => r.Subject)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (resource == null) return NotFound();
            if (resource.Subject == null || resource.Subject.UserId != CurrentUserId.Value) return Forbid();

            _context.Resources.Remove(resource);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Resource deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateSubjectsDropdown(int? selectedSubjectId = null)
        {
            var userId = CurrentUserId!.Value;
            var subjects = await _context.Subjects
                .Where(s => s.UserId == userId)
                .OrderBy(s => s.SubjectName)
                .Select(s => new { s.Id, s.SubjectName })
                .ToListAsync();

            ViewData["SubjectId"] = new SelectList(subjects, "Id", "SubjectName", selectedSubjectId);
        }

        private static bool IsValidHttpUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var u) &&
                   (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);
        }
    }
}

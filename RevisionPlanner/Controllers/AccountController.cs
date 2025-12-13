using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RevisionPlanner.Data;
using RevisionPlanner.Models.ViewModels;

namespace RevisionPlanner.Controllers
{
    public class AccountController : Controller
    {
        private readonly RevisionPlannerDbContext _context;

        public AccountController(RevisionPlannerDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in, go Home
            if (HttpContext.Session.GetInt32("UserId") != null)
                return RedirectToAction("Index", "Home");

            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.EmailId == model.EmailId && u.EmailPassword == model.EmailPassword);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model); // stays on same page with error
            }

            // ✅ store login in session
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");

            return RedirectToAction("Index", "Home"); // success → Home
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}

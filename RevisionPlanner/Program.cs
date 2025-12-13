using Microsoft.EntityFrameworkCore;
using RevisionPlanner.Data;
//using RevisionPlanner.Models; // optional

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// EF Core DbContext
builder.Services.AddDbContext<RevisionPlannerDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Session (for storing logged-in UserId/UserName)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // ✅ Important for MVC static files (css/js/images)

app.UseRouting();

// ✅ Enable session BEFORE authorization/endpoints
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")  // ✅ Start at Login
    .WithStaticAssets();

app.Run();

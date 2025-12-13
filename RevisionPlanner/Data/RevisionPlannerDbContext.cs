using Microsoft.EntityFrameworkCore;
using RevisionPlanner.Models;

namespace RevisionPlanner.Data
{
    public class RevisionPlannerDbContext : DbContext
    {
        public RevisionPlannerDbContext(DbContextOptions<RevisionPlannerDbContext> options)
            : base(options)
        { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<Timetable> Timetables => Set<Timetable>();
        public DbSet<Resource> Resources => Set<Resource>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Optional: store enum as int (default) - explicit:
            modelBuilder.Entity<Subject>()
                .Property(s => s.Difficulty)
                .HasConversion<int>();
        }
    }
}

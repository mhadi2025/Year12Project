namespace RevisionPlanner.Models
{
    public class Resource
    {
        public int Id { get; set; }

        public int SubjectId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        // Navigation property
        public Subject? Subject { get; set; }
    }

}

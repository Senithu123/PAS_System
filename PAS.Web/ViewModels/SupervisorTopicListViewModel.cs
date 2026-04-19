namespace PAS.Web.ViewModels
{
    public class SupervisorTopicListViewModel
    {
        public int Id { get; set; }
        public string ProjectTitle { get; set; } = string.Empty;
        public string ResearchArea { get; set; } = string.Empty;
        public string DifficultyLevel { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? AttachmentFileName { get; set; }
        public bool HasAttachment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
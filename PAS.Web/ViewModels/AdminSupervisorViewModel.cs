namespace PAS.Web.ViewModels
{
    public class AdminSupervisorViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ExpertiseArea { get; set; } = "Not Set";
        public int AssignedStudentsCount { get; set; }
        public int TopicsSubmittedCount { get; set; }
    }
}
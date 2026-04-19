namespace PAS.Web.ViewModels
{
    public class SupervisorProfilePageViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Supervisor";
        public string ExpertiseArea { get; set; } = "Not Set";
        public int AssignedStudentsCount { get; set; }
        public int UnderReviewCount { get; set; }
    }
}
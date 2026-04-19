namespace PAS.Web.ViewModels
{
    public class StudentProfilePageViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Student";
        public int TotalProposals { get; set; }
        public int MatchedCount { get; set; }
        public int PendingCount { get; set; }
    }
}
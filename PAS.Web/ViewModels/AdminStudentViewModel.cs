namespace PAS.Web.ViewModels
{
    public class AdminStudentViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalProposals { get; set; }
        public int MatchedCount { get; set; }
    }
}
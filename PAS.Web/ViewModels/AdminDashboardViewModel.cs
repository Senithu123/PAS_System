using System.Collections.Generic;

namespace PAS.Web.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalSupervisors { get; set; }
        public int TotalProposals { get; set; }
        public int PendingCount { get; set; }
        public int UnderReviewCount { get; set; }
        public int MatchedCount { get; set; }

        public List<string> RecentItems { get; set; } = new();
    }
}
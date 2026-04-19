using System.Collections.Generic;

namespace PAS.Web.ViewModels
{
    public class StudentDashboardViewModel
    {
        public int TotalProposals { get; set; }
        public int PendingCount { get; set; }
        public int UnderReviewCount { get; set; }
        public int MatchedCount { get; set; }
        public int RejectedCount { get; set; }
        public int WithdrawnCount { get; set; }

        public List<string> RecentItems { get; set; } = new();
    }
}
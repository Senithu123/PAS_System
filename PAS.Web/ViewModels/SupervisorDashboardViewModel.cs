using System.Collections.Generic;

namespace PAS.Web.ViewModels
{
    public class SupervisorDashboardViewModel
    {
        public string ExpertiseArea { get; set; } = "Not Set";
        public int AvailableToReview { get; set; }
        public int UnderReviewCount { get; set; }
        public int MatchedCount { get; set; }
        public int TopicsSubmittedCount { get; set; }

        public List<string> RecentItems { get; set; } = new();
    }
}
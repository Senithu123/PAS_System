namespace PAS.Web.ViewModels
{
    public class HomeDashboardViewModel
    {
        public int TotalProposals { get; set; }
        public int TotalResearchAreas { get; set; }
        public int TotalUsers { get; set; }

        public List<string> RecentStatuses { get; set; } = new();
    }
}
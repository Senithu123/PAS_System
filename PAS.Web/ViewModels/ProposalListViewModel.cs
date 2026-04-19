namespace PAS.Web.ViewModels
{
    public class ProposalListViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ResearchArea { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsIdentityRevealed { get; set; }
        public string StudentEmail { get; set; } = string.Empty;
        public string? SupervisorEmail { get; set; }
    }
}
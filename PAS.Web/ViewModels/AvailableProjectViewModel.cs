namespace PAS.Web.ViewModels
{
    public class AvailableProjectViewModel
    {
        public int ProposalId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ResearchArea { get; set; } = string.Empty;
        public string TechnicalStack { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
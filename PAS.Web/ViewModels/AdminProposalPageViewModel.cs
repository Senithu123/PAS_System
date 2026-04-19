namespace PAS.Web.ViewModels
{
    public class AdminProposalPageViewModel
    {
        public AdminProposalSummaryViewModel Summary { get; set; } = new();
        public List<ProposalListViewModel> Proposals { get; set; } = new();
    }
}
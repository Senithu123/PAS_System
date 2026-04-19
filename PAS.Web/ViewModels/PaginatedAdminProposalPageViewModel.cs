namespace PAS.Web.ViewModels
{
    public class PaginatedAdminProposalPageViewModel
    {
        public AdminProposalSummaryViewModel Summary { get; set; } = new();
        public List<ProposalListViewModel> Proposals { get; set; } = new();

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
    }
}
namespace PAS.Web.ViewModels
{
    public class PaginatedStudentProposalPageViewModel
    {
        public List<StudentProposalViewModel> Proposals { get; set; } = new();
        public StudentProposalSummaryViewModel Summary { get; set; } = new();

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
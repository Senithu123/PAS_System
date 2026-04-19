namespace PAS.Web.ViewModels
{
    public class StudentMatchResultViewModel
    {
        public bool HasProposal { get; set; }
        public bool IsMatched { get; set; }

        public string ProposalTitle { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ResearchArea { get; set; } = string.Empty;

        public string SupervisorName { get; set; } = string.Empty;
        public string SupervisorEmail { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }
}
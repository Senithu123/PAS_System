namespace PAS.Web.ViewModels
{
    public class AssignedStudentViewModel
    {
        public int ProposalId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string ProposalTitle { get; set; } = string.Empty;
        public string ResearchArea { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
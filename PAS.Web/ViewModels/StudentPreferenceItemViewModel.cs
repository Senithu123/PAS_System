namespace PAS.Web.ViewModels
{
    public class StudentPreferenceItemViewModel
    {
        public int Id { get; set; }
        public int ProjectProposalId { get; set; }
        public string ProjectTitle { get; set; } = string.Empty;
        public string ResearchArea { get; set; } = string.Empty;
        public int PreferenceRank { get; set; }
    }
}
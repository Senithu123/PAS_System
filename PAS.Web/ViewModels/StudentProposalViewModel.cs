namespace PAS.Web.ViewModels
{
    public class StudentProposalViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ResearchArea { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsIdentityRevealed { get; set; }
        public string SupervisorEmail { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public bool HasUploadedFile { get; set; }
    }
}
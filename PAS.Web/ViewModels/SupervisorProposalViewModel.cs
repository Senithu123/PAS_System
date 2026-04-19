using System;

namespace PAS.Web.ViewModels
{
    public class SupervisorProposalViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Abstract { get; set; } = string.Empty;
        public string TechnicalStack { get; set; } = string.Empty;
        public string ResearchArea { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public bool HasUploadedFile { get; set; }
        public string? FileName { get; set; }

        public bool CanReview { get; set; }
        public bool CanMatch { get; set; }
    }
}
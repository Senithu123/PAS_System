using System.ComponentModel.DataAnnotations;

namespace PAS.Web.ViewModels
{
    public class AdminSettingsViewModel
    {
        [Display(Name = "Allow Proposal Submission")]
        public bool IsProposalSubmissionOpen { get; set; }

        [Display(Name = "Allow Topic Publishing")]
        public bool IsTopicPublishingOpen { get; set; }

        [Display(Name = "Allow Matching Phase")]
        public bool IsMatchingOpen { get; set; }

        [Display(Name = "Allow File Uploads")]
        public bool AllowFileUploads { get; set; }

        [Range(1, 10)]
        [Display(Name = "Maximum Preferences Per Student")]
        public int MaxPreferencesPerStudent { get; set; }
    }
}
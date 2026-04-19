using System.ComponentModel.DataAnnotations;

namespace PAS.Web.ViewModels
{
    public class StudentPreferenceInputViewModel
    {
        [Required]
        [Display(Name = "Project")]
        public int ProjectProposalId { get; set; }

        [Required]
        [Range(1, 10)]
        [Display(Name = "Preference Rank")]
        public int PreferenceRank { get; set; }
    }
}
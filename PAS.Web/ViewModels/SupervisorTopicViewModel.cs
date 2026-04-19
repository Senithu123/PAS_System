using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PAS.Web.ViewModels
{
    public class SupervisorTopicViewModel
    {
        [Required]
        [Display(Name = "Project Title")]
        public string ProjectTitle { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Research Area")]
        public int ResearchAreaId { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Required Skills")]
        public string RequiredSkills { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Expected Outcomes")]
        public string ExpectedOutcomes { get; set; } = string.Empty;

        [Required]
        [Range(1, 20)]
        public int Capacity { get; set; }

        [Required]
        [Display(Name = "Difficulty Level")]
        public string DifficultyLevel { get; set; } = string.Empty;

        [Display(Name = "Attachment File")]
        public IFormFile? AttachmentFile { get; set; }

        public string? CurrentAttachmentFileName { get; set; }
    }
}
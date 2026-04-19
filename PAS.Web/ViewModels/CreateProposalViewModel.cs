using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PAS.Web.ViewModels
{
    public class CreateProposalViewModel
    {
        [Required]
        [StringLength(200)]
        [Display(Name = "Project Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Project Abstract")]
        public string Abstract { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Technical Stack")]
        public string TechnicalStack { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Research Area")]
        public int ResearchAreaId { get; set; }

        [Display(Name = "Proposal File")]
        public IFormFile? ProposalFile { get; set; }

        public string? CurrentFileName { get; set; }
    }
}
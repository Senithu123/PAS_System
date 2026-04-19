using System.ComponentModel.DataAnnotations;
using PAS.Core.Enums;

namespace PAS.Core.Entities
{
    public class ProjectProposal
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Abstract { get; set; } = string.Empty;

        [Required]
        public string TechnicalStack { get; set; } = string.Empty;

        [Required]
        public int ResearchAreaId { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        public ProposalStatus Status { get; set; } = ProposalStatus.Pending;

        public bool IsIdentityRevealed { get; set; } = false;

        public string? MatchedSupervisorId { get; set; }

        public string? FileName { get; set; }
        public string? FilePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ApplicationUser? Student { get; set; }
        public ApplicationUser? MatchedSupervisor { get; set; }
        public ResearchArea? ResearchArea { get; set; }
    }
}
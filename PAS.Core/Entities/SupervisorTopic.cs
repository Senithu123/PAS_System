using System.ComponentModel.DataAnnotations;

namespace PAS.Core.Entities
{
    public class SupervisorTopic
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string ProjectTitle { get; set; } = string.Empty;

        [Required]
        public int ResearchAreaId { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string RequiredSkills { get; set; } = string.Empty;

        [Required]
        public string ExpectedOutcomes { get; set; } = string.Empty;

        [Range(1, 20)]
        public int Capacity { get; set; }

        [Required]
        [StringLength(50)]
        public string DifficultyLevel { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Draft";

        [Required]
        public string SupervisorId { get; set; } = string.Empty;

        public string? AttachmentFileName { get; set; }
        public string? AttachmentFilePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ApplicationUser? Supervisor { get; set; }
        public ResearchArea? ResearchArea { get; set; }
    }
}
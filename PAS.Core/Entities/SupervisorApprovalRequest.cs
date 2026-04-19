using System.ComponentModel.DataAnnotations;

namespace PAS.Core.Entities
{
    public class SupervisorApprovalRequest
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Department { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string RequestedExpertise { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Notes { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }

        public ApplicationUser? User { get; set; }
    }
}
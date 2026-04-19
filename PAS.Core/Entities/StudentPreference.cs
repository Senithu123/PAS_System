using System.ComponentModel.DataAnnotations;

namespace PAS.Core.Entities
{
    public class StudentPreference
    {
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        public int ProjectProposalId { get; set; }

        [Range(1, 10)]
        public int PreferenceRank { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser? Student { get; set; }
        public ProjectProposal? ProjectProposal { get; set; }
    }
}
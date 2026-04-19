using System.ComponentModel.DataAnnotations;

namespace PAS.Core.Entities
{
    public class SystemSetting
    {
        public int Id { get; set; }

        public bool IsProposalSubmissionOpen { get; set; } = true;
        public bool IsTopicPublishingOpen { get; set; } = true;
        public bool IsMatchingOpen { get; set; } = true;
        public bool AllowFileUploads { get; set; } = true;

        [Range(1, 10)]
        public int MaxPreferencesPerStudent { get; set; } = 3;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
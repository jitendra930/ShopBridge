using System.ComponentModel.DataAnnotations;

namespace ShopBridge.Models.JobAutomation
{
    public class JobPosting
    {
        [Required]
        public string JobId { get; set; } = string.Empty;

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Company { get; set; } = string.Empty;

        public string SourcePlatform { get; set; } = "LinkedIn";

        public string Location { get; set; } = string.Empty;

        public string WorkMode { get; set; } = string.Empty;

        public int MinimumYearsExperience { get; set; }

        public bool OffersVisaSponsorship { get; set; }

        public int? CompensationMin { get; set; }

        public string ApplicationUrl { get; set; } = string.Empty;

        public DateTimeOffset PostedAt { get; set; } = DateTimeOffset.UtcNow;

        public List<string> RequiredSkills { get; set; } = new();

        public List<string> PreferredSkills { get; set; } = new();
    }
}

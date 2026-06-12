using System.ComponentModel.DataAnnotations;

namespace ShopBridge.Models.JobAutomation
{
    public class CandidateProfile
    {
        [Required]
        public string CandidateId { get; set; } = "default";

        [Required]
        public string FullName { get; set; } = string.Empty;

        public string ResumeHeadline { get; set; } = string.Empty;

        public int YearsOfExperience { get; set; }

        public bool NeedsVisaSponsorship { get; set; }

        public int MinimumCompensation { get; set; }

        public int DailyApplicationCap { get; set; } = 5;

        public int CompanyCooldownHours { get; set; } = 24;

        public double MinimumMatchScore { get; set; } = 0.65;

        public List<string> Skills { get; set; } = new();

        public List<string> PreferredTitles { get; set; } = new();

        public List<string> PreferredLocations { get; set; } = new();

        public List<string> PreferredWorkModes { get; set; } = new();

        public List<string> BlacklistedCompanies { get; set; } = new();

        public List<string> WhitelistedCompanies { get; set; } = new();

        public List<string> VerifiedHighlights { get; set; } = new();
    }
}

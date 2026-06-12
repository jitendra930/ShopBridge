namespace ShopBridge.Configuration
{
    public class JobAutomationOptions
    {
        public string SupportedPlatform { get; set; } = "LinkedIn";
        public bool AllowDirectSubmission { get; set; }
        public int DailyApplicationCap { get; set; } = 5;
        public int CompanyCooldownHours { get; set; } = 24;
        public double MinimumMatchScore { get; set; } = 0.65;
        public double HumanReviewScoreThreshold { get; set; } = 0.8;
        public int MonitorIntervalMinutes { get; set; } = 60;
    }
}

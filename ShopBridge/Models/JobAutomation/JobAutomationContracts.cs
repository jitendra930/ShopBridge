namespace ShopBridge.Models.JobAutomation
{
    public enum ReviewQueueStatus
    {
        PendingApproval,
        NeedsReview,
        ApprovedForManualSubmission,
        Rejected,
        Submitted
    }

    public class JobAutomationPolicy
    {
        public string Platform { get; set; } = "LinkedIn";
        public bool DirectAutoSubmissionAllowed { get; set; }
        public string RecommendedMode { get; set; } = "Human approval queue";
        public string Guidance { get; set; } = string.Empty;
    }

    public class JobImportResult
    {
        public int ImportedCount { get; set; }
        public int DuplicateCount { get; set; }
    }

    public class JobMatchResult
    {
        public string JobId { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public double Score { get; set; }
        public bool Eligible { get; set; }
        public bool AddedToQueue { get; set; }
        public ReviewQueueStatus? QueueStatus { get; set; }
        public List<string> Reasons { get; set; } = new();
    }

    public class ApplicationQueueItem
    {
        public Guid QueueItemId { get; set; } = Guid.NewGuid();
        public string JobId { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public double MatchScore { get; set; }
        public ReviewQueueStatus Status { get; set; }
        public string ResumeSummary { get; set; } = string.Empty;
        public string CoverLetterDraft { get; set; } = string.Empty;
        public string ReviewerNotes { get; set; } = string.Empty;
        public string SubmissionMode { get; set; } = "ManualLinkedInSubmission";
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    public class QueueReviewRequest
    {
        public bool Approve { get; set; }
        public string ReviewerNotes { get; set; } = string.Empty;
    }

    public class ApplicationOutcomeRecord
    {
        public Guid? QueueItemId { get; set; }
        public string JobId { get; set; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        public bool GotResponse { get; set; }
        public bool InterviewScheduled { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    public class AutomationAlert
    {
        public string Level { get; set; } = "Info";
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    public class ScheduledReviewSummary
    {
        public int JobsReviewed { get; set; }
        public int QueueItemsCreated { get; set; }
        public int AlertsRaised { get; set; }
        public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    public class AutomationDashboardSnapshot
    {
        public int ImportedJobs { get; set; }
        public int QueueDepth { get; set; }
        public int ApprovedForManualSubmission { get; set; }
        public int SubmittedApplications { get; set; }
        public int ResponsesReceived { get; set; }
        public int InterviewsScheduled { get; set; }
        public double ResponseRate { get; set; }
        public double InterviewRate { get; set; }
        public List<AutomationAlert> Alerts { get; set; } = new();
    }
}

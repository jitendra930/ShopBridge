using ShopBridge.Models.JobAutomation;

namespace ShopBridge.Interface
{
    public interface IJobAutomationService
    {
        Task<JobAutomationPolicy> GetPolicyAsync();
        Task<CandidateProfile> SaveProfileAsync(CandidateProfile profile);
        Task<JobImportResult> ImportJobsAsync(IEnumerable<JobPosting> jobs);
        Task<IReadOnlyCollection<JobMatchResult>> ReviewNewJobsAsync();
        Task<IReadOnlyCollection<ApplicationQueueItem>> GetQueueAsync();
        Task<ApplicationQueueItem?> ReviewQueueItemAsync(Guid queueItemId, QueueReviewRequest request);
        Task<ApplicationOutcomeRecord> RecordOutcomeAsync(ApplicationOutcomeRecord outcome);
        Task<AutomationDashboardSnapshot> GetDashboardAsync();
        Task<ScheduledReviewSummary> RunScheduledReviewAsync(CancellationToken cancellationToken = default);
    }
}

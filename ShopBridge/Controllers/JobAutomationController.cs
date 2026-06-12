using Microsoft.AspNetCore.Mvc;
using ShopBridge.Interface;
using ShopBridge.Models.JobAutomation;

namespace ShopBridge.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class JobAutomationController : ControllerBase
    {
        private readonly IJobAutomationService _jobAutomationService;

        public JobAutomationController(IJobAutomationService jobAutomationService)
        {
            _jobAutomationService = jobAutomationService;
        }

        [HttpGet]
        public async Task<ActionResult<JobAutomationPolicy>> Policy()
        {
            return Ok(await _jobAutomationService.GetPolicyAsync());
        }

        [HttpPost]
        public async Task<ActionResult<CandidateProfile>> SaveProfile(CandidateProfile profile)
        {
            return Ok(await _jobAutomationService.SaveProfileAsync(profile));
        }

        [HttpPost]
        public async Task<ActionResult<JobImportResult>> ImportJobs(List<JobPosting> jobs)
        {
            return Ok(await _jobAutomationService.ImportJobsAsync(jobs));
        }

        [HttpPost]
        public async Task<ActionResult<IReadOnlyCollection<JobMatchResult>>> ReviewNewJobs()
        {
            return Ok(await _jobAutomationService.ReviewNewJobsAsync());
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyCollection<ApplicationQueueItem>>> Queue()
        {
            return Ok(await _jobAutomationService.GetQueueAsync());
        }

        [HttpPost("{queueItemId:guid}")]
        public async Task<ActionResult<ApplicationQueueItem>> ReviewQueueItem(Guid queueItemId, QueueReviewRequest request)
        {
            var queueItem = await _jobAutomationService.ReviewQueueItemAsync(queueItemId, request);
            if (queueItem == null)
            {
                return NotFound();
            }

            return Ok(queueItem);
        }

        [HttpPost]
        public async Task<ActionResult<ApplicationOutcomeRecord>> RecordOutcome(ApplicationOutcomeRecord outcome)
        {
            return Ok(await _jobAutomationService.RecordOutcomeAsync(outcome));
        }

        [HttpGet]
        public async Task<ActionResult<AutomationDashboardSnapshot>> Dashboard()
        {
            return Ok(await _jobAutomationService.GetDashboardAsync());
        }

        [HttpPost]
        public async Task<ActionResult<ScheduledReviewSummary>> RunScheduledReview()
        {
            return Ok(await _jobAutomationService.RunScheduledReviewAsync());
        }
    }
}

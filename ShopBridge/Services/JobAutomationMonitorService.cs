using Microsoft.Extensions.Options;
using ShopBridge.Configuration;
using ShopBridge.Interface;

namespace ShopBridge.Services
{
    public class JobAutomationMonitorService : BackgroundService
    {
        private readonly IJobAutomationService _jobAutomationService;
        private readonly JobAutomationOptions _options;
        private readonly ILogger<JobAutomationMonitorService> _logger;

        public JobAutomationMonitorService(
            IJobAutomationService jobAutomationService,
            IOptions<JobAutomationOptions> options,
            ILogger<JobAutomationMonitorService> logger)
        {
            _jobAutomationService = jobAutomationService;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromMinutes(Math.Max(_options.MonitorIntervalMinutes, 1));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var summary = await _jobAutomationService.RunScheduledReviewAsync(stoppingToken);
                    _logger.LogInformation(
                        "Scheduled job automation review completed. Reviewed={Reviewed}, Queued={Queued}, Alerts={Alerts}",
                        summary.JobsReviewed,
                        summary.QueueItemsCreated,
                        summary.AlertsRaised);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Scheduled job automation review failed.");
                }

                await Task.Delay(interval, stoppingToken);
            }
        }
    }
}

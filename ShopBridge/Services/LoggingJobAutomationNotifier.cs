using ShopBridge.Interface;
using ShopBridge.Models.JobAutomation;

namespace ShopBridge.Services
{
    public class LoggingJobAutomationNotifier : IJobAutomationNotifier
    {
        private readonly ILogger<LoggingJobAutomationNotifier> _logger;

        public LoggingJobAutomationNotifier(ILogger<LoggingJobAutomationNotifier> logger)
        {
            _logger = logger;
        }

        public Task NotifyAsync(AutomationAlert alert, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Job automation alert [{Level}]: {Message}", alert.Level, alert.Message);
            return Task.CompletedTask;
        }
    }
}

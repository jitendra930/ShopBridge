using ShopBridge.Models.JobAutomation;

namespace ShopBridge.Interface
{
    public interface IJobAutomationNotifier
    {
        Task NotifyAsync(AutomationAlert alert, CancellationToken cancellationToken = default);
    }
}

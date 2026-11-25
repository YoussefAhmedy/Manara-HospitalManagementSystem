namespace Hospital.BLL.Services.Abstraction;

public interface IEmailNotificationService
{
    Task NotifyUserAsync(
        string recipientUserId,
        string category,
        string subject,
        string bodySummary,
        string? actionUrl = null,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);

    Task MarkAllReadAsync(string userId, CancellationToken cancellationToken = default);
}

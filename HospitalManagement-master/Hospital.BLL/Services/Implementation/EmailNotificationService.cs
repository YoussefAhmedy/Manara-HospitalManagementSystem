using Hospital.BLL.Helpers;
using Hospital.BLL.Services.Abstraction;
using Hospital.DAL.DataBase;
using Hospital.DAL.Entities.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace Hospital.BLL.Services.Implementation;

public sealed class EmailNotificationService(
    HospitalDbContext dbContext,
    IEmailQueue emailQueue,
    IOptionsMonitor<EmailOptions> emailOptions,
    ILogger<EmailNotificationService> logger) : IEmailNotificationService
{
    public async Task NotifyUserAsync(
        string recipientUserId,
        string category,
        string subject,
        string bodySummary,
        string? actionUrl = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipientUserId))
        {
            return;
        }

        var user = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == recipientUserId, cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var preferenceEnabled = category switch
        {
            "LabResult" or "RadiologyResult" => user.EmailNotifyLabs,
            "Prescription" => user.EmailNotifyPrescriptions,
            "Appointment" => user.EmailNotifyAppointments,
            "ConsultationReply" => user.EmailNotifyReplies,
            _ => true
        };

        var sentViaSmtp = false;
        var deliveryStatus = "In-App & Ready";

        if (!preferenceEnabled)
        {
            deliveryStatus = "Skipped-Preference";
        }
        else if (emailOptions.CurrentValue.Enabled)
        {
            try
            {
                var safeName = WebUtility.HtmlEncode($"{user.FirstName} {user.LastName}".Trim());
                var safeTitle = WebUtility.HtmlEncode(subject);
                var safeBody = WebUtility.HtmlEncode(bodySummary);
                var html = $"""
                    <!DOCTYPE html>
                    <html><body style="margin:0;background:#F8FAFC;font-family:Inter,Arial,sans-serif;color:#0F172A;">
                    <div style="max-width:600px;margin:28px auto;padding:28px;background:#ffffff;border:1px solid #E2E8F0;border-radius:16px;">
                    <p style="font-size:12px;font-weight:700;letter-spacing:0.14em;color:#2563EB;margin:0 0 8px;">MANARA MEDICAL COMPLEX · ALEXANDRIA</p>
                    <h2 style="margin:0 0 12px;color:#0F172A;">{safeTitle}</h2>
                    <p style="margin:0 0 12px;color:#334155;">Dear {safeName},</p>
                    <p style="margin:0 0 18px;line-height:1.6;color:#334155;">{safeBody}</p>
                    <hr style="border:0;border-top:1px solid #E2E8F0;margin:20px 0;">
                    <p style="font-size:12px;color:#64748B;margin:0;">Open your MANARA Updates Portal to review details or book a follow-up consultation.</p>
                    </div></body></html>
                    """;

                sentViaSmtp = emailQueue.TryEnqueue(user.Email, $"MANARA: {subject}", html);
                deliveryStatus = sentViaSmtp ? "Sent" : "In-App & Ready";
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Non-fatal email delivery enqueue failure for notification log.");
                deliveryStatus = "In-App & Ready";
            }
        }

        dbContext.EmailNotificationLogs.Add(new EmailNotificationLog
        {
            RecipientUserId = user.Id,
            RecipientEmail = user.Email,
            RecipientName = $"{user.FirstName} {user.LastName}".Trim(),
            Category = category,
            Subject = subject.Length > 200 ? subject[..200] : subject,
            BodySummary = bodySummary.Length > 1500 ? bodySummary[..1500] : bodySummary,
            ActionUrl = actionUrl,
            SentViaSmtp = sentViaSmtp,
            DeliveryStatus = deliveryStatus,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return 0;
        }

        var unreadLogs = await dbContext.EmailNotificationLogs
            .CountAsync(n => n.RecipientUserId == userId && !n.IsRead, cancellationToken);

        var unreadPatientReplies = await dbContext.ConsultationInquiries
            .CountAsync(c => c.PatientUserId == userId && c.Status == "Answered" && !c.IsReadByPatient, cancellationToken);

        return unreadLogs + unreadPatientReplies;
    }

    public async Task MarkAllReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var unreadLogs = await dbContext.EmailNotificationLogs
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);
        foreach (var log in unreadLogs)
        {
            log.IsRead = true;
        }

        var unreadReplies = await dbContext.ConsultationInquiries
            .Where(c => c.PatientUserId == userId && c.Status == "Answered" && !c.IsReadByPatient)
            .ToListAsync(cancellationToken);
        foreach (var reply in unreadReplies)
        {
            reply.IsReadByPatient = true;
        }

        if (unreadLogs.Count > 0 || unreadReplies.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

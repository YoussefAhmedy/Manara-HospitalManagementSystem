using System.ComponentModel.DataAnnotations;

namespace Hospital.DAL.Entities.Operations;

public class EmailNotificationLog
{
    public int Id { get; set; }

    [Required, MaxLength(64)]
    public string RecipientUserId { get; set; } = string.Empty;

    [Required, MaxLength(160)]
    public string RecipientEmail { get; set; } = string.Empty;

    [MaxLength(120)]
    public string RecipientName { get; set; } = string.Empty;

    /// <summary>LabResult | RadiologyResult | Prescription | Appointment | ConsultationReply | General</summary>
    [Required, MaxLength(40)]
    public string Category { get; set; } = "General";

    [Required, MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required, MaxLength(1500)]
    public string BodySummary { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? ActionUrl { get; set; }

    public bool SentViaSmtp { get; set; }

    [Required, MaxLength(40)]
    public string DeliveryStatus { get; set; } = "Recorded";

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

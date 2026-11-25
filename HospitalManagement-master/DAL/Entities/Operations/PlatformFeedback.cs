using System.ComponentModel.DataAnnotations;

namespace Hospital.DAL.Entities.Operations;

public class PlatformFeedback
{
    public int Id { get; set; }

    [MaxLength(64)]
    public string? UserId { get; set; }

    [Required, MaxLength(120)]
    public string AuthorName { get; set; } = string.Empty;

    [MaxLength(160)]
    public string AuthorEmail { get; set; } = string.Empty;

    /// <summary>Review | Suggestion | Complaint</summary>
    [Required, MaxLength(32)]
    public string Category { get; set; } = "Review";

    /// <summary>Hospital | Doctor | Staff | Department</summary>
    [Required, MaxLength(32)]
    public string TargetScope { get; set; } = "Hospital";

    public int? TargetDoctorId { get; set; }

    [MaxLength(120)]
    public string? TargetName { get; set; }

    public int? Rating { get; set; }

    [Required, MaxLength(180)]
    public string Subject { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    /// <summary>New | Reviewed | Resolved</summary>
    [Required, MaxLength(32)]
    public string Status { get; set; } = "New";

    [MaxLength(1000)]
    public string? ManagerNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }
}

using Hospital.DAL.Entities.Organization;
using System.ComponentModel.DataAnnotations;

namespace Hospital.DAL.Entities.Clinical;

public class ConsultationInquiry
{
    public int Id { get; set; }

    [MaxLength(450)]
    public string? PatientUserId { get; set; }

    [MaxLength(450)]
    public string? PatientId { get; set; }
    public Patient? Patient { get; set; }

    [Required, MaxLength(120)]
    public string PatientName { get; set; } = string.Empty;

    [Required, MaxLength(160)]
    public string PatientEmail { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    [MaxLength(450)]
    public string? DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    [Required, MaxLength(180)]
    public string Subject { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Question { get; set; } = string.Empty;

    [Required, MaxLength(32)]
    public string Status { get; set; } = "Pending";

    [MaxLength(2000)]
    public string? DoctorReply { get; set; }

    [MaxLength(64)]
    public string? RepliedByUserId { get; set; }

    [MaxLength(120)]
    public string? RepliedByName { get; set; }

    public DateTime? RepliedAt { get; set; }

    public bool PatientNotifiedByEmail { get; set; }

    public bool IsReadByPatient { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

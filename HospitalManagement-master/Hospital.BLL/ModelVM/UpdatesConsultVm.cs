using Hospital.DAL.Entities;
using Hospital.DAL.Entities.Clinical;
using Hospital.DAL.Entities.Diagnostics;
using Hospital.DAL.Entities.Operations;
using Hospital.DAL.Entities.Organization;
using Hospital.DAL.Entities.Pharmacy;

namespace Hospital.BLL.ModelVM;

public sealed class UpdatesHubVm
{
    public bool IsAuthenticated { get; init; }
    public bool IsPatient { get; init; }
    public bool IsDoctor { get; init; }
    public bool IsManager { get; init; }

    public string DisplayName { get; init; } = string.Empty;
    public int UnreadCount { get; init; }

    // Patient view slices
    public Patient? Patient { get; init; }
    public IReadOnlyList<LabOrder> ReleasedLabOrders { get; init; } = [];
    public IReadOnlyList<ImagingOrder> ReleasedImagingOrders { get; init; } = [];
    public IReadOnlyList<ClinicalNote> DoctorNotes { get; init; } = [];
    public IReadOnlyList<Prescription> Prescriptions { get; init; } = [];
    public IReadOnlyList<Appointment> UpcomingAppointments { get; init; } = [];
    public IReadOnlyList<Appointment> CompletedAppointments { get; init; } = [];
    public IReadOnlyList<EmailNotificationLog> Notifications { get; init; } = [];
    public IReadOnlyList<ConsultationInquiry> PatientInquiries { get; init; } = [];
    public Dictionary<string, Doctor> DoctorsById { get; init; } = [];

    // Doctor / Manager view slices
    public Doctor? AttendingDoctor { get; init; }
    public IReadOnlyList<Appointment> AssignedPatientVisits { get; init; } = [];
    public IReadOnlyList<ConsultationInquiry> OpenInquiries { get; init; } = [];
    public IReadOnlyList<ConsultationInquiry> AnsweredInquiries { get; init; } = [];
    public IReadOnlyList<(string Label, string TemplateText)> QuickReplyTemplates { get; init; } = [];
}

public sealed class ConsultHubVm
{
    public IReadOnlyList<Department> Departments { get; init; } = [];
    public IReadOnlyList<Doctor> Doctors { get; init; } = [];
    public IReadOnlyList<PlatformFeedback> PublishedReviews { get; init; } = [];
    public string? PrefillName { get; init; }
    public string? PrefillEmail { get; init; }
    public int? PreselectedDepartmentId { get; init; }
    public int TotalReviewsCount { get; init; }
    public double AverageRating { get; init; }
}

public sealed class ManageFeedbackVm
{
    public IReadOnlyList<PlatformFeedback> Items { get; init; } = [];
    public string? SelectedCategory { get; init; }
    public string? SelectedStatus { get; init; }
}

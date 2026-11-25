using Hospital.DAL.Entities;
using Hospital.DAL.Entities.Billing;
using Hospital.DAL.Entities.Pharmacy;

namespace Hospital.BLL.ModelVM;

public sealed class PublicDepartmentVm
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? FloorLocation { get; init; }
    public string? Description { get; init; }
    public string? Capabilities { get; init; }
    public IReadOnlyList<PublicDoctorVm> Doctors { get; init; } = [];
}

public sealed class MainVm
{
    public IReadOnlyList<PublicDoctorVm> Doctors { get; init; } = [];
    public IReadOnlyList<PublicDepartmentVm> Departments { get; init; } = [];
    public bool IsReturningPatient { get; init; }
    public HashSet<string> FollowUpEligibleDoctorIds { get; init; } = [];
}

public sealed class SettingsVm
{
    public EditProfileVm Profile { get; set; } = new();
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
    public string? ConfirmNewPassword { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
    public bool UseWorkspaceLayout { get; set; }

    // Notification preferences
    public bool EmailNotifyLabs { get; set; } = true;
    public bool EmailNotifyPrescriptions { get; set; } = true;
    public bool EmailNotifyAppointments { get; set; } = true;
    public bool EmailNotifyReplies { get; set; } = true;

    // Accessibility preferences
    public string UiFontSize { get; set; } = "default";
    public string UiDensity { get; set; } = "comfortable";
    public bool UiReducedMotion { get; set; }
}

public sealed class PatientMyCareVm
{
    public Patient Patient { get; init; } = default!;
    public IReadOnlyList<Prescription> Prescriptions { get; init; } = [];
    public IReadOnlyList<Appointment> CompletedVisits { get; init; } = [];
    public IReadOnlyList<Appointment> UpcomingVisits { get; init; } = [];
    public IReadOnlyList<Invoice> Invoices { get; init; } = [];
    public Dictionary<string, Doctor> DoctorsById { get; init; } = [];
}

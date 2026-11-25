using System.Security.Claims;
using Hospital.BLL.Common;
using Hospital.BLL.Helpers;
using Hospital.BLL.Permissions;
using Hospital.BLL.Services.Abstraction;
using Hospital.DAL.DataBase;
using Hospital.DAL.Entities.OwnedTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Controllers;

public sealed class DashboardBookingItem
{
    public int AppointmentId { get; init; }
    public string Number { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public int SlotMinutes { get; init; }
    public string PatientId { get; init; } = string.Empty;
    public string PatientName { get; init; } = string.Empty;
    public string Mrn { get; init; } = string.Empty;
    public string DoctorId { get; init; } = string.Empty;
    public string DoctorName { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = "General";
    public bool IsFollowUp { get; init; }
    public string Notes { get; init; } = string.Empty;
    public AppointStatus Status { get; init; }
    public int? EncounterId { get; init; }
}

/// <summary>
/// Staff-only operational landing workspace. Patients are strictly excluded from accessing /Dashboard
/// and remain exclusively in the MANARA public portal & patient care experience.
/// </summary>
[Authorize(Roles = "Admin,HospitalAdmin,SuperAdmin,Management,Doctor,Receptionist,LabTechnician,Radiologist,Pharmacist,BillingOfficer,Nurse,Auditor")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class DashboardController(
    IWorkspaceService workspace,
    IAppointmentSchedulingService scheduling,
    IEncounterService encounters,
    ILabService lab,
    IAdmissionService admissions,
    IReportService reports,
    HospitalDbContext dbContext) : Controller
{
    private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Overview";
        ViewData["Section"] = "Workspace";

        if (User.IsInRole(nameof(Role.Doctor)))
        {
            var doctorId = CurrentUserId();
            var kpis = await workspace.DoctorAsync(doctorId, cancellationToken);
            var queue = await scheduling.QueueAsync(DateTime.UtcNow.Date, cancellationToken);

            var doctorAppointmentsRaw = await dbContext.Appointments.AsNoTracking()
                .Include(a => a.Patient)
                .Include(a => a.Doctor).ThenInclude(d => d!.Department)
                .Include(a => a.Doctor).ThenInclude(d => d!.Specialization)
                .Where(a => a.DoctorID == doctorId && a.Status != AppointStatus.Cancelled && a.Status != AppointStatus.NotApproved)
                .OrderByDescending(a => a.AppointmentDate >= DateTime.UtcNow.Date)
                .ThenBy(a => a.AppointmentDate)
                .ThenBy(a => a.SlotMinutes)
                .Take(16)
                .ToListAsync(cancellationToken);

            var patientIds = doctorAppointmentsRaw.Select(a => a.PatientID).Distinct().ToList();
            var priorCompletedPatientIds = (await dbContext.Appointments.AsNoTracking()
                .Where(a => a.DoctorID == doctorId && patientIds.Contains(a.PatientID) && a.Status == AppointStatus.Completed)
                .Select(a => a.PatientID)
                .Distinct()
                .ToListAsync(cancellationToken)).ToHashSet();

            var encounterByPatient = await dbContext.Encounters.AsNoTracking()
                .Where(e => e.DoctorId == doctorId && patientIds.Contains(e.PatientId))
                .OrderByDescending(e => e.RegisteredAtUtc)
                .Select(e => new { e.PatientId, e.Id })
                .ToListAsync(cancellationToken);

            var doctorBookings = doctorAppointmentsRaw.Select(a => new DashboardBookingItem
            {
                AppointmentId = a.AppointmentID,
                Number = a.Number,
                Date = a.AppointmentDate,
                SlotMinutes = a.SlotMinutes,
                PatientId = a.PatientID,
                PatientName = a.Patient != null ? $"{a.Patient.FirstName} {a.Patient.LastName}".Trim() : "Patient",
                Mrn = a.Patient?.Mrn ?? "—",
                DoctorId = a.DoctorID ?? doctorId,
                DoctorName = a.Doctor != null ? $"Dr. {a.Doctor.FirstName} {a.Doctor.LastName}" : "Attending",
                DepartmentName = a.Doctor?.Department?.Name ?? a.Doctor?.Specialization?.Name ?? "Outpatient",
                IsFollowUp = priorCompletedPatientIds.Contains(a.PatientID) && a.Status != AppointStatus.Completed,
                Notes = a.Notes ?? "",
                Status = a.Status,
                EncounterId = encounterByPatient.FirstOrDefault(e => e.PatientId == a.PatientID)?.Id
            }).ToList();

            ViewData["Subtitle"] = "Your clinic schedule: strictly patients booked with you, clinical notes, and encounter links.";
            ViewBag.Kpis = kpis;
            ViewBag.Queue = queue;
            ViewBag.DoctorBookings = doctorBookings;
            return View("Clinician");
        }

        if (User.IsInRole(nameof(Role.Receptionist)))
        {
            ViewBag.Kpis = await workspace.ReceptionAsync(cancellationToken);
            ViewBag.Queue = await scheduling.QueueAsync(DateTime.UtcNow.Date, cancellationToken);
            ViewData["Subtitle"] = "Registration, check-in and today's appointment flow.";
            return View("Reception");
        }

        if (User.IsInRole(nameof(Role.LabTechnician)))
        {
            return RedirectToAction("Index", "Laboratory");
        }

        if (User.IsInRole(nameof(Role.Pharmacist)))
        {
            return RedirectToAction("Index", "Pharmacy");
        }

        if (User.IsInRole(nameof(Role.BillingOfficer)))
        {
            return RedirectToAction("Index", "Billing");
        }

        if (User.IsInRole(nameof(Role.Nurse)))
        {
            ViewBag.Kpis = await workspace.ReceptionAsync(cancellationToken);
            var triage = await encounters.QueryAsync(Hospital.DAL.Entities.Clinical.EncounterStatus.Registered, null, null, new DateRange(null, null), null, 1, 10, cancellationToken);
            ViewBag.Triage = triage;
            ViewData["Subtitle"] = "Triage queue and vitals recording.";
            return View("Reception");
        }

        // Administration, management and auditor share the operational overview + full bookings ledger.
        ViewBag.Kpis = await workspace.AdminAsync(cancellationToken);
        ViewBag.Occupancy = await admissions.SummaryAsync(cancellationToken);
        ViewBag.Recent = await workspace.RecentActivityAsync(8, cancellationToken);
        var month = await reports.RunAsync("appointments-status", new ReportFilter(DateRange.ThisMonth(DateOnly.FromDateTime(DateTime.UtcNow))), cancellationToken);
        ViewBag.AppointmentOutcomes = month;
        var revenue = await reports.RunAsync("revenue", new ReportFilter(DateRange.LastDays(14, DateOnly.FromDateTime(DateTime.UtcNow))), cancellationToken);
        ViewBag.Revenue = revenue;

        var ledgerRaw = await dbContext.Appointments.AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d!.Department)
            .Include(a => a.Doctor).ThenInclude(d => d!.Specialization)
            .OrderByDescending(a => a.AppointmentDate)
            .ThenBy(a => a.SlotMinutes)
            .Take(12)
            .ToListAsync(cancellationToken);

        var completedPairs = (await dbContext.Appointments.AsNoTracking()
            .Where(a => a.Status == AppointStatus.Completed && a.DoctorID != null)
            .Select(a => new { a.PatientID, DoctorId = a.DoctorID! })
            .Distinct()
            .ToListAsync(cancellationToken))
            .Select(x => $"{x.PatientID}:{x.DoctorId}")
            .ToHashSet();

        ViewBag.BookingsLedger = ledgerRaw.Select(a => new DashboardBookingItem
        {
            AppointmentId = a.AppointmentID,
            Number = a.Number,
            Date = a.AppointmentDate,
            SlotMinutes = a.SlotMinutes,
            PatientId = a.PatientID,
            PatientName = a.Patient != null ? $"{a.Patient.FirstName} {a.Patient.LastName}".Trim() : "Patient",
            Mrn = a.Patient?.Mrn ?? "—",
            DoctorId = a.DoctorID ?? "",
            DoctorName = a.Doctor != null ? $"Dr. {a.Doctor.FirstName} {a.Doctor.LastName}".Trim() : "Unassigned",
            DepartmentName = a.Doctor?.Department?.Name ?? a.Doctor?.Specialization?.Name ?? "General",
            IsFollowUp = a.DoctorID != null && completedPairs.Contains($"{a.PatientID}:{a.DoctorID}") && a.Status != AppointStatus.Completed,
            Notes = a.Notes ?? "",
            Status = a.Status
        }).ToList();

        ViewData["Subtitle"] = "Operational posture across admissions, bookings ledger, diagnostics, and governance.";
        return View("Operations");
    }

    [Authorize(Policy = "perm:" + Permission.ReportFinancial)]
    public async Task<IActionResult> Revenue(CancellationToken cancellationToken)
    {
        var report = await reports.RunAsync("revenue", new ReportFilter(DateRange.LastDays(14, DateOnly.FromDateTime(DateTime.UtcNow))), cancellationToken);
        return Json(new
        {
            total = report.Summary.FirstOrDefault(s => s.Key == "Net revenue").Value,
            rows = report.Rows
        });
    }
}

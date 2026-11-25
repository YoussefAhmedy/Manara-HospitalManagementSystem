using System.Security.Claims;
using Hospital.BLL.Common;
using Hospital.BLL.Common.Rules;
using Hospital.BLL.Helpers;
using Hospital.BLL.Permissions;
using Hospital.BLL.Services.Abstraction;
using Hospital.DAL.DataBase;
using Hospital.DAL.Entities.OwnedTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Controllers;

public sealed record PublicBookRequest(string DoctorId, DateTime Date, int SlotMinutes, string? Notes);

/// <summary>Scheduling workspace: availability, booking, check-in and queue management.</summary>
[Authorize]
public sealed class AppointmentsController(
    IAppointmentSchedulingService scheduling,
    IPatientLookupService patients,
    IOrganizationDirectoryService directory,
    HospitalDbContext dbContext,
    IAuditLogger audit,
    IEmailNotificationService notifications) : Controller
{
    private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public async Task<IActionResult> Index(DateTime? date, string? status, string? doctorId, string? term, int page = 1, CancellationToken cancellationToken = default)
    {
        if (User.IsInRole(nameof(Role.Patient)))
        {
            return RedirectToAction("Index", "Updates");
        }

        if (!User.HasClaim(Hospital.BLL.Helpers.AppClaimTypes.Permission, Permission.AppointmentView))
        {
            return Forbid();
        }

        bool isDoctorScoped = User.IsInRole(nameof(Role.Doctor)) &&
                              !User.IsInRole(nameof(Role.Admin)) &&
                              !User.IsInRole(nameof(Role.HospitalAdmin)) &&
                              !User.IsInRole(nameof(Role.SuperAdmin)) &&
                              !User.IsInRole(nameof(Role.Management));
        var effectiveDoctorId = isDoctorScoped ? CurrentUserId() : doctorId;

        AppointStatus? parsed = Enum.TryParse<AppointStatus>(status, true, out var value) ? value : null;
        var result = await scheduling.QueryAsync(date, parsed, effectiveDoctorId, null, term, page, 20, cancellationToken);

        ViewData["Title"] = "Appointments";
        ViewData["Section"] = "Scheduling";
        ViewData["Subtitle"] = isDoctorScoped
            ? "Your attending consultation schedule and assigned patient bookings."
            : "Hospital-wide slot scheduling with conflict protection and live check-in ledger.";

        ViewBag.Date = date?.ToString("yyyy-MM-dd");
        ViewBag.Status = status;
        ViewBag.DoctorId = effectiveDoctorId;
        ViewBag.Term = term;
        ViewBag.IsDoctorScoped = isDoctorScoped;
        ViewBag.Directory = await directory.SnapshotAsync(cancellationToken);
        ViewBag.Queue = await scheduling.QueueAsync(date?.Date ?? DateTime.UtcNow.Date, cancellationToken);
        return View(result);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> PublicCatalog(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        var isPatient = await dbContext.Patients.AsNoTracking().AnyAsync(p => p.Id == userId, cancellationToken);

        var departments = await dbContext.Departments.AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.Id)
            .Select(d => new
            {
                id = d.Id,
                code = d.Code,
                name = d.Name,
                floorLocation = d.FloorLocation,
                description = d.Description
            })
            .ToListAsync(cancellationToken);

        var followUpDoctorIds = isPatient
            ? (await dbContext.Appointments.AsNoTracking()
                .Where(a => a.PatientID == userId && a.Status == AppointStatus.Completed && a.DoctorID != null)
                .Select(a => a.DoctorID!)
                .Distinct()
                .ToListAsync(cancellationToken)).ToHashSet()
            : new HashSet<string>();

        var doctorsRaw = await dbContext.Doctors.AsNoTracking()
            .Include(d => d.Department)
            .Include(d => d.Specialization)
            .Include(d => d.Schedules)
            .Where(d => d.IsActive)
            .OrderByDescending(d => d.IsTopRated)
            .ThenBy(d => d.FirstName)
            .ToListAsync(cancellationToken);

        var doctors = doctorsRaw.Select(d =>
        {
            var deptId = d.DepartmentId ?? departments.FirstOrDefault(x =>
                string.Equals(x.name, d.Department?.Name ?? d.Specialization?.Name, StringComparison.OrdinalIgnoreCase))?.id ?? 0;
            var deptName = d.Department?.Name ?? d.Specialization?.Name ?? "General Clinic";
            var days = d.Schedules?
                .Where(s => s.Status == Status.Assigned)
                .Select(s => (int)s.Day)
                .Distinct()
                .OrderBy(x => x)
                .ToList() ?? [];
            return new
            {
                id = d.Id,
                name = $"Dr. {d.FirstName} {d.LastName}".Trim(),
                departmentId = deptId,
                departmentName = deptName,
                title = d.Title,
                credentials = d.Credentials,
                clinicRoom = d.ClinicRoom,
                image = string.IsNullOrWhiteSpace(d.Image) ? "doc-salma.jpg" : d.Image,
                fee = d.SessionPrice,
                followUpFee = d.FollowUpFee > 0 ? d.FollowUpFee : Math.Round(d.SessionPrice * 0.60m, 0),
                isFollowUp = followUpDoctorIds.Contains(d.Id),
                slotMinutes = d.SlotMinutes,
                daysOfWeek = days
            };
        }).ToList();

        return Json(new
        {
            isPatient,
            minDate = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd"),
            departments,
            doctors
        });
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> PublicAvailability(DateTime date, int? departmentId, string? doctorId, CancellationToken cancellationToken)
    {
        var targetDate = date.Date;
        if (targetDate <= DateTime.UtcNow.Date)
        {
            targetDate = DateTime.UtcNow.Date.AddDays(1);
        }

        var day = targetDate.DayOfWeek;
        var userId = CurrentUserId();
        var followUpDoctorIds = (await dbContext.Appointments.AsNoTracking()
            .Where(a => a.PatientID == userId && a.Status == AppointStatus.Completed && a.DoctorID != null)
            .Select(a => a.DoctorID!)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();

        var doctorsQuery = dbContext.Doctors.AsNoTracking()
            .Include(d => d.Department)
            .Include(d => d.Specialization)
            .Include(d => d.Schedules).ThenInclude(s => s.Shift)
            .Where(d => d.IsActive);

        if (!string.IsNullOrWhiteSpace(doctorId))
        {
            doctorsQuery = doctorsQuery.Where(d => d.Id == doctorId);
        }
        else if (departmentId.HasValue && departmentId.Value > 0)
        {
            doctorsQuery = doctorsQuery.Where(d => d.DepartmentId == departmentId.Value || d.SpecializationId == departmentId.Value);
        }

        var doctors = await doctorsQuery.ToListAsync(cancellationToken);
        var docIds = doctors.Select(d => d.Id).ToList();

        var taken = await dbContext.Appointments.AsNoTracking()
            .Where(a => a.AppointmentDate == targetDate && a.DoctorID != null && docIds.Contains(a.DoctorID))
            .Select(a => new { a.DoctorID, a.SlotMinutes, a.Status })
            .ToListAsync(cancellationToken);

        var clinics = new List<object>();
        foreach (var d in doctors)
        {
            var schedule = d.Schedules?
                .FirstOrDefault(s => s.Day == day && s.Status == Status.Assigned && s.Shift is not null);
            if (schedule?.Shift is null) continue;

            var generated = SlotRules.GenerateSlots(schedule.Shift.StartTIme, schedule.Shift.EndTIme, d.SlotMinutes);
            var busy = taken.Where(t => t.DoctorID == d.Id && SlotRules.OccupiesSlot(t.Status))
                .Select(t => t.SlotMinutes)
                .ToHashSet();

            var slots = generated.Select(m => new
            {
                slotMinutes = m,
                timeLabel = DateTime.Today.AddMinutes(m).ToString("hh:mm tt"),
                isAvailable = !busy.Contains(m)
            }).ToList();

            var openCount = slots.Count(s => s.isAvailable);
            clinics.Add(new
            {
                doctorId = d.Id,
                doctorName = $"Dr. {d.FirstName} {d.LastName}".Trim(),
                departmentId = d.DepartmentId ?? 0,
                departmentName = d.Department?.Name ?? d.Specialization?.Name ?? "General",
                clinicRoom = d.ClinicRoom,
                fee = d.SessionPrice,
                followUpFee = d.FollowUpFee > 0 ? d.FollowUpFee : Math.Round(d.SessionPrice * 0.60m, 0),
                isFollowUp = followUpDoctorIds.Contains(d.Id),
                openCount,
                slots
            });
        }

        return Json(new
        {
            date = targetDate.ToString("yyyy-MM-dd"),
            dayName = targetDate.ToString("dddd, dd MMM yyyy"),
            clinics
        });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublicBook([FromBody] PublicBookRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.DoctorId))
        {
            return BadRequest(new { ok = false, message = "Please choose a specialist physician." });
        }

        var userId = CurrentUserId();
        var isPatient = await dbContext.Patients.AsNoTracking().AnyAsync(p => p.Id == userId, cancellationToken);
        if (!isPatient)
        {
            return Ok(new
            {
                ok = false,
                message = "Only registered patient accounts can self-book online consultations. Clinical staff manage patient visits in Patients Registry."
            });
        }

        var res = await scheduling.BookAsync(userId, request.DoctorId, request.Date, request.SlotMinutes, request.Notes?.Trim(), userId, cancellationToken);
        if (!res.Succeeded || res.Value is null)
        {
            return Ok(new { ok = false, message = res.Error ?? "Could not complete reservation." });
        }

        var doc = await dbContext.Doctors.AsNoTracking()
            .Include(d => d.Department)
            .Include(d => d.Specialization)
            .FirstOrDefaultAsync(d => d.Id == request.DoctorId, cancellationToken);

        var isFollowUp = await dbContext.Appointments.AsNoTracking()
            .AnyAsync(a => a.PatientID == userId && a.DoctorID == request.DoctorId && a.Status == AppointStatus.Completed, cancellationToken);

        var chargedFee = doc is null ? 0m : (isFollowUp ? (doc.FollowUpFee > 0 ? doc.FollowUpFee : Math.Round(doc.SessionPrice * 0.60m, 0)) : doc.SessionPrice);
        var slotLabel = DateTime.Today.AddMinutes(res.Value.SlotMinutes).ToString("hh:mm tt");

        await audit.RecordAsync("patient.public-appointment-booked", userId, userId, "Appointment", res.Value.AppointmentID.ToString(), cancellationToken);
        await notifications.NotifyUserAsync(
            userId,
            "Appointment",
            $"Consultation Requested ({res.Value.Number})",
            $"Scheduled with Dr. {doc?.FirstName} {doc?.LastName} on {res.Value.AppointmentDate:ddd, dd MMM yyyy} at {slotLabel} ({(isFollowUp ? "Follow-Up" : "Initial")} • {chargedFee:0} EGP).",
            "/Updates",
            cancellationToken);

        return Ok(new
        {
            ok = true,
            appointmentNumber = res.Value.Number,
            doctorName = doc != null ? $"Dr. {doc.FirstName} {doc.LastName}".Trim() : "Attending Specialist",
            departmentName = doc?.Department?.Name ?? doc?.Specialization?.Name ?? "Outpatient",
            dateLabel = res.Value.AppointmentDate.ToString("ddd, dd MMM yyyy"),
            timeLabel = slotLabel,
            visitType = isFollowUp ? "Returning Follow-Up" : "Initial Consultation",
            fee = $"{chargedFee:0} EGP"
        });
    }

    [Authorize(Policy = "perm:" + Permission.AppointmentView)]
    public async Task<IActionResult> Book(DateTime? date, string? doctorId, string? patientId, CancellationToken cancellationToken)
    {
        var target = (date ?? DateTime.UtcNow.Date.AddDays(1)).Date;
        var boards = await scheduling.AvailabilityAsync(target, doctorId, cancellationToken);

        ViewData["Title"] = "Book appointment";
        ViewData["Section"] = "Scheduling";
        ViewData["Subtitle"] = "Pick a clinician and a free slot. Availability is computed from the weekly clinic schedule.";

        ViewBag.Date = target;
        ViewBag.Boards = boards;
        ViewBag.Directory = await directory.SnapshotAsync(cancellationToken);
        ViewBag.Patients = await patients.SearchAsync(null, 1, 50, cancellationToken);
        ViewBag.SelectedPatient = patientId;
        return View();
    }

    [HttpPost]
    [Authorize(Policy = "perm:" + Permission.AppointmentCreate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(string patientId, string doctorId, DateTime date, int slotMinutes, string? notes, CancellationToken cancellationToken)
    {
        var result = await scheduling.BookAsync(patientId, doctorId, date, slotMinutes, notes, CurrentUserId(), cancellationToken);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Error;
            return RedirectToAction(nameof(Book), new { date, doctorId, patientId });
        }

        await audit.RecordAsync("appointment.book", CurrentUserId(), patientId, "Appointment", result.Value!.AppointmentID.ToString(), cancellationToken);
        TempData["StatusMessage"] = $"Appointment {result.Value.Number} requested. Confirm it once the clinician reviews the request.";
        return RedirectToAction(nameof(Index), new { date = date.ToString("yyyy-MM-dd") });
    }

    [HttpPost]
    [Authorize(Policy = "perm:" + Permission.AppointmentManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, DateTime? date, CancellationToken cancellationToken)
    {
        var result = await scheduling.ConfirmAsync(id, CurrentUserId(), cancellationToken);
        await Flash(result, "appointment.confirm", id, "Appointment confirmed.");
        if (result.Succeeded)
        {
            var appt = await dbContext.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.AppointmentID == id, cancellationToken);
            if (appt is not null)
            {
                await notifications.NotifyUserAsync(
                    appt.PatientID,
                    "Appointment",
                    $"Appointment Confirmed ({appt.Number})",
                    $"Your appointment {appt.Number} on {appt.AppointmentDate:ddd, dd MMM yyyy} is confirmed.",
                    "/Updates",
                    cancellationToken);
            }
        }
        return RedirectToAction(nameof(Index), new { date = date?.ToString("yyyy-MM-dd") });
    }

    [HttpPost]
    [Authorize(Policy = "perm:" + Permission.AppointmentCheckIn)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(int id, DateTime? date, CancellationToken cancellationToken)
    {
        var result = await scheduling.CheckInAsync(id, CurrentUserId(), cancellationToken);
        if (result.Succeeded)
        {
            await audit.RecordAsync("appointment.checkin", CurrentUserId(), null, "Appointment", id.ToString(), cancellationToken);
            TempData["StatusMessage"] = "Patient checked in and an encounter was opened.";
            return RedirectToAction("Details", "Encounters", new { id = result.Value!.EncounterId });
        }

        TempData["ErrorMessage"] = result.Error;
        return RedirectToAction(nameof(Index), new { date = date?.ToString("yyyy-MM-dd") });
    }

    [HttpPost]
    [Authorize(Policy = "perm:" + Permission.AppointmentManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason, DateTime? date, CancellationToken cancellationToken)
    {
        var result = await scheduling.CancelAsync(id, CurrentUserId(), reason, cancellationToken);
        await Flash(result, "appointment.cancel", id, "Appointment cancelled and the slot released.");
        return RedirectToAction(nameof(Index), new { date = date?.ToString("yyyy-MM-dd") });
    }

    [HttpPost]
    [Authorize(Policy = "perm:" + Permission.AppointmentManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NoShow(int id, DateTime? date, CancellationToken cancellationToken)
    {
        var result = await scheduling.MarkNoShowAsync(id, CurrentUserId(), cancellationToken);
        await Flash(result, "appointment.noshow", id, "Appointment marked as a no-show.");
        return RedirectToAction(nameof(Index), new { date = date?.ToString("yyyy-MM-dd") });
    }

    private async Task Flash(Hospital.BLL.Common.Result result, string eventType, int id, string successMessage)
    {
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Error;
            return;
        }

        await audit.RecordAsync(eventType, CurrentUserId(), null, "Appointment", id.ToString());
        TempData["StatusMessage"] = successMessage;
    }
}

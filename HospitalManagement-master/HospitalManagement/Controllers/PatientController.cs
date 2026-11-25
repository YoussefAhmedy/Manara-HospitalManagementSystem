using System.Security.Claims;
using AutoMapper;
using Hospital.BLL.Helpers;
using Hospital.BLL.ModelVM;
using Hospital.BLL.Notifications;
using Hospital.BLL.Services.Abstraction;
using Hospital.DAL.DataBase;
using Hospital.DAL.Entities;
using Hospital.DAL.Entities.OwnedTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Controllers;

[Authorize(Roles = "Patient")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PatientController : Controller
{
    private readonly IDoctorService _doctorService;
    private readonly IPatientService _patientService;
    private readonly IMapper _mapper;
    private readonly IAppointmentService _appointmentService;
    private readonly ImedicalRecordService _medicalRecordService;
    private readonly IScheduleService _scheduleService;
    private readonly IEmailQueue _emailQueue;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<PatientController> _logger;
    private readonly HospitalDbContext _dbContext;

    public PatientController(
        IDoctorService doctorService,
        IPatientService patientService,
        IMapper mapper,
        IAppointmentService appointmentService,
        ImedicalRecordService medicalRecordService,
        IScheduleService scheduleService,
        IEmailQueue emailQueue,
        IAuditLogger auditLogger,
        ILogger<PatientController> logger,
        HospitalDbContext dbContext)
    {
        _doctorService = doctorService;
        _patientService = patientService;
        _mapper = mapper;
        _appointmentService = appointmentService;
        _medicalRecordService = medicalRecordService;
        _scheduleService = scheduleService;
        _emailQueue = emailQueue;
        _auditLogger = auditLogger;
        _logger = logger;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var patientId = CurrentUserId();
        if (patientId is null)
        {
            return Challenge();
        }

        var today = DateTime.Today;
        var appointments = _appointmentService.GetAppointments(appointment => appointment.PatientID == patientId);
        var upcoming = appointments
            .Where(appointment => appointment.AppointmentDate >= today && appointment.Status != AppointStatus.Cancelled)
            .OrderBy(appointment => appointment.AppointmentDate)
            .Take(5)
            .ToList();

        var recentRecords = await _medicalRecordService.GetForPatient(patientId)
            .OrderByDescending(record => record.RecordDate)
            .Take(5)
            .ToListAsync();

        await _auditLogger.RecordAsync("patient.dashboard-viewed", patientId, patientId, "patient-dashboard", patientId);
        return View(new PatientDashboardVm
        {
            UpcomingAppointments = upcoming,
            RecentRecords = recentRecords,
            TotalAppointments = appointments.Count(),
            UpcomingAppointmentCount = appointments.Count(appointment =>
                appointment.AppointmentDate >= today && appointment.Status != AppointStatus.Cancelled)
        });
    }

    [HttpGet]
    public async Task<IActionResult> MyCare()
    {
        var patientId = CurrentUserId();
        if (patientId is null) return Challenge();

        var patient = await _dbContext.Patients.AsNoTracking()
            .Include(p => p.Allergies)
            .FirstOrDefaultAsync(p => p.Id == patientId);
        if (patient is null) return NotFound();

        var prescriptions = await _dbContext.Prescriptions.AsNoTracking()
            .Include(p => p.Encounter).ThenInclude(e => e!.Doctor).ThenInclude(d => d!.Specialization)
            .Include(p => p.Items).ThenInclude(i => i.Medication)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync();

        var completedVisits = await _dbContext.Appointments.AsNoTracking()
            .Include(a => a.Doctor).ThenInclude(d => d!.Specialization)
            .Where(a => a.PatientID == patientId && a.Status == AppointStatus.Completed)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();

        var upcomingVisits = await _dbContext.Appointments.AsNoTracking()
            .Include(a => a.Doctor).ThenInclude(d => d!.Specialization)
            .Where(a => a.PatientID == patientId && a.AppointmentDate >= DateTime.Today && a.Status != AppointStatus.Cancelled)
            .OrderBy(a => a.AppointmentDate)
            .ToListAsync();

        var invoices = await _dbContext.Invoices.AsNoTracking()
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Where(i => i.PatientId == patientId)
            .OrderByDescending(i => i.IssuedAtUtc)
            .Take(10)
            .ToListAsync();

        var doctorsById = await _dbContext.Doctors.AsNoTracking()
            .Include(d => d.Specialization)
            .Where(d => d.IsActive)
            .ToDictionaryAsync(d => d.Id);

        await _auditLogger.RecordAsync("patient.mycare-viewed", patientId, patientId, "mycare", patientId);
        return View(new PatientMyCareVm
        {
            Patient = patient,
            Prescriptions = prescriptions,
            CompletedVisits = completedVisits,
            UpcomingVisits = upcomingVisits,
            Invoices = invoices,
            DoctorsById = doctorsById
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BookFollowUp(string doctorId, DateTime preferredDate, string? notes)
    {
        var patientId = CurrentUserId();
        if (patientId is null) return Challenge();

        var doctor = await _doctorService.DoctorByIdAsync(doctorId);
        if (doctor is null)
        {
            TempData["ErrorMessage"] = "Doctor not found.";
            return RedirectToAction(nameof(MyCare));
        }

        var date = preferredDate.Date < DateTime.Today.AddDays(1) ? DateTime.Today.AddDays(1) : preferredDate.Date;
        var hasCompletedPrior = await _dbContext.Appointments.AnyAsync(
            a => a.PatientID == patientId && a.DoctorID == doctor.Id && a.Status == AppointStatus.Completed);

        var appliedFee = hasCompletedPrior ? doctor.FollowUpFee : doctor.SessionPrice;
        var label = hasCompletedPrior ? $"Follow-Up Visit ({appliedFee:0} EGP returning rate)" : $"Consultation ({appliedFee:0} EGP)";

        // Allocate non-conflicting slot between 09:30 and 13:30
        var existingSlots = await _dbContext.Appointments
            .Where(a => a.DoctorID == doctor.Id && a.AppointmentDate == date && a.Status != AppointStatus.Cancelled && a.Status != AppointStatus.NotApproved)
            .Select(a => a.SlotMinutes)
            .ToListAsync();
        int chosenSlot = 570;
        while (existingSlots.Contains(chosenSlot) && chosenSlot < 810) chosenSlot += 30;

        var appointment = new Appointment
        {
            Number = $"FUP-{Random.Shared.Next(10000, 99999)}",
            AppointmentDate = date,
            SlotMinutes = chosenSlot,
            DurationMinutes = doctor.SlotMinutes > 0 ? doctor.SlotMinutes : 30,
            Status = AppointStatus.Approved,
            Notes = string.IsNullOrWhiteSpace(notes) ? label : $"{label} — {notes.Trim()}",
            PatientID = patientId,
            DoctorID = doctor.Id,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Appointments.Add(appointment);
        await _dbContext.SaveChangesAsync();

        await _auditLogger.RecordAsync("patient.followup-booked", patientId, patientId, "appointment", appointment.AppointmentID.ToString());
        TempData["StatusMessage"] = $"Follow-up confirmed with Dr. {doctor.FirstName} {doctor.LastName} on {date:ddd, dd MMM yyyy} ({appliedFee:0} EGP).";
        return RedirectToAction(nameof(MyCare));
    }

    [HttpGet]
    public async Task<IActionResult> AddAppointment(string? doctorId = null, int? departmentId = null)
    {
        var patientId = CurrentUserId();
        var followUpIds = string.IsNullOrWhiteSpace(patientId)
            ? new List<string>()
            : await _dbContext.Appointments.AsNoTracking()
                .Where(a => a.PatientID == patientId && a.Status == AppointStatus.Completed && a.DoctorID != null)
                .Select(a => a.DoctorID!)
                .Distinct()
                .ToListAsync();

        var departments = await _dbContext.Departments.AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .ToListAsync();

        var doctors = await _dbContext.Doctors.AsNoTracking()
            .Include(d => d.Department)
            .Include(d => d.Specialization)
            .Where(d => d.IsActive)
            .OrderByDescending(d => d.IsTopRated)
            .ThenBy(d => d.FirstName)
            .ToListAsync();

        if (departmentId is null && !string.IsNullOrWhiteSpace(doctorId))
        {
            departmentId = doctors.FirstOrDefault(d => d.Id == doctorId)?.DepartmentId;
        }

        ViewBag.FollowUpDoctorIds = followUpIds;
        return View(new AppointmentVm
        {
            Departments = departments,
            Doctors = doctors,
            DepartmentId = departmentId,
            DoctorId = doctorId ?? string.Empty
        });
    }

    [HttpPost]
    public async Task<IActionResult> AddAppointment(AppointmentVm model)
    {
        model.Departments = await _dbContext.Departments.AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .ToListAsync();
        model.Doctors = await _dbContext.Doctors.AsNoTracking()
            .Include(d => d.Department)
            .Include(d => d.Specialization)
            .Where(d => d.IsActive)
            .OrderByDescending(d => d.IsTopRated)
            .ThenBy(d => d.FirstName)
            .ToListAsync();
        ViewBag.FollowUpDoctorIds = await _dbContext.Appointments.AsNoTracking()
            .Where(a => a.PatientID == CurrentUserId() && a.Status == AppointStatus.Completed && a.DoctorID != null)
            .Select(a => a.DoctorID!)
            .Distinct()
            .ToListAsync();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var patientId = CurrentUserId();
        if (patientId is null)
        {
            return Challenge();
        }

        var date = model.AppointmentDate.Date;
        if (!AppointmentRules.IsBookableDate(date, DateTime.Today))
        {
            ModelState.AddModelError(nameof(model.AppointmentDate), "Choose a future clinic date.");
            return View(model);
        }

        var doctor = await _doctorService.DoctorByIdAsync(model.DoctorId);
        if (doctor is null)
        {
            ModelState.AddModelError(nameof(model.DoctorId), "Choose an available clinician.");
            return View(model);
        }

        var hasSchedule = _scheduleService.GetDoctorSchedulesById(doctor.Id).Any(schedule =>
            schedule.Status == Status.Assigned && AppointmentRules.IsScheduledForDate(date, schedule.Day));
        if (!hasSchedule)
        {
            ModelState.AddModelError(nameof(model.AppointmentDate), "This clinician is not scheduled for that day.");
            return View(model);
        }

        var appointment = new Appointment
        {
            AppointmentDate = date,
            Status = AppointStatus.Pending,
            Notes = model.Notes?.Trim(),
            PatientID = patientId,
            DoctorID = doctor.Id
        };

        if (!await _appointmentService.AddAppointment(appointment))
        {
            _logger.LogWarning("An appointment booking request could not be saved.");
            ModelState.AddModelError(string.Empty,
                "The request could not be saved. The clinician's daily capacity may have filled; please choose another date.");
            return View(model);
        }

        await _auditLogger.RecordAsync("patient.appointment-requested", patientId, patientId,
            "appointment", appointment.AppointmentID.ToString());
        var patientEmail = User.FindFirstValue(ClaimTypes.Email);
        var queued = !string.IsNullOrWhiteSpace(patientEmail) && _emailQueue.TryEnqueue(
            patientEmail,
            "Appointment request received",
            EmailTemplates.AppointmentUpdate(
                User.FindFirstValue(ClaimTypes.GivenName) ?? "there",
                "pending review",
                date.ToString("D")));
        TempData["StatusMessage"] = queued
            ? "Appointment request received. A confirmation email has been queued; we will notify you when it is reviewed."
            : "Appointment request received. Email delivery is not configured; updates will appear in your dashboard.";
        return RedirectToAction(nameof(ListAppointments));
    }

    [HttpGet]
    public async Task<IActionResult> ListAppointments()
    {
        var patientId = CurrentUserId();
        if (patientId is null)
        {
            return Challenge();
        }

        await _auditLogger.RecordAsync("patient.appointments-viewed", patientId, patientId, "appointments", patientId);
        var appointments = await _appointmentService.GetAppointments(appointment => appointment.PatientID == patientId)
            .OrderByDescending(appointment => appointment.AppointmentDate)
            .ToListAsync();
        return View(appointments);
    }

    [HttpGet]
    public async Task<IActionResult> AppointmentDetails(int? id)
    {
        var patientId = CurrentUserId();
        if (patientId is null)
        {
            return Challenge();
        }
        if (id is null)
        {
            return BadRequest();
        }

        var appointment = await _appointmentService.GetAppointmentById(id.Value);
        if (appointment is null || !ResourceOwnership.IsOwner(patientId, appointment.PatientID))
        {
            await _auditLogger.RecordAsync("security.patient-appointment-access-denied", patientId,
                appointment?.PatientID, "appointment", id.Value.ToString());
            return NotFound();
        }

        await _auditLogger.RecordAsync("patient.appointment-viewed", patientId, patientId,
            "appointment", appointment.AppointmentID.ToString());
        var model = _mapper.Map<AppointmentVm>(appointment);
        model.Id = appointment.AppointmentID;
        model.DoctorId = appointment.DoctorID ?? string.Empty;
        model.Doctors = _doctorService.GetAllDoctors();
        return View(model);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        var patientId = CurrentUserId();
        if (patientId is null)
        {
            return Challenge();
        }

        var appointment = await _appointmentService.GetAppointmentById(id);
        if (appointment is null || !ResourceOwnership.IsOwner(patientId, appointment.PatientID))
        {
            await _auditLogger.RecordAsync("security.patient-appointment-cancel-denied", patientId,
                appointment?.PatientID, "appointment", id.ToString());
            return NotFound();
        }

        if (appointment.AppointmentDate.Date < DateTime.Today || appointment.Status == AppointStatus.Cancelled)
        {
            return Conflict("This appointment can no longer be cancelled.");
        }

        appointment.Status = AppointStatus.Cancelled;
        if (!await _appointmentService.UpdateAppointment(appointment))
        {
            return Conflict("The appointment changed while you were viewing it. Refresh and try again.");
        }

        await _auditLogger.RecordAsync("patient.appointment-cancelled", patientId, patientId,
            "appointment", appointment.AppointmentID.ToString());
        return Ok(new { redirect = "/Patient/ListAppointments" });
    }

    [HttpPost]
    public async Task<IActionResult> EditAppointment(AppointmentVm model)
    {
        model.Doctors = _doctorService.GetAllDoctors();
        var patientId = CurrentUserId();
        if (patientId is null)
        {
            return Challenge();
        }
        if (model.Id is null)
        {
            return BadRequest();
        }

        var appointment = await _appointmentService.GetAppointmentById(model.Id.Value);
        if (appointment is null || !ResourceOwnership.IsOwner(patientId, appointment.PatientID))
        {
            await _auditLogger.RecordAsync("security.patient-appointment-edit-denied", patientId,
                appointment?.PatientID, "appointment", model.Id.Value.ToString());
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View("AppointmentDetails", model);
        }
        if (appointment.Status is AppointStatus.Approved or AppointStatus.Cancelled)
        {
            ModelState.AddModelError(string.Empty, "Approved or cancelled appointments cannot be changed here.");
            return View("AppointmentDetails", model);
        }
        if (model.AppointmentDate.Date < DateTime.Today)
        {
            ModelState.AddModelError(nameof(model.AppointmentDate), "Choose today or a future clinic date.");
            return View("AppointmentDetails", model);
        }

        var doctor = await _doctorService.DoctorByIdAsync(model.DoctorId);
        if (doctor is null)
        {
            ModelState.AddModelError(nameof(model.DoctorId), "Choose an available clinician.");
            return View("AppointmentDetails", model);
        }
        var hasSchedule = _scheduleService.GetDoctorSchedulesById(doctor.Id).Any(schedule =>
            schedule.Status == Status.Assigned &&
            AppointmentRules.IsScheduledForDate(model.AppointmentDate.Date, schedule.Day));
        if (!hasSchedule)
        {
            ModelState.AddModelError(nameof(model.AppointmentDate), "This clinician is not scheduled for that day.");
            return View("AppointmentDetails", model);
        }

        appointment.AppointmentDate = model.AppointmentDate.Date;
        appointment.Notes = model.Notes?.Trim();
        appointment.DoctorID = doctor.Id;
        appointment.Status = AppointStatus.Pending;
        appointment.Doctor = doctor;
        if (!await _appointmentService.UpdateAppointment(appointment))
        {
            ModelState.AddModelError(string.Empty,
                "The appointment could not be updated. It may have changed or the clinician's day may be full.");
            return View("AppointmentDetails", model);
        }

        await _auditLogger.RecordAsync("patient.appointment-rescheduled", patientId, patientId,
            "appointment", appointment.AppointmentID.ToString());
        TempData["StatusMessage"] = "Your appointment request was updated.";
        return RedirectToAction(nameof(ListAppointments));
    }

    [HttpGet]
    public async Task<IActionResult> GetMedicalRecords()
    {
        var patientId = CurrentUserId();
        if (patientId is null)
        {
            return Challenge();
        }

        await _auditLogger.RecordAsync("patient.medical-records-viewed", patientId, patientId,
            "medical-records", patientId);
        var records = await _medicalRecordService.GetForPatient(patientId)
            .OrderByDescending(record => record.RecordDate)
            .Take(100)
            .ToListAsync();
        return View(records);
    }

    [HttpGet]
    public async Task<IActionResult> GetDoctorDetails(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        var doctor = await _doctorService.GetDoctorAndSchedulesById(id);
        return doctor is null ? NotFound() : View(doctor);
    }

    private string? CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
}

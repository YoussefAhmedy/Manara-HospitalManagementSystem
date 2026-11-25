using System.Security.Claims;
using Hospital.BLL.Helpers;
using Hospital.BLL.ModelVM;
using Hospital.BLL.Services.Abstraction;
using Hospital.DAL.DataBase;
using Hospital.DAL.Entities;
using Hospital.DAL.Entities.Clinical;
using Hospital.DAL.Entities.Diagnostics;
using Hospital.DAL.Entities.OwnedTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Controllers;

public sealed class UpdatesController(
    HospitalDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IEmailNotificationService notifications,
    IAuditLogger audit) : Controller
{
    private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "My Updates & Notifications";

        var userId = CurrentUserId();
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Challenge();
        }

        var isPatient = User.IsInRole(nameof(Role.Patient)) &&
                        !User.IsInRole(nameof(Role.Admin)) &&
                        !User.IsInRole(nameof(Role.HospitalAdmin)) &&
                        !User.IsInRole(nameof(Role.Doctor));
        var isDoctor = User.IsInRole(nameof(Role.Doctor));
        var isManager = User.IsInRole(nameof(Role.Admin)) || User.IsInRole(nameof(Role.HospitalAdmin)) || User.IsInRole(nameof(Role.Management));

        var unreadCount = await notifications.GetUnreadCountAsync(userId, cancellationToken);
        var doctorMap = await dbContext.Doctors.AsNoTracking()
            .Include(d => d.Department)
            .Include(d => d.Specialization)
            .ToDictionaryAsync(d => d.Id, cancellationToken);

        if (isPatient)
        {
            var patient = await dbContext.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == userId, cancellationToken);
            var releasedLabs = await dbContext.LabOrders.AsNoTracking()
                .Include(o => o.Items).ThenInclude(i => i.LabTest)
                .Where(o => o.PatientId == userId && o.Status == LabOrderStatus.Released)
                .OrderByDescending(o => o.ReleasedAtUtc ?? o.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            var releasedImaging = await dbContext.ImagingOrders.AsNoTracking()
                .Where(i => i.PatientId == userId && i.Status == ImagingOrderStatus.Released)
                .OrderByDescending(i => i.VerifiedAtUtc ?? i.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            var notes = await dbContext.ClinicalNotes.AsNoTracking()
                .Include(n => n.Encounter)
                .Where(n => n.Encounter != null && n.Encounter.PatientId == userId && n.Status == NoteStatus.Signed)
                .OrderByDescending(n => n.SignedAtUtc ?? n.CreatedAtUtc)
                .Take(10)
                .ToListAsync(cancellationToken);

            var prescriptions = await dbContext.Prescriptions.AsNoTracking()
                .Include(p => p.Items).ThenInclude(i => i.Medication)
                .Where(p => p.PatientId == userId)
                .OrderByDescending(p => p.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            var upcoming = await dbContext.Appointments.AsNoTracking()
                .Where(a => a.PatientID == userId &&
                            (a.Status == AppointStatus.Pending || a.Status == AppointStatus.Approved || a.Status == AppointStatus.CheckedIn))
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync(cancellationToken);

            var completed = await dbContext.Appointments.AsNoTracking()
                .Where(a => a.PatientID == userId && a.Status == AppointStatus.Completed)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync(cancellationToken);

            var inbox = await dbContext.EmailNotificationLogs.AsNoTracking()
                .Where(n => n.RecipientUserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .ToListAsync(cancellationToken);

            var inquiries = await dbContext.ConsultationInquiries.AsNoTracking()
                .Include(c => c.Department)
                .Where(c => c.PatientUserId == userId || c.PatientEmail == user.Email)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(cancellationToken);

            return View(new UpdatesHubVm
            {
                IsAuthenticated = true,
                IsPatient = true,
                DisplayName = $"{user.FirstName} {user.LastName}".Trim(),
                UnreadCount = unreadCount,
                Patient = patient,
                ReleasedLabOrders = releasedLabs,
                ReleasedImagingOrders = releasedImaging,
                DoctorNotes = notes,
                Prescriptions = prescriptions,
                UpcomingAppointments = upcoming,
                CompletedAppointments = completed,
                Notifications = inbox,
                PatientInquiries = inquiries,
                DoctorsById = doctorMap
            });
        }

        // Doctor or Manager/Staff Hub
        doctorMap.TryGetValue(userId, out var currentDoctor);
        var visitsQuery = dbContext.Appointments.AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.Status != AppointStatus.Cancelled);

        if (isDoctor && !isManager)
        {
            visitsQuery = visitsQuery.Where(a => a.DoctorID == userId);
        }

        var assignedVisits = await visitsQuery
            .OrderByDescending(a => a.AppointmentDate)
            .Take(16)
            .ToListAsync(cancellationToken);

        var openInquiriesQuery = dbContext.ConsultationInquiries.AsNoTracking()
            .Include(c => c.Department)
            .Where(c => c.Status == "Pending");

        if (isDoctor && !isManager && currentDoctor?.DepartmentId is int docDeptId)
        {
            openInquiriesQuery = openInquiriesQuery.Where(c => c.DepartmentId == null || c.DepartmentId == docDeptId);
        }

        var openInquiries = await openInquiriesQuery
            .OrderByDescending(c => c.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        var answeredInquiries = await dbContext.ConsultationInquiries.AsNoTracking()
            .Include(c => c.Department)
            .Where(c => c.Status == "Answered")
            .OrderByDescending(c => c.RepliedAt ?? c.CreatedAt)
            .Take(12)
            .ToListAsync(cancellationToken);

        return View(new UpdatesHubVm
        {
            IsAuthenticated = true,
            IsDoctor = isDoctor,
            IsManager = isManager,
            DisplayName = $"{user.FirstName} {user.LastName}".Trim(),
            UnreadCount = unreadCount,
            AttendingDoctor = currentDoctor,
            AssignedPatientVisits = assignedVisits,
            OpenInquiries = openInquiries,
            AnsweredInquiries = answeredInquiries,
            DoctorsById = doctorMap,
            QuickReplyTemplates =
            [
                ("Normal Results Guidance", "Thank you for reaching out. Based on your recent MANARA clinical parameters, no acute intervention is needed right now. Continue your current daily regimen, maintain hydration, and log any new changes."),
                ("Schedule Follow-Up", "Your inquiry merits an in-person clinical examination so we can review vital signs and adjust therapy safely. Please use the Book Follow-Up link in My Updates to select a returning consultation slot at the reduced follow-up rate."),
                ("Prep Before Visit", "Please bring prior medication boxes and complete an 8-hour plain-water fast if additional morning lab biomarkers are scheduled prior to your review.")
            ]
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await notifications.MarkAllReadAsync(CurrentUserId(), cancellationToken);
        TempData["StatusMessage"] = "All notifications and doctor replies marked as read.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplyInquiry(int id, string replyText, CancellationToken cancellationToken)
    {
        if (!User.IsInRole(nameof(Role.Doctor)) &&
            !User.IsInRole(nameof(Role.Admin)) &&
            !User.IsInRole(nameof(Role.HospitalAdmin)) &&
            !User.IsInRole(nameof(Role.Management)))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(replyText))
        {
            TempData["ErrorMessage"] = "Please enter a clinical response before sending.";
            return RedirectToAction(nameof(Index));
        }

        var inquiry = await dbContext.ConsultationInquiries.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (inquiry is null)
        {
            return NotFound();
        }

        var responder = await userManager.FindByIdAsync(CurrentUserId());
        var responderName = responder is not null
            ? (User.IsInRole(nameof(Role.Doctor)) ? $"Dr. {responder.FirstName} {responder.LastName}".Trim() : $"{responder.FirstName} {responder.LastName}".Trim())
            : "MANARA Physician";

        inquiry.DoctorReply = replyText.Trim();
        inquiry.RepliedByUserId = CurrentUserId();
        inquiry.RepliedByName = responderName;
        inquiry.RepliedAt = DateTime.UtcNow;
        inquiry.Status = "Answered";
        inquiry.IsReadByPatient = false;
        inquiry.PatientNotifiedByEmail = true;

        await dbContext.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(inquiry.PatientUserId))
        {
            await notifications.NotifyUserAsync(
                inquiry.PatientUserId,
                "ConsultationReply",
                $"{responderName} replied to your consultation: {inquiry.Subject}",
                inquiry.DoctorReply,
                "/Updates",
                cancellationToken);
        }

        await audit.RecordAsync("consultation.reply", CurrentUserId(), inquiry.PatientUserId, "ConsultationInquiry", id.ToString(), cancellationToken);
        TempData["StatusMessage"] = $"Reply posted by {responderName} and patient notified in My Updates.";
        return RedirectToAction(nameof(Index));
    }
}

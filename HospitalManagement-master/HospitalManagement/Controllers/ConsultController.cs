using System.Security.Claims;
using Hospital.BLL.Helpers;
using Hospital.BLL.ModelVM;
using Hospital.BLL.Permissions;
using Hospital.BLL.Services.Abstraction;
using Hospital.DAL.DataBase;
using Hospital.DAL.Entities;
using Hospital.DAL.Entities.Clinical;
using Hospital.DAL.Entities.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Controllers;

public sealed class ConsultController(
    HospitalDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IMedicalAssistantService assistant,
    IAuditLogger audit) : Controller
{
    public sealed record AssistantPromptRequest(string Prompt);

    private string? CurrentUserIdOrNull() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index(int? departmentId = null, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Consult & Feedback";
        var departments = await dbContext.Departments.AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

        var allReviews = await dbContext.PlatformFeedbacks.AsNoTracking()
            .Where(f => f.Category == "Review")
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);

        var reviews = allReviews.Take(8).ToList();
        var ratingValues = allReviews.Where(f => f.Rating.HasValue).Select(f => f.Rating!.Value).ToList();

        ApplicationUser? currentUser = null;
        var userId = CurrentUserIdOrNull();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            currentUser = await userManager.FindByIdAsync(userId);
        }

        return View(new ConsultHubVm
        {
            Departments = departments,
            PublishedReviews = reviews,
            PrefillName = currentUser is not null ? $"{currentUser.FirstName} {currentUser.LastName}".Trim() : null,
            PrefillEmail = currentUser?.Email,
            PreselectedDepartmentId = departmentId,
            TotalReviewsCount = ratingValues.Count,
            AverageRating = ratingValues.Count > 0 ? Math.Round(ratingValues.Average(), 2) : 4.9
        });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitQuestion(
        string patientName,
        string patientEmail,
        int? departmentId,
        string? doctorId,
        string subject,
        string question,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(patientName) || string.IsNullOrWhiteSpace(patientEmail) ||
            string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(question))
        {
            TempData["ErrorMessage"] = "Please complete your name, email, consultation topic, and clinical question.";
            return RedirectToAction(nameof(Index));
        }

        var userId = CurrentUserIdOrNull();
        if (departmentId == 0) departmentId = null;

        dbContext.ConsultationInquiries.Add(new ConsultationInquiry
        {
            PatientUserId = userId,
            PatientName = patientName.Trim(),
            PatientEmail = patientEmail.Trim(),
            DepartmentId = departmentId,
            Subject = subject.Trim(),
            Question = question.Trim(),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("consultation.submit", userId, userId, "ConsultationInquiry", subject.Trim(), cancellationToken);
        TempData["StatusMessage"] = "Your medical question has been routed to our attending department specialists. Track responses anytime inside My Updates.";
        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitFeedback(
        string authorName,
        string authorEmail,
        string category,
        string targetScope,
        string? targetName,
        int? rating,
        string subject,
        string message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(authorName) || string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
        {
            TempData["ErrorMessage"] = "Please enter your name, subject, and feedback details.";
            return RedirectToAction(nameof(Index));
        }

        var normalizedCat = category is "Suggestion" or "Complaint" ? category : "Review";
        var normalizedScope = targetScope is "Doctor" or "Department" or "Staff" ? targetScope : "Hospital";
        var clampedRating = normalizedCat == "Review" ? Math.Clamp(rating ?? 5, 1, 5) : (int?)null;

        var userId = CurrentUserIdOrNull();
        dbContext.PlatformFeedbacks.Add(new PlatformFeedback
        {
            UserId = userId,
            AuthorName = authorName.Trim(),
            AuthorEmail = (authorEmail ?? string.Empty).Trim(),
            Category = normalizedCat,
            TargetScope = normalizedScope,
            TargetName = string.IsNullOrWhiteSpace(targetName) ? "MANARA Medical Complex" : targetName.Trim(),
            Rating = clampedRating,
            Subject = subject.Trim(),
            Message = message.Trim(),
            Status = "New",
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync($"feedback.{normalizedCat.ToLowerInvariant()}", userId, userId, "PlatformFeedback", subject.Trim(), cancellationToken);
        TempData["StatusMessage"] = normalizedCat == "Review"
            ? "Thank you for sharing your 5-star MANARA experience!"
            : $"Your {normalizedCat.ToLowerInvariant()} has been logged for our Hospital Manager review.";
        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AskAssistant([FromBody] AssistantPromptRequest request, CancellationToken cancellationToken)
    {
        var reply = await assistant.AskAsync(request?.Prompt ?? string.Empty, cancellationToken);
        return Json(new
        {
            isMedical = reply.IsMedical,
            isUrgent = reply.IsUrgent,
            answer = reply.Answer,
            suggestedDepartment = reply.SuggestedDepartment,
            suggestedDoctorName = reply.SuggestedDoctorName,
            disclaimer = reply.Disclaimer
        });
    }

    [Authorize(Policy = "perm:" + Permission.AuditView)]
    [HttpGet]
    public async Task<IActionResult> Manage(string? category = null, string? status = null, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Feedback, Suggestions & Complaints";
        ViewData["Subtitle"] = "Hospital Manager oversight queue for patient experience, clinic suggestions, and service concerns.";

        var query = dbContext.PlatformFeedbacks.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(f => f.Category == category);
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(f => f.Status == status);
        }

        var items = await query.OrderByDescending(f => f.CreatedAt).Take(100).ToListAsync(cancellationToken);
        return View(new ManageFeedbackVm
        {
            Items = items,
            SelectedCategory = category,
            SelectedStatus = status
        });
    }

    [Authorize(Policy = "perm:" + Permission.AuditView)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveFeedback(int id, string status, string? managerNotes, CancellationToken cancellationToken)
    {
        var entry = await dbContext.PlatformFeedbacks.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (entry is null) return NotFound();

        entry.Status = status is "Reviewed" or "Resolved" ? status : "Reviewed";
        entry.ManagerNotes = managerNotes?.Trim();
        entry.ReviewedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await audit.RecordAsync("feedback.resolve", CurrentUserIdOrNull(), entry.UserId, "PlatformFeedback", id.ToString(), cancellationToken);
        TempData["StatusMessage"] = $"Feedback item #{id} updated to {entry.Status}.";
        return RedirectToAction(nameof(Manage));
    }
}

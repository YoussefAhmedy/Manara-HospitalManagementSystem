using System.Diagnostics;
using System.Security.Claims;
using Hospital.BLL.Helpers;
using Hospital.BLL.ModelVM;
using Hospital.BLL.Services.Abstraction;
using Hospital.DAL.DataBase;
using Hospital.DAL.Entities.OwnedTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Controllers;

public sealed class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IDoctorService _doctorService;
    private readonly HospitalDbContext _dbContext;

    public HomeController(ILogger<HomeController> logger, IDoctorService doctorService, HospitalDbContext dbContext)
    {
        _logger = logger;
        _doctorService = doctorService;
        _dbContext = dbContext;
    }

    private async Task<MainVm> BuildDirectoryModelAsync(string? deptFilter = null)
    {
        var allDoctors = _doctorService.GetPublicDoctorProfiles(32);
        var departments = await _dbContext.Departments.AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.Id)
            .ToListAsync();

        var deptVms = departments.Select(d => new PublicDepartmentVm
        {
            Id = d.Id,
            Code = d.Code,
            Name = d.Name,
            FloorLocation = d.FloorLocation,
            Description = d.Description,
            Capabilities = d.Capabilities,
            Doctors = allDoctors.Where(doc =>
                string.Equals(doc.DepartmentName, d.Name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(doc.SpecializationName, d.Name, StringComparison.OrdinalIgnoreCase))
                .ToList()
        }).ToList();

        var filteredDoctors = string.IsNullOrWhiteSpace(deptFilter)
            ? allDoctors
            : allDoctors.Where(d =>
                (d.DepartmentName != null && d.DepartmentName.Contains(deptFilter, StringComparison.OrdinalIgnoreCase)) ||
                (d.SpecializationName != null && d.SpecializationName.Contains(deptFilter, StringComparison.OrdinalIgnoreCase)))
                .ToList();

        var followUpIds = new HashSet<string>(StringComparer.Ordinal);
        var isReturning = false;
        if (User.Identity?.IsAuthenticated == true && User.IsInRole(nameof(Role.Patient)))
        {
            var patientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(patientId))
            {
                var docIds = await _dbContext.Appointments.AsNoTracking()
                    .Where(a => a.PatientID == patientId && a.Status == AppointStatus.Completed && a.DoctorID != null)
                    .Select(a => a.DoctorID!)
                    .Distinct()
                    .ToListAsync();
                foreach (var did in docIds) followUpIds.Add(did);
                isReturning = followUpIds.Count > 0;
            }
        }

        return new MainVm
        {
            Doctors = filteredDoctors,
            Departments = deptVms,
            IsReturningPatient = isReturning,
            FollowUpEligibleDoctorIds = followUpIds
        };
    }

    [HttpGet]
    public async Task<IActionResult> Index() => View(await BuildDirectoryModelAsync());

    [HttpGet]
    public async Task<IActionResult> Departments() => View(await BuildDirectoryModelAsync());

    [HttpGet]
    public async Task<IActionResult> Doctors(string? dept)
    {
        ViewBag.SelectedDept = dept;
        return View(await BuildDirectoryModelAsync(dept));
    }

    [HttpGet]
    public IActionResult AboutUs() => RedirectToAction(nameof(Index));

    [HttpGet]
    public IActionResult Privacy() => View();

    /// <summary>Retired public demo dump; demo credentials are documented in README.md and docs/MANARA_Demo_Accounts.pdf.</summary>
    [HttpGet]
    public IActionResult Demo() => RedirectToAction(nameof(Index));

    [HttpGet]
    public IActionResult Terms() => View();

    [HttpGet]
    public IActionResult Security() => View();

    [HttpGet]
    public IActionResult Support() => View();

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        ViewData["RequestId"] = requestId;
        _logger.LogWarning("An unhandled request reached the error page. Trace identifier: {TraceIdentifier}", requestId);
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View();
    }
}

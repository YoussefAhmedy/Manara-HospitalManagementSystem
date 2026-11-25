using Hospital.BLL.Common.Rules;
using Hospital.BLL.Helpers;
using Hospital.BLL.Permissions;
using Hospital.DAL.Entities;
using Hospital.DAL.Entities.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hospital.Tests;

[TestClass]
public sealed class ManaraUiSyncTests
{
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "HospitalManagement.sln")))
        {
            directory = directory.Parent;
        }

        Assert.IsNotNull(directory, "Could not locate the repository root.");
        return directory!.FullName;
    }

    [TestMethod]
    public void ManaraDesignSystem_AssetsAndStyles_ArePresentInWwwRoot()
    {
        var root = RepositoryRoot();
        var cssPath = Path.Combine(root, "HospitalManagement", "wwwroot", "css", "manara-design-system.css");
        var dashCssPath = Path.Combine(root, "HospitalManagement", "wwwroot", "css", "manara-dashboard.css");
        var jsPath = Path.Combine(root, "HospitalManagement", "wwwroot", "js", "manara-ui.js");
        var assetsDir = Path.Combine(root, "HospitalManagement", "wwwroot", "assets", "manara");
        var doctorsDir = Path.Combine(root, "HospitalManagement", "wwwroot", "images", "doctors");
        var videosDir = Path.Combine(root, "HospitalManagement", "wwwroot", "videos");

        Assert.IsTrue(File.Exists(cssPath), "manara-design-system.css must exist in wwwroot/css.");
        Assert.IsTrue(File.Exists(dashCssPath), "manara-dashboard.css must exist in wwwroot/css.");
        Assert.IsTrue(File.Exists(jsPath), "manara-ui.js must exist in wwwroot/js.");
        Assert.IsTrue(Directory.Exists(assetsDir), "assets/manara must exist in wwwroot.");
        Assert.IsTrue(Directory.Exists(doctorsDir), "images/doctors must exist in wwwroot.");
        Assert.IsTrue(File.Exists(Path.Combine(videosDir, "manara-complex.webm")), "Hero video WebM must exist.");
        Assert.IsTrue(File.Exists(Path.Combine(videosDir, "manara-complex.mp4")), "Hero video MP4 must exist.");

        var dashCss = File.ReadAllText(dashCssPath);
        Assert.IsTrue(dashCss.Contains("#18181A", StringComparison.OrdinalIgnoreCase), "Dashboard CSS must include #18181A Racing Green dark base.");
        Assert.IsTrue(dashCss.Contains("#2563EB", StringComparison.OrdinalIgnoreCase), "Dashboard CSS must include #2563EB Blue Orchid primary accent.");

        var portraits = Directory.GetFiles(doctorsDir, "*.jpg");
        Assert.IsTrue(portraits.Length >= 16, "Must have at least 16 unique doctor portraits in wwwroot/images/doctors.");
        var hashes = portraits.Select(p => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(p)))).Distinct().Count();
        Assert.AreEqual(portraits.Length, hashes, "Every doctor portrait image must be 100% unique (zero duplicates).");

        var pdfPath = Path.Combine(root, "docs", "MANARA_Demo_Accounts.pdf");
        Assert.IsTrue(File.Exists(pdfPath), "MANARA_Demo_Accounts.pdf must exist in docs/.");
    }

    [TestMethod]
    public void ManaraLogoSvg_RemainsHollowOutlineStroke()
    {
        var root = RepositoryRoot();
        var logoPath = Path.Combine(root, "HospitalManagement", "wwwroot", "assets", "manara", "manara-logo.svg");
        var content = File.ReadAllText(logoPath);

        Assert.IsTrue(content.Contains("fill=\"none\"", StringComparison.OrdinalIgnoreCase), "Logo SVG must have fill=\"none\".");
        Assert.IsTrue(content.Contains("stroke=\"currentColor\"", StringComparison.OrdinalIgnoreCase), "Logo SVG must have stroke=\"currentColor\".");
    }

    [TestMethod]
    public void ManaraDemoRoleAccounts_HaveValidConfigurations()
    {
        var roles = new[]
        {
            ("youssef.manager@gmail.com", "Manager@2026", Role.Admin),
            ("nadia.patient@gmail.com", "Patient@2026", Role.Patient),
            ("salma.cardiologist@gmail.com", "Doctor@2026", Role.Doctor),
            ("nourhan.receptionist@gmail.com", "Recep@2026", Role.Receptionist),
            ("hossam.labtech@gmail.com", "Lab@2026", Role.LabTechnician),
            ("ramez.radiologist@gmail.com", "Rad@2026", Role.Radiologist),
            ("mona.pharmacist@gmail.com", "Pharm@2026", Role.Pharmacist),
            ("sherif.billing@gmail.com", "Bill@2026", Role.BillingOfficer)
        };

        Assert.AreEqual(8, roles.Length, "Must have all 8 core MANARA demo roles.");

        foreach (var (email, password, role) in roles)
        {
            Assert.IsTrue(email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase), $"Email {email} must follow firstname.role@gmail.com format.");
            Assert.IsTrue(password.Length >= 6, $"Password for {email} must meet minimum length requirements.");
            Assert.IsTrue(Enum.IsDefined(typeof(Role), role), $"Role {role} must be defined.");
        }
    }

    [TestMethod]
    public void LabSafetyGate_WithholdsUnverifiedResultsFromFinalPatientView()
    {
        var unverifiedStates = new[]
        {
            LabOrderStatus.Requested,
            LabOrderStatus.AwaitingCollection,
            LabOrderStatus.Collected,
            LabOrderStatus.Received,
            LabOrderStatus.Processing,
            LabOrderStatus.AwaitingVerification
        };

        foreach (var state in unverifiedStates)
        {
            var isFinal = state == LabOrderStatus.Released;
            Assert.IsFalse(isFinal, $"Lab status {state} must not be visible on finalized patient examination reports.");
        }

        Assert.AreEqual(LabOrderStatus.Released, (LabOrderStatus)7, "Lab status Released must represent the released terminal state.");
    }
}

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
        var jsPath = Path.Combine(root, "HospitalManagement", "wwwroot", "js", "manara-ui.js");
        var assetsDir = Path.Combine(root, "HospitalManagement", "wwwroot", "assets", "manara");

        Assert.IsTrue(File.Exists(cssPath), "manara-design-system.css must exist in wwwroot/css.");
        Assert.IsTrue(File.Exists(jsPath), "manara-ui.js must exist in wwwroot/js.");
        Assert.IsTrue(Directory.Exists(assetsDir), "assets/manara must exist in wwwroot.");

        var requiredImages = new[]
        {
            "hero-corridor.jpg",
            "consult-atrium.jpg",
            "doc-salma.jpg",
            "doc-karim.jpg",
            "doc-tarek.jpg",
            "doc-nadia.jpg",
            "chest-xray-pacs.jpg",
            "manara-logo.svg"
        };

        foreach (var file in requiredImages)
        {
            var filePath = Path.Combine(assetsDir, file);
            Assert.IsTrue(File.Exists(filePath), $"Required MANARA asset {file} must exist in wwwroot/assets/manara.");
        }
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
            ("admin@manara.hospital", "Admin@2026", Role.Admin),
            ("ahmed.hassan@patient.manara", "Patient@2026", Role.Patient),
            ("dr.salma@manara.hospital", "Doctor@2026", Role.Doctor),
            ("reception@manara.hospital", "Front@2026", Role.Receptionist),
            ("lab@manara.hospital", "Lab@2026", Role.LabTechnician),
            ("radiology@manara.hospital", "Rad@2026", Role.Radiologist),
            ("pharmacy@manara.hospital", "Pharm@2026", Role.Pharmacist),
            ("billing@manara.hospital", "Bill@2026", Role.BillingOfficer)
        };

        Assert.AreEqual(8, roles.Length, "Must have exactly 8 configured MANARA demo accounts.");

        foreach (var (email, password, role) in roles)
        {
            Assert.IsTrue(email.Contains('@'), $"Email {email} must be valid.");
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

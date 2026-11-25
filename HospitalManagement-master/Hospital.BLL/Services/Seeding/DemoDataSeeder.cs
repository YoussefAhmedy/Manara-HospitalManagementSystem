using Hospital.BLL.Helpers;
using Hospital.DAL.DataBase;
using Hospital.DAL.Entities;
using Hospital.DAL.Entities.Billing;
using Hospital.DAL.Entities.Clinical;
using Hospital.DAL.Entities.Diagnostics;
using Hospital.DAL.Entities.OwnedTypes;
using Hospital.DAL.Entities.Operations;
using Hospital.DAL.Entities.Organization;
using Hospital.DAL.Entities.Pharmacy;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Hospital.BLL.Services.Seeding;

/// <summary>
/// Idempotent synthetic dataset for demonstration and evaluation. Accounts use synthetic
/// addresses and placeholder credentials that are printed once to the operator; no value
/// here is a secret and every patient row is flagged <c>IsSynthetic</c>.
/// <para>
/// The demonstration password is a published constant by design: these accounts exist to be
/// shared with evaluators, and pretending otherwise would be worse than saying so. An operator
/// may override it with <c>Demo:Password</c>, and the seeder refuses to run at all unless demo
/// seeding is explicitly enabled outside Development.
/// </para>
/// </summary>
public sealed class DemoDataSeeder(
    HospitalDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration) : IDemoDataSeeder
{
    public const string DefaultDemoPassword = "Careaxis-Demo-2026!";

    private string DemoPassword =>
        configuration["Demo:Password"] is { Length: > 0 } configured ? configured : DefaultDemoPassword;

    public async Task<string> SeedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureManaraAccountsAsync(cancellationToken);

        if (await dbContext.Branches.AnyAsync(cancellationToken))
        {
            return "Demo data already present; ensured MANARA role accounts.";
        }

        var branch = new Branch { Name = "MANARA Medical Complex", Code = "MNR-01", City = "Alexandria", Phone = "+20 3 480 0190" };
        dbContext.Branches.Add(branch);

        var departments = new List<Department>
        {
            new() { Name = "Internal Medicine", Code = "IM", Branch = branch },
            new() { Name = "Cardiology", Code = "CARD", Branch = branch },
            new() { Name = "Pediatrics", Code = "PED", Branch = branch },
            new() { Name = "Clinical Laboratory", Code = "LAB", Branch = branch },
            new() { Name = "Radiology & Imaging", Code = "RAD", Branch = branch },
            new() { Name = "Pharmacy", Code = "PHM", Branch = branch }
        };
        dbContext.Departments.AddRange(departments);

        var specializations = new List<Specialization>
        {
            new() { Name = "Internal Medicine" },
            new() { Name = "Cardiology" },
            new() { Name = "Pediatrics" }
        };
        dbContext.Specializations.AddRange(specializations);

        var shifts = new List<Shift>
        {
            new() { ShiftType = ShiftType.Morning, StartTIme = new TimeSpan(9, 0, 0), EndTIme = new TimeSpan(13, 0, 0) },
            new() { ShiftType = ShiftType.Afternoon, StartTIme = new TimeSpan(14, 0, 0), EndTIme = new TimeSpan(18, 0, 0) }
        };
        dbContext.Shifts.AddRange(shifts);

        var labTests = new List<LabTest>
        {
            new() { Code = "CBC", Name = "Complete blood count", Category = "Haematology", Unit = "10^9/L", NormalLow = 4, NormalHigh = 11, Price = 25 },
            new() { Code = "HGB", Name = "Haemoglobin", Category = "Haematology", Unit = "g/dL", NormalLow = 12, NormalHigh = 17, CriticalLow = 7, Price = 15 },
            new() { Code = "GLU", Name = "Fasting glucose", Category = "Chemistry", Unit = "mg/dL", NormalLow = 70, NormalHigh = 100, CriticalHigh = 400, Price = 18 },
            new() { Code = "K", Name = "Potassium", Category = "Chemistry", Unit = "mmol/L", NormalLow = 3.5m, NormalHigh = 5.0m, CriticalLow = 2.5m, CriticalHigh = 6.5m, Price = 20 },
            new() { Code = "NA", Name = "Sodium", Category = "Chemistry", Unit = "mmol/L", NormalLow = 135, NormalHigh = 145, Price = 20 },
            new() { Code = "CRP", Name = "C-reactive protein", Category = "Immunology", Unit = "mg/L", NormalHigh = 5, Price = 30 },
            new() { Code = "TSH", Name = "Thyroid stimulating hormone", Category = "Endocrinology", Unit = "mIU/L", NormalLow = 0.4m, NormalHigh = 4.0m, Price = 35 },
            new() { Code = "URIN", Name = "Urinalysis", Category = "Microbiology", Specimen = SpecimenType.Urine, Unit = "dipstick", Price = 12 }
        };
        dbContext.LabTests.AddRange(labTests);

        var medications = new List<Medication>
        {
            new() { Name = "Amoxicillin", GenericName = "Amoxicillin", Form = "capsule", Strength = "500 mg", Unit = "capsule", UnitPrice = 0.8m, ReorderLevel = 100 },
            new() { Name = "Paracetamol", GenericName = "Acetaminophen", Form = "tablet", Strength = "500 mg", Unit = "tablet", UnitPrice = 0.15m, ReorderLevel = 200 },
            new() { Name = "Metformin", GenericName = "Metformin", Form = "tablet", Strength = "850 mg", Unit = "tablet", UnitPrice = 0.3m, ReorderLevel = 100 },
            new() { Name = "Atorvastatin", GenericName = "Atorvastatin", Form = "tablet", Strength = "20 mg", Unit = "tablet", UnitPrice = 0.6m, ReorderLevel = 80 },
            new() { Name = "Salbutamol", GenericName = "Salbutamol", Form = "inhaler", Strength = "100 mcg", Unit = "inhaler", UnitPrice = 4.5m, ReorderLevel = 20 },
            new() { Name = "Omeprazole", GenericName = "Omeprazole", Form = "capsule", Strength = "20 mg", Unit = "capsule", UnitPrice = 0.35m, ReorderLevel = 120 }
        };
        dbContext.Medications.AddRange(medications);

        var serviceItems = new List<ServiceItem>
        {
            new() { Code = "CONS-IM", Name = "Internal medicine consultation", Category = ServiceCategory.Consultation, Price = 60, Department = departments[0] },
            new() { Code = "CONS-CARD", Name = "Cardiology consultation", Category = ServiceCategory.Consultation, Price = 90, Department = departments[1] },
            new() { Code = "LAB-CBC", Name = "Complete blood count", Category = ServiceCategory.Laboratory, Price = 25, Department = departments[3] },
            new() { Code = "RAD-CXR", Name = "Chest X-ray", Category = ServiceCategory.Radiology, Price = 70, Department = departments[4] },
            new() { Code = "ROOM-STD", Name = "Standard room (per day)", Category = ServiceCategory.Room, Price = 150, Department = departments[0] }
        };
        dbContext.ServiceItems.AddRange(serviceItems);

        var ward = new Ward { Name = "Inpatient Ward A", Code = "WA", Department = departments[0] };
        var room1 = new Room { Ward = ward, Number = "101", Class = "Standard", DailyRate = 150 };
        var room2 = new Room { Ward = ward, Number = "102", Class = "Standard", DailyRate = 150 };
        room1.Beds.AddRange([new Bed { Label = "101-A" }, new Bed { Label = "101-B" }]);
        room2.Beds.AddRange([new Bed { Label = "102-A" }, new Bed { Label = "102-B" }]);
        ward.Rooms.AddRange([room1, room2]);
        dbContext.Wards.Add(ward);

        await dbContext.SaveChangesAsync(cancellationToken);

        // ---------------------------------------------------------------- users
        var doctors = new List<Doctor>();
        var doctorSeeds = new[]
        {
            ("sara.hassan", "Sara", "Hassan", 0, departments[0]),
            ("omar.fahmy", "Omar", "Fahmy", 1, departments[1]),
            ("mona.adel", "Mona", "Adel", 2, departments[2])
        };

        foreach (var (handle, first, last, specIndex, department) in doctorSeeds)
        {
            var doctor = new Doctor
            {
                UserName = $"{handle}@example.invalid",
                Email = $"{handle}@example.invalid",
                EmailConfirmed = true,
                FirstName = first,
                LastName = last,
                Gender = specIndex == 2 ? "Female" : "Female",
                EmployeeNumber = $"DR-{specIndex + 1:000}",
                Salary = 18000,
                SessionPrice = 60,
                SlotMinutes = 20,
                Specialization = specializations[specIndex],
                Department = department,
                ClinicRoom = $"C-{200 + specIndex}"
            };

            var created = await userManager.CreateAsync(doctor, DemoPassword);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException($"Demo clinician creation failed: {string.Join(", ", created.Errors.Select(e => e.Code))}");
            }

            await userManager.AddToRoleAsync(doctor, Role.Doctor.ToString());
            doctors.Add(doctor);

            foreach (var day in new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday })
            {
                dbContext.Schedules.Add(new Schedule
                {
                    DoctorId = doctor.Id,
                    ShiftId = shifts[0].ShiftId,
                    Day = day,
                    Status = DAL.Entities.OwnedTypes.Status.Assigned
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var staffSeeds = new (string Handle, string First, string Last, Role Role)[]
        {
            ("youssef.manager.seed", "Youssef", "El-Masry", Role.HospitalAdmin),
            ("reception", "Nour", "Samir", Role.Receptionist),
            ("nurse", "Hala", "Ibrahim", Role.Nurse),
            ("lab", "Youssef", "Kamel", Role.LabTechnician),
            ("pharmacy", "Rania", "Mostafa", Role.Pharmacist),
            ("billing", "Tarek", "Aly", Role.BillingOfficer),
            ("records", "Dina", "Rashad", Role.MedicalRecordsOfficer),
            ("management", "Hassan", "Zaki", Role.Management),
            ("auditor", "Laila", "Nabil", Role.Auditor)
        };

        var staff = new List<(ApplicationUser User, Role Role)>();
        foreach (var seed in staffSeeds)
        {
            var user = new ApplicationUser
            {
                UserName = $"{seed.Handle}@example.invalid",
                Email = $"{seed.Handle}@example.invalid",
                EmailConfirmed = true,
                FirstName = seed.First,
                LastName = seed.Last,
                Gender = "Female"
            };

            var created = await userManager.CreateAsync(user, DemoPassword);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException($"Demo staff creation failed: {string.Join(", ", created.Errors.Select(e => e.Code))}");
            }

            await userManager.AddToRoleAsync(user, seed.Role.ToString());
            staff.Add((user, seed.Role));
        }

        // ---------------------------------------------------------------- patients
        var patientSeeds = new (string Handle, string First, string Last, string Sex, int Year, string Blood)[]
        {
            ("patient", "Layla", "Mahmoud", "Female", 1988, "A+"),
            ("ayman.saleh", "Ayman", "Saleh", "Male", 1972, "O-"),
            ("heba.fouad", "Heba", "Fouad", "Female", 1995, "B+"),
            ("kareem.taha", "Kareem", "Taha", "Male", 2014, "A-"),
            ("nadia.hussein", "Nadia", "Hussein", "Female", 1960, "AB+"),
            ("ziad.adel", "Ziad", "Adel", "Male", 1990, "O+")
        };

        var patients = new List<Patient>();
        var index = 1;
        foreach (var seed in patientSeeds)
        {
            var patient = new Patient
            {
                UserName = $"{seed.Handle}@example.invalid",
                Email = $"{seed.Handle}@example.invalid",
                EmailConfirmed = true,
                FirstName = seed.First,
                LastName = seed.Last,
                Gender = seed.Sex,
                DateOfBirth = new DateTime(seed.Year, 4, 12, 0, 0, 0, DateTimeKind.Utc),
                PhoneNumber = $"+20 100 000 {index:0000}",
                Address = "Synthetic address, Alexandria",
                EmergencyContact = "Synthetic contact +20 100 000 9999",
                BloodGroup = seed.Blood,
                Mrn = $"PT-{index:000000}",
                IsSynthetic = true,
                MedicalHistory = index == 2 ? "Hypertension diagnosed 2019; Type 2 diabetes." : null
            };

            var created = await userManager.CreateAsync(patient, DemoPassword);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException($"Demo patient creation failed: {string.Join(", ", created.Errors.Select(e => e.Code))}");
            }

            await userManager.AddToRoleAsync(patient, Role.Patient.ToString());
            patients.Add(patient);
            index++;
        }

        dbContext.Allergies.AddRange(
            new Allergy { Patient = patients[1], Substance = "Penicillin", Reaction = "Urticaria", Severity = AllergySeverity.Severe },
            new Allergy { Patient = patients[4], Substance = "Aspirin", Reaction = "Bronchospasm", Severity = AllergySeverity.Severe },
            new Allergy { Patient = patients[2], Substance = "Dust mites", Reaction = "Rhinitis", Severity = AllergySeverity.Mild });

        dbContext.StockBatches.AddRange(
            new StockBatch { Medication = medications[0], BatchNumber = "AMX-2026-01", ExpiryDate = DateTime.UtcNow.Date.AddDays(120), QuantityOnHand = 400, Supplier = "Synthetic wholesaler" },
            new StockBatch { Medication = medications[1], BatchNumber = "PCM-2026-01", ExpiryDate = DateTime.UtcNow.Date.AddDays(400), QuantityOnHand = 1200, Supplier = "Synthetic wholesaler" },
            new StockBatch { Medication = medications[2], BatchNumber = "MET-2026-01", ExpiryDate = DateTime.UtcNow.Date.AddDays(60), QuantityOnHand = 90, Supplier = "Synthetic wholesaler" },
            new StockBatch { Medication = medications[3], BatchNumber = "ATV-2026-01", ExpiryDate = DateTime.UtcNow.Date.AddDays(300), QuantityOnHand = 150, Supplier = "Synthetic wholesaler" },
            new StockBatch { Medication = medications[4], BatchNumber = "SAL-2026-01", ExpiryDate = DateTime.UtcNow.Date.AddDays(45), QuantityOnHand = 12, Supplier = "Synthetic wholesaler" },
            new StockBatch { Medication = medications[5], BatchNumber = "OMP-2026-01", ExpiryDate = DateTime.UtcNow.Date.AddDays(500), QuantityOnHand = 300, Supplier = "Synthetic wholesaler" });

        dbContext.InsurancePolicies.Add(new InsurancePolicy
        {
            Patient = patients[0],
            ProviderName = "Synthetic Health Assurance",
            PolicyNumber = "SYN-0001",
            CoveragePercent = 80,
            MemberNumber = "M-0001",
            IsActive = true
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        // ---------------------------------------------------------------- activity
        var today = DateTime.UtcNow.Date;
        var random = new Random(20261122);
        var encounterCount = 0;

        for (var dayOffset = 13; dayOffset >= 0; dayOffset--)
        {
            var day = today.AddDays(-dayOffset);
            if (day.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday)
            {
                continue;
            }

            var visits = dayOffset == 0 ? 4 : random.Next(3, 6);
            for (var visit = 0; visit < visits; visit++)
            {
                var patient = patients[random.Next(patients.Count)];
                var doctor = doctors[random.Next(doctors.Count)];
                var slot = 540 + visit * 40;
                if (slot >= 780)
                {
                    break;
                }

                encounterCount++;
                var isToday = dayOffset == 0;
                var status = isToday && visit > 1
                    ? EncounterStatus.InConsultation
                    : isToday ? EncounterStatus.Triaged : EncounterStatus.Signed;

                var appointment = new Appointment
                {
                    Number = $"APT-{encounterCount:000000}",
                    AppointmentDate = day,
                    SlotMinutes = slot,
                    DurationMinutes = doctor.SlotMinutes,
                    Status = dayOffset switch
                    {
                        0 when visit == 0 => AppointStatus.Completed,
                        0 when visit == 1 => AppointStatus.CheckedIn,
                        0 => AppointStatus.Approved,
                        _ => status == EncounterStatus.Signed ? AppointStatus.Completed : AppointStatus.Approved
                    },
                    Patient = patient,
                    Doctor = doctor,
                    Notes = "Synthetic demonstration visit",
                    CreatedAtUtc = day.AddDays(-2)
                };
                dbContext.Appointments.Add(appointment);

                var encounter = new Encounter
                {
                    Number = $"ENC-{encounterCount:000000}",
                    Patient = patient,
                    Doctor = doctor,
                    Department = doctor.Department,
                    Type = EncounterType.Outpatient,
                    Status = status,
                    ChiefComplaint = visit % 3 == 0 ? "Chest discomfort on exertion" : visit % 3 == 1 ? "Persistent dry cough" : "Follow-up review",
                    RegisteredAtUtc = day.AddHours(9).AddMinutes(visit * 40),
                    TriagedAtUtc = day.AddHours(9).AddMinutes(visit * 40 + 5),
                    StartedAtUtc = day.AddHours(9).AddMinutes(visit * 40 + 10),
                    CompletedAtUtc = status is EncounterStatus.Completed or EncounterStatus.Signed ? day.AddHours(9).AddMinutes(visit * 40 + 35) : null,
                    SignedAtUtc = status == EncounterStatus.Signed ? day.AddHours(10).AddMinutes(visit * 40 + 30) : null,
                    SignedByUserId = status == EncounterStatus.Signed ? doctor.Id : null
                };

                encounter.Vitals.Add(new VitalSign
                {
                    RecordedAtUtc = encounter.RegisteredAtUtc.AddMinutes(6),
                    RecordedByUserId = staff[2].User.Id,
                    TemperatureC = 36.4m + (decimal)random.NextDouble() * 1.6m,
                    SystolicBp = 110 + random.Next(0, 40),
                    DiastolicBp = 70 + random.Next(0, 20),
                    PulseBpm = 62 + random.Next(0, 35),
                    RespiratoryRate = 14 + random.Next(0, 6),
                    OxygenSaturation = 95 + (decimal)random.NextDouble() * 4,
                    HeightCm = 160 + random.Next(0, 25),
                    WeightKg = 55 + random.Next(0, 45),
                    PainScore = random.Next(0, 5)
                });

                encounter.Diagnoses.Add(new Diagnosis
                {
                    Code = visit % 3 == 0 ? "I20.9" : visit % 3 == 1 ? "J06.9" : "Z00.0",
                    Description = visit % 3 == 0 ? "Angina pectoris, unspecified" : visit % 3 == 1 ? "Acute upper respiratory infection" : "General medical examination",
                    Type = DiagnosisType.Primary,
                    RecordedByUserId = doctor.Id,
                    RecordedAtUtc = encounter.RegisteredAtUtc.AddMinutes(25)
                });

                encounter.Notes.Add(new ClinicalNote
                {
                    Content = "Synthetic consultation note. Patient reviewed; vitals stable; no acute distress. Plan discussed with patient and safety advice given.",
                    AuthorUserId = doctor.Id,
                    AuthorRole = Role.Doctor.ToString(),
                    Status = status == EncounterStatus.Signed ? NoteStatus.Signed : NoteStatus.Draft,
                    CreatedAtUtc = encounter.RegisteredAtUtc.AddMinutes(30),
                    SignedAtUtc = status == EncounterStatus.Signed ? day.AddHours(10).AddMinutes(visit * 40 + 30) : null,
                    SignedByUserId = status == EncounterStatus.Signed ? doctor.Id : null
                });

                dbContext.Encounters.Add(encounter);

                if (status is EncounterStatus.Signed or EncounterStatus.Completed)
                {
                    var test = labTests[random.Next(labTests.Count)];
                    var orderStatus = dayOffset > 3 ? LabOrderStatus.Released : LabOrderStatus.Processing;
                    var numeric = test.NormalLow.HasValue && test.NormalHigh.HasValue
                        ? Math.Round((test.NormalLow.Value + test.NormalHigh.Value) / 2, 1)
                        : (decimal?)null;

                    dbContext.LabOrders.Add(new LabOrder
                    {
                        Number = $"LAB-{encounterCount:000000}",
                        Patient = patient,
                        Encounter = encounter,
                        OrderedByUserId = doctor.Id,
                        ClinicalNotes = "Synthetic laboratory order",
                        Status = orderStatus,
                        SpecimenId = $"SPC-{encounterCount:000000}",
                        CollectedAtUtc = encounter.RegisteredAtUtc.AddMinutes(15),
                        ReceivedAtUtc = encounter.RegisteredAtUtc.AddMinutes(25),
                        ReleasedAtUtc = orderStatus == LabOrderStatus.Released ? encounter.RegisteredAtUtc.AddHours(3) : null,
                        ReleasedByUserId = orderStatus == LabOrderStatus.Released ? staff[3].User.Id : null,
                        CreatedAtUtc = encounter.RegisteredAtUtc.AddMinutes(12),
                        Items =
                        [
                            new LabOrderItem
                            {
                                LabTest = test,
                                ResultValue = numeric?.ToString("0.0") ?? "Negative",
                                NumericResult = numeric,
                                ResultPrice = test.Price,
                                Flag = DAL.Entities.Diagnostics.ResultFlag.Normal,
                                EnteredByUserId = staff[3].User.Id,
                                EnteredAtUtc = encounter.RegisteredAtUtc.AddHours(2),
                                VerifiedByUserId = orderStatus == LabOrderStatus.Released ? staff[3].User.Id : null,
                                VerifiedAtUtc = orderStatus == LabOrderStatus.Released ? encounter.RegisteredAtUtc.AddHours(3) : null
                            }
                        ]
                    });

                    if (visit == 0 && dayOffset < 10)
                    {
                        dbContext.Prescriptions.Add(new Prescription
                        {
                            Number = $"RX-{encounterCount:000000}",
                            Patient = patient,
                            Encounter = encounter,
                            PrescribedByUserId = doctor.Id,
                            Status = dayOffset > 5 ? PrescriptionStatus.Dispensed : PrescriptionStatus.Reviewed,
                            CreatedAtUtc = encounter.RegisteredAtUtc.AddMinutes(32),
                            ReviewedByUserId = staff[4].User.Id,
                            ReviewedAtUtc = encounter.RegisteredAtUtc.AddMinutes(50),
                            Items =
                            [
                                new PrescriptionItem
                                {
                                    Medication = medications[random.Next(medications.Count)],
                                    Dose = "1 tablet",
                                    Frequency = "Twice daily",
                                    Duration = "5 days",
                                    Route = "Oral",
                                    Quantity = 10,
                                    DispensedQuantity = dayOffset > 5 ? 10 : 0
                                }
                            ]
                        });
                    }

                    dbContext.Invoices.Add(new Invoice
                    {
                        Number = $"INV-{encounterCount:000000}",
                        Patient = patient,
                        Encounter = encounter,
                        Status = InvoiceStatus.Issued,
                        IssuedAtUtc = encounter.RegisteredAtUtc.AddMinutes(40),
                        InsuranceCoveredAmount = patient == patients[0] ? 48 : 0,
                        Lines =
                        [
                            new InvoiceLine { Description = "Consultation", Quantity = 1, UnitPrice = 60, ServiceItem = serviceItems[0] },
                            new InvoiceLine { Description = test.Name, Quantity = 1, UnitPrice = test.Price, ServiceItem = serviceItems[2] }
                        ]
                    });
                }
            }
        }

        var admissionPatient = patients[3];
        var admission = new Admission
        {
            Number = "ADM-000001",
            Patient = admissionPatient,
            AdmittingDoctor = doctors[2],
            Status = AdmissionStatus.Admitted,
            Reason = "Synthetic observation admission for dehydration",
            RequestedAtUtc = DateTime.UtcNow.AddDays(-1),
            AdmittedAtUtc = DateTime.UtcNow.AddDays(-1).AddHours(3)
        };
        admission.BedAssignments.Add(new BedAssignment { Bed = room1.Beds[0], AssignedAtUtc = DateTime.UtcNow.AddDays(-1).AddHours(3) });
        dbContext.Admissions.Add(admission);

        dbContext.Admissions.Add(new Admission
        {
            Number = "ADM-000002",
            Patient = patients[4],
            AdmittingDoctor = doctors[0],
            Status = AdmissionStatus.Requested,
            Reason = "Synthetic chest pain work-up",
            RequestedAtUtc = DateTime.UtcNow.AddHours(-3)
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return $"Seeded synthetic dataset: {patients.Count} patients, {doctors.Count} clinicians, {encounterCount} encounters. Demonstration password for every demo account: {DemoPassword}";
    }

    private async Task EnsureManaraAccountsAsync(CancellationToken cancellationToken)
    {
        var branch = await dbContext.Branches.FirstOrDefaultAsync(cancellationToken);
        if (branch == null)
        {
            branch = new Branch { Name = "Main Medical Center", Code = "MANARA-01", City = "Alexandria", Phone = "+20 3 000 0000" };
            dbContext.Branches.Add(branch);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // 1. Ensure all 9 Active Clinical & Diagnostic Departments (zero duplicates / zero empty departments)
        var deptDefs = new (string Name, string Code, string Floor, string Desc, string Caps)[]
        {
            ("Cardiology", "CARD", "Main Pavilion · Floor 3",
             "Comprehensive heart, vascular, and rhythm diagnostics with non-invasive stress testing and preventive cardiac rehabilitation.",
             "12-Lead ECG · Echocardiography · Holter Monitoring · Lipid Risk Clinic"),
            ("Internal Medicine", "IM", "Main Pavilion · Floor 2",
             "Holistic adult primary and chronic disease care covering diabetes, metabolic disorders, thyroid balance, and executive wellness.",
             "Metabolic Panels · Endocrinology · Hypertension Care · Preventive Screening"),
            ("Orthopedics", "ORTH", "Surgical Wing · Floor 1",
             "Specialized joint preservation, sports injury recovery, spine stabilization, and minimally invasive arthroscopic procedures.",
             "Sports Rehabilitation · Arthroscopy · Fracture Clinic · Spine Consults"),
            ("Pediatrics", "PED", "Family Pavilion · Floor 1",
             "Gentle developmental care, neonatal follow-up, childhood immunizations, and acute pediatric consultations in a calm sanctuary.",
             "Well-Child Visits · Growth Tracking · Adolescent Care · Immunizations"),
            ("Radiology & Imaging", "RAD", "Diagnostic Center · Ground",
             "High-resolution 3T MRI, low-dose multi-slice CT, digital radiography, and consultant-verified PACS diagnostic reporting.",
             "3T MRI · Multi-Slice CT · Digital X-Ray · Doppler Ultrasound"),
            ("Clinical Laboratory", "LAB", "Diagnostic Center · Ground",
             "Automated hematology, clinical chemistry, endocrinology assays, and a strict two-stage clinical verification safety gate.",
             "Hematology · HbA1c & Lipids · Microbiology · Dual-Verified Results"),
            ("Neurology", "NEUR", "Neuroscience Suite · Floor 4",
             "Precision evaluation of migraine, epilepsy, peripheral neuropathy, memory disorders, and clinical neurophysiology.",
             "EEG Monitoring · Nerve Conduction · Migraine Clinic · Stroke Follow-Up"),
            ("Dermatology", "DERM", "Aesthetic & Clinical Wing · Floor 2",
             "Evidence-based medical dermatology, dermoscopy skin screening, allergic dermatitis treatment, and minor dermatologic surgery.",
             "Digital Dermoscopy · Allergy Patching · Eczema Care · Minor Surgery"),
            ("Pharmacy", "PHM", "Main Pavilion · Ground",
             "Clinical pharmacotherapy consultations, anticoagulation dosing stewardship, pharmacogenomics, and verified inpatient/outpatient dispensing.",
             "Pharmacogenomics · Anticoagulation Clinic · Polypharmacy Review · Therapeutics")
        };

        var depts = new Dictionary<string, Department>(StringComparer.OrdinalIgnoreCase);
        foreach (var def in deptDefs)
        {
            var dept = await dbContext.Departments.FirstOrDefaultAsync(d => d.Code == def.Code || d.Name == def.Name, cancellationToken);
            if (dept == null)
            {
                dept = new Department
                {
                    Name = def.Name,
                    Code = def.Code,
                    Branch = branch,
                    FloorLocation = def.Floor,
                    Description = def.Desc,
                    Capabilities = def.Caps,
                    IsActive = true
                };
                dbContext.Departments.Add(dept);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                dept.Name = def.Name;
                dept.FloorLocation = def.Floor;
                dept.Description = def.Desc;
                dept.Capabilities = def.Caps;
                dept.IsActive = true;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            depts[def.Name] = dept;
        }

        // Consolidate legacy duplicate "Paediatrics" (PAED) into canonical "Pediatrics" (PED)
        if (depts.TryGetValue("Pediatrics", out var canonicalPed))
        {
            var dupPaeds = await dbContext.Departments
                .Where(d => (d.Code == "PAED" || d.Name == "Paediatrics") && d.Id != canonicalPed.Id)
                .ToListAsync(cancellationToken);
            foreach (var dup in dupPaeds)
            {
                await dbContext.Doctors.Where(d => d.DepartmentId == dup.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.DepartmentId, canonicalPed.Id), cancellationToken);
                await dbContext.Encounters.Where(e => e.DepartmentId == dup.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.DepartmentId, canonicalPed.Id), cancellationToken);
                await dbContext.ServiceItems.Where(si => si.DepartmentId == dup.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.DepartmentId, canonicalPed.Id), cancellationToken);
                await dbContext.Wards.Where(w => w.DepartmentId == dup.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.DepartmentId, canonicalPed.Id), cancellationToken);
                await dbContext.ConsultationInquiries.Where(c => c.DepartmentId == dup.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.DepartmentId, canonicalPed.Id), cancellationToken);

                dbContext.Departments.Remove(dup);
            }
            if (dupPaeds.Count > 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        // 2. Ensure all 8 Specializations
        var specs = new Dictionary<string, Specialization>(StringComparer.OrdinalIgnoreCase);
        foreach (var def in deptDefs)
        {
            var spec = await dbContext.Specializations.FirstOrDefaultAsync(s => s.Name == def.Name, cancellationToken);
            if (spec == null)
            {
                spec = await dbContext.Specializations.FirstOrDefaultAsync(s => s.Name.StartsWith(def.Name.Substring(0, 4)), cancellationToken);
                if (spec == null)
                {
                    spec = new Specialization { Name = def.Name };
                    dbContext.Specializations.Add(spec);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }
            specs[def.Name] = spec;
        }

        // 3. Ensure Shifts
        var morningShift = await dbContext.Shifts.FirstOrDefaultAsync(s => s.ShiftType == ShiftType.Morning, cancellationToken);
        if (morningShift == null)
        {
            morningShift = new Shift { ShiftType = ShiftType.Morning, StartTIme = new TimeSpan(9, 0, 0), EndTIme = new TimeSpan(14, 0, 0) };
            dbContext.Shifts.Add(morningShift);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var afternoonShift = await dbContext.Shifts.FirstOrDefaultAsync(s => s.ShiftType == ShiftType.Afternoon, cancellationToken);
        if (afternoonShift == null)
        {
            afternoonShift = new Shift { ShiftType = ShiftType.Afternoon, StartTIme = new TimeSpan(14, 0, 0), EndTIme = new TimeSpan(18, 0, 0) };
            dbContext.Shifts.Add(afternoonShift);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // 4. Ensure Core Staff & Patient Accounts using clean firstname.role@gmail.com convention
        async Task<ApplicationUser?> EnsureUserAsync(string email, string password, string first, string last, string gender, string[] roles)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = first,
                    LastName = last,
                    Gender = gender,
                    CreatedAt = DateTime.UtcNow
                };
                var res = await userManager.CreateAsync(user, password);
                if (res.Succeeded)
                {
                    foreach (var role in roles)
                    {
                        await userManager.AddToRoleAsync(user, role);
                    }
                }
            }
            else
            {
                // Sync name/gender/confirmation and clear any lockout for demo accounts
                bool needsUpdate = false;
                if (user.FirstName != first) { user.FirstName = first; needsUpdate = true; }
                if (user.LastName != last) { user.LastName = last; needsUpdate = true; }
                if (string.IsNullOrWhiteSpace(user.Gender)) { user.Gender = gender; needsUpdate = true; }
                if (!user.EmailConfirmed) { user.EmailConfirmed = true; needsUpdate = true; }
                if (user.LockoutEnd is not null || user.AccessFailedCount > 0)
                {
                    user.LockoutEnd = null;
                    user.AccessFailedCount = 0;
                    needsUpdate = true;
                }
                if (needsUpdate) await userManager.UpdateAsync(user);

                // Reset demo password to the unified DemoPassword so all accounts share one known credential
                var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
                await userManager.ResetPasswordAsync(user, resetToken, password);

                // Ensure roles are still assigned
                var existingRoles = await userManager.GetRolesAsync(user);
                foreach (var role in roles)
                {
                    if (!existingRoles.Contains(role))
                        await userManager.AddToRoleAsync(user, role);
                }
            }
            return user;
        }

        // Purge any legacy generic "admin" accounts cleanly so only named accounts exist
        var legacyAdminEmails = new[]
        {
            "admin@manara.hospital",
            "admin@manara.internal",
            "admin@example.invalid",
            "omar.admin@gmail.com"
        };
        var legacyAdminUsers = await dbContext.Users
            .Where(u => (u.Email != null && legacyAdminEmails.Contains(u.Email)) || u.UserName == "admin")
            .ToListAsync(cancellationToken);
        foreach (var legacyUser in legacyAdminUsers)
        {
            var legacyId = legacyUser.Id;
            var currentRoles = await userManager.GetRolesAsync(legacyUser);
            if (currentRoles.Count > 0)
            {
                await userManager.RemoveFromRolesAsync(legacyUser, currentRoles);
            }

            try
            {
                var deleted = await userManager.DeleteAsync(legacyUser);
                if (deleted.Succeeded) continue;
            }
            catch
            {
                // Historical foreign keys reference this user ID; retire cleanly
            }

            dbContext.ChangeTracker.Clear();
            var trackedLegacy = await userManager.FindByIdAsync(legacyId);
            if (trackedLegacy is not null)
            {
                trackedLegacy.EmailConfirmed = false;
                trackedLegacy.LockoutEnabled = true;
                trackedLegacy.LockoutEnd = DateTimeOffset.MaxValue;
                trackedLegacy.FirstName = "Retired";
                trackedLegacy.LastName = "Account";
                trackedLegacy.UserName = $"retired.{trackedLegacy.Id[..Math.Min(8, trackedLegacy.Id.Length)]}";
                trackedLegacy.Email = $"{trackedLegacy.UserName}@retired.local";
                await userManager.UpdateAsync(trackedLegacy);
            }
        }

        await EnsureUserAsync("youssef.manager@gmail.com", DemoPassword, "Youssef", "Ahmed El-Sayed", "Male", [Role.Admin.ToString(), Role.HospitalAdmin.ToString()]);
        await EnsureUserAsync("nourhan.receptionist@gmail.com", DemoPassword, "Nourhan", "Adel", "Female", [Role.Receptionist.ToString()]);
        await EnsureUserAsync("hossam.labtech@gmail.com", DemoPassword, "Hossam", "Gamal", "Male", [Role.LabTechnician.ToString()]);
        await EnsureUserAsync("ramez.radiologist@gmail.com", DemoPassword, "Ramez", "Mansour", "Male", [Role.Radiologist.ToString()]);
        await EnsureUserAsync("mona.pharmacist@gmail.com", DemoPassword, "Mona", "El-Gamal", "Female", [Role.Pharmacist.ToString()]);
        await EnsureUserAsync("sherif.billing@gmail.com", DemoPassword, "Sherif", "Kamel", "Male", [Role.BillingOfficer.ToString()]);

        // Primary Patient with seeded recovery & care plan
        var ahmedPatient = await userManager.FindByEmailAsync("ahmed.patient@gmail.com") as Patient;
        if (ahmedPatient == null)
        {
            ahmedPatient = new Patient
            {
                UserName = "ahmed.patient@gmail.com",
                Email = "ahmed.patient@gmail.com",
                EmailConfirmed = true,
                FirstName = "Ahmed",
                LastName = "Hassan",
                Gender = "Male",
                DateOfBirth = new DateTime(1985, 6, 18, 0, 0, 0, DateTimeKind.Utc),
                PhoneNumber = "+20 100 555 0142",
                Address = "14 Cornish Boulevard, Alexandria",
                EmergencyContact = "Heba Hassan (+20 100 555 0198)",
                BloodGroup = "O+",
                Mrn = "MRN-10042",
                RecoveryProgress = 82,
                ActiveCondition = "Essential Hypertension & Lipid Management",
                DoctorCareNotes = "Blood pressure is trending steadily within target range. Continue morning medication with breakfast, limit sodium intake, walk 30 minutes daily, and return in 14 days for follow-up ECG.",
                NextFollowUpDate = DateTime.UtcNow.Date.AddDays(14),
                IsSynthetic = true,
                CreatedAt = DateTime.UtcNow
            };
            if ((await userManager.CreateAsync(ahmedPatient, "Patient@2026")).Succeeded)
            {
                await userManager.AddToRoleAsync(ahmedPatient, Role.Patient.ToString());
            }
        }
        else
        {
            ahmedPatient.RecoveryProgress = ahmedPatient.RecoveryProgress == 0 ? 82 : ahmedPatient.RecoveryProgress;
            ahmedPatient.ActiveCondition ??= "Essential Hypertension & Lipid Management";
            ahmedPatient.DoctorCareNotes ??= "Blood pressure is trending steadily within target range. Continue morning medication with breakfast, limit sodium intake, walk 30 minutes daily, and return in 14 days for follow-up ECG.";
            ahmedPatient.NextFollowUpDate ??= DateTime.UtcNow.Date.AddDays(14);
            await userManager.UpdateAsync(ahmedPatient);
        }

        var nadiaPatient = await userManager.FindByEmailAsync("nadia.patient@gmail.com") as Patient;
        if (nadiaPatient == null)
        {
            nadiaPatient = new Patient
            {
                UserName = "nadia.patient@gmail.com",
                Email = "nadia.patient@gmail.com",
                EmailConfirmed = true,
                FirstName = "Nadia",
                LastName = "Mahmoud",
                Gender = "Female",
                DateOfBirth = new DateTime(1992, 9, 10, 0, 0, 0, DateTimeKind.Utc),
                PhoneNumber = "+20 101 444 0210",
                Address = "28 Zamalek Gardens, Cairo",
                BloodGroup = "A+",
                Mrn = "MRN-10089",
                RecoveryProgress = 68,
                ActiveCondition = "Seasonal Bronchial Sensitivity",
                DoctorCareNotes = "Airway inflammation improving. Complete prescribed course and keep evening inhaler available if dry cough persists.",
                NextFollowUpDate = DateTime.UtcNow.Date.AddDays(10),
                IsSynthetic = true,
                CreatedAt = DateTime.UtcNow
            };
            if ((await userManager.CreateAsync(nadiaPatient, "Patient@2026")).Succeeded)
            {
                await userManager.AddToRoleAsync(nadiaPatient, Role.Patient.ToString());
            }
        }

        // Deactivate legacy non-portrait doctors so public showcase reflects only the 16 unique MANARA specialists
        var legacyDoctors = await dbContext.Doctors
            .Where(d => d.Email != null && (d.Email.EndsWith("@example.invalid") || d.Email.EndsWith("@manara.internal") || d.Email.EndsWith("@manara.hospital")))
            .ToListAsync(cancellationToken);
        foreach (var leg in legacyDoctors)
        {
            leg.IsActive = false;
        }
        if (legacyDoctors.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // 5. Seed 45 Unique-Portrait Specialist Doctors across all 9 Departments (5 Doctors per Department)
        var doctorCatalog = new (
            string Email, string First, string Last, string Gender, string DeptName,
            string Title, string Credentials, string Room, decimal Fee, decimal FollowUpFee,
            int ExpYears, decimal Rating, int Reviews, bool TopRated, string Bio, string Img, string Seal, DayOfWeek[] Days)[]
        {
            // ── Cardiology (5) ──
            ("salma.cardiologist@gmail.com", "Salma", "El-Sayed", "Female", "Cardiology",
             "Senior Consultant Cardiologist", "MD, FESC", "Clinic 301", 600, 200, 14, 4.95m, 186, true,
             "Fellow of the European Society of Cardiology specializing in echocardiography, preventive heart care, and women's cardiovascular wellness.",
             "doc-salma.jpg", "CARDIAC EXCELLENCE • MANARA", [DayOfWeek.Sunday, DayOfWeek.Tuesday, DayOfWeek.Thursday]),
            ("hisham.cardiologist@gmail.com", "Hisham", "El-Masry", "Male", "Cardiology",
             "Consultant Interventional Cardiologist", "MD, FACC", "Clinic 302", 700, 220, 16, 4.85m, 119, false,
             "Specialist in coronary risk stratification, complex hypertension therapy, and non-invasive cardiac diagnostics.",
             "doc-hisham.jpg", "HEART INSTITUTE • MANARA", [DayOfWeek.Monday, DayOfWeek.Wednesday]),
            ("dina.cardiologist@gmail.com", "Dina", "El-Sharif", "Female", "Cardiology",
             "Preventive Lipid & Hypertension Consultant", "MD, FESC", "Clinic 303", 550, 180, 11, 4.88m, 94, false,
             "Focuses on familial dyslipidemia, resistant blood pressure optimization, and longitudinal preventive cardiology.",
             "doc-dina.jpg", "PREVENTIVE CARDIO • MANARA", [DayOfWeek.Sunday, DayOfWeek.Wednesday]),
            ("omar.khaled.cardiologist@gmail.com", "Omar", "Khaled", "Male", "Cardiology",
             "Consultant Cardiac Electrophysiologist", "MD, PhD", "Clinic 304", 720, 240, 15, 4.91m, 128, false,
             "Arrhythmia specialist leading Holter telemetry evaluation, atrial fibrillation care, and syncope diagnostics.",
             "doc-omar-k.jpg", "RHYTHM CARE • MANARA", [DayOfWeek.Monday, DayOfWeek.Thursday]),
            ("helena.cardiologist@gmail.com", "Helena", "Kostas", "Female", "Cardiology",
             "Consultant Cardiac Imaging Specialist", "MD, EACVI", "Clinic 305", 640, 210, 13, 4.89m, 81, false,
             "Directs 3D transthoracic stress echocardiography and congenital adult valvular follow-up.",
             "doc-helena.jpg", "CARDIAC IMAGING • MANARA", [DayOfWeek.Tuesday, DayOfWeek.Thursday]),

            // ── Internal Medicine (5) ──
            ("karim.internist@gmail.com", "Karim", "Mansour", "Male", "Internal Medicine",
             "Head of Internal Medicine", "MD, MRCP", "Clinic 204", 450, 150, 18, 4.88m, 142, false,
             "Royal College of Physicians fellow leading comprehensive metabolic screening, diabetes care, and adult chronic disease programs.",
             "doc-karim.jpg", "INTERNAL MEDICINE • MANARA", [DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Wednesday]),
            ("mariam.internist@gmail.com", "Mariam", "Soliman", "Female", "Internal Medicine",
             "Consultant Endocrinologist", "MD, PhD", "Clinic 205", 500, 160, 12, 4.84m, 97, false,
             "Dedicated to thyroid disorders, hormonal longevity, insulin resistance, and personalized executive health plans.",
             "doc-mariam.jpg", "METABOLIC CARE • MANARA", [DayOfWeek.Tuesday, DayOfWeek.Thursday]),
            ("bassem.internist@gmail.com", "Bassem", "Tawfik", "Male", "Internal Medicine",
             "Consultant Hepatology & GI Internist", "MD, MRCP", "Clinic 206", 480, 160, 14, 4.86m, 108, false,
             "Guides non-invasive hepatic care, digestive inflammatory management, and structured metabolic reversal.",
             "doc-bassem.jpg", "HEPATIC CARE • MANARA", [DayOfWeek.Sunday, DayOfWeek.Wednesday]),
            ("rana.internist@gmail.com", "Rana", "El-Gohary", "Female", "Internal Medicine",
             "Infectious Disease & Immunity Consultant", "MD, MSc", "Clinic 207", 440, 140, 9, 4.83m, 72, false,
             "Specializes in autoimmune screening, post-viral fatigue recovery, and adult travel immunizations.",
             "doc-rana.jpg", "IMMUNE HEALTH • MANARA", [DayOfWeek.Monday, DayOfWeek.Thursday]),
            ("sameh.internist@gmail.com", "Sameh", "Mikhail", "Male", "Internal Medicine",
             "Senior Geriatrics & Multimorbidity Physician", "MD, FRCP", "Clinic 209", 520, 170, 24, 4.92m, 154, false,
             "Senior internist coordinating integrative senior wellness, renal protection, and medication harmonization.",
             "doc-sameh.jpg", "SENIOR WELLNESS • MANARA", [DayOfWeek.Sunday, DayOfWeek.Tuesday]),

            // ── Orthopedics (5) ──
            ("tarek.orthopedist@gmail.com", "Tarek", "Nabil", "Male", "Orthopedics",
             "Consultant Orthopedic Surgeon", "MD, FRCS", "Clinic 112", 550, 180, 19, 4.92m, 164, true,
             "Senior orthopedic reconstructive specialist focused on minimally invasive joint arthroscopy and spine preservation.",
             "doc-tarek.jpg", "JOINT & SPINE • MANARA", [DayOfWeek.Sunday, DayOfWeek.Tuesday]),
            ("sherif.orthopedist@gmail.com", "Sherif", "El-Shazly", "Male", "Orthopedics",
             "Sports Injury & Joint Specialist", "MD, FAAOS", "Clinic 114", 600, 200, 13, 4.78m, 88, false,
             "Sports medicine consultant guiding athletes and active adults through ligament recovery and precision biomechanics.",
             "doc-sherif.jpg", "SPORTS ORTHOPEDICS • MANARA", [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Thursday]),
            ("ashraf.orthopedist@gmail.com", "Ashraf", "Abdelmalak", "Male", "Orthopedics",
             "Senior Arthroplasty & Hip/Knee Consultant", "MD, FRCS", "Clinic 115", 620, 200, 21, 4.89m, 131, false,
             "Dedicated to robotic-assisted knee and hip joint preservation and structured post-operative mobility.",
             "doc-ashraf.jpg", "JOINT SURGERY • MANARA", [DayOfWeek.Sunday, DayOfWeek.Wednesday]),
            ("yara.orthopedist@gmail.com", "Yara", "Hegazy", "Female", "Orthopedics",
             "Hand, Wrist & Microsurgery Specialist", "MD, FEBOT", "Clinic 116", 520, 170, 10, 4.87m, 79, false,
             "European Board specialist treating tendon entrapment, wrist instability, and pediatric fracture recovery.",
             "doc-yara.jpg", "HAND & UPPER LIMB • MANARA", [DayOfWeek.Tuesday, DayOfWeek.Thursday]),
            ("ramy.orthopedist@gmail.com", "Ramy", "El-Banna", "Male", "Orthopedics",
             "Consultant Spine & Trauma Orthopedist", "MD, PhD", "Clinic 118", 580, 190, 15, 4.84m, 102, false,
             "Focuses on cervical and lumbar disc stabilization, scoliosis evaluation, and rapid trauma recovery.",
             "doc-ramy.jpg", "SPINE CARE • MANARA", [DayOfWeek.Monday, DayOfWeek.Thursday]),

            // ── Pediatrics (5) ──
            ("nadia.pediatrician@gmail.com", "Nadia", "Farouk", "Female", "Pediatrics",
             "Senior Specialist Pediatrician", "MD, MRCPCH", "Clinic 105", 400, 120, 11, 4.94m, 158, true,
             "Gentle family pediatrician focusing on early childhood milestones, respiratory wellness, and preventive immunization.",
             "doc-nadia.jpg", "CHILD WELLNESS • MANARA", [DayOfWeek.Sunday, DayOfWeek.Wednesday]),
            ("hana.pediatrician@gmail.com", "Hana", "Abdelrahman", "Female", "Pediatrics",
             "Consultant Adolescent Pediatrician", "MD, DCH", "Clinic 107", 380, 120, 9, 4.86m, 134, false,
             "Warm, attentive pediatric specialist dedicated to developmental nutrition, pediatric allergy management, and newborn follow-up.",
             "doc-hana.jpg", "PEDIATRIC SANCTUARY • MANARA", [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday]),
            ("noura.pediatrician@gmail.com", "Noura", "El-Wakeel", "Female", "Pediatrics",
             "Pediatric Allergy & Pulmonology Consultant", "MD, EAACI", "Clinic 108", 420, 130, 12, 4.90m, 114, false,
             "Guides childhood asthma control, food allergy testing, and serene seasonal respiratory care.",
             "doc-noura.jpg", "CHILD RESPIRATORY • MANARA", [DayOfWeek.Sunday, DayOfWeek.Tuesday]),
            ("hatem.pediatrician@gmail.com", "Hatem", "Shoukry", "Male", "Pediatrics",
             "Consultant Neonatologist & NICU Lead", "MD, PhD", "Clinic 109", 460, 150, 16, 4.88m, 96, false,
             "Specializes in premature infant developmental follow-up, feeding milestones, and early neonatal screening.",
             "doc-hatem.jpg", "NEONATAL CARE • MANARA", [DayOfWeek.Monday, DayOfWeek.Wednesday]),
            ("ines.pediatrician@gmail.com", "Ines", "Mansour", "Female", "Pediatrics",
             "Pediatric Neurodevelopment Specialist", "MD, MRCPCH", "Clinic 110", 430, 140, 10, 4.85m, 68, false,
             "Focuses on school-readiness neurodevelopment, sleep hygiene, and sensory-friendly family consultations.",
             "doc-ines.jpg", "DEVELOPMENTAL CARE • MANARA", [DayOfWeek.Tuesday, DayOfWeek.Thursday]),

            // ── Radiology & Imaging (5) ──
            ("amr.radiologist@gmail.com", "Amr", "Fouad", "Male", "Radiology & Imaging",
             "Consultant Diagnostic Radiologist", "MD, FRCR", "Imaging Suite 1", 450, 150, 17, 4.85m, 92, false,
             "Fellow of the Royal College of Radiologists overseeing 3T MRI interpretation, cardiac CT, and rapid PACS verification.",
             "doc-amr.jpg", "DIAGNOSTIC PACS • MANARA", [DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday]),
            ("farida.radiologist@gmail.com", "Farida", "El-Khatib", "Female", "Radiology & Imaging",
             "Consultant Musculoskeletal Radiologist", "MD, EDiR", "Imaging Suite 2", 480, 160, 11, 4.90m, 105, false,
             "European Diploma radiologist specializing in high-resolution joint MRI, breast imaging, and vascular Doppler ultrasound.",
             "doc-farida.jpg", "PRECISION IMAGING • MANARA", [DayOfWeek.Sunday, DayOfWeek.Wednesday, DayOfWeek.Thursday]),
            ("walid.radiologist@gmail.com", "Walid", "El-Sawy", "Male", "Radiology & Imaging",
             "Consultant Neuroradiologist", "MD, PhD", "Imaging Suite 3", 520, 170, 14, 4.87m, 83, false,
             "Specializes in 3T brain & spine MRI, functional neuro-tractography, and low-dose cranial angiography.",
             "doc-walid.jpg", "NEURO IMAGING • MANARA", [DayOfWeek.Sunday, DayOfWeek.Tuesday]),
            ("christine.radiologist@gmail.com", "Christine", "Dimitri", "Female", "Radiology & Imaging",
             "Interventional & Body Radiologist", "MD, EBIR", "Imaging Suite 4", 540, 180, 12, 4.89m, 77, false,
             "Performs ultrasound-guided biopsies, thyroid fine-needle diagnostics, and multi-slice abdominal CT correlation.",
             "doc-christine.jpg", "INTERVENTIONAL PACS • MANARA", [DayOfWeek.Monday, DayOfWeek.Wednesday]),
            ("ziad.radiologist@gmail.com", "Ziad", "El-Araby", "Male", "Radiology & Imaging",
             "Cardiac CT & Thoracic Radiologist", "MD, EDiR", "Imaging Suite 5", 490, 160, 9, 4.82m, 61, false,
             "Dedicated to coronary calcium scoring, high-resolution chest CT, and digital PACS quality review.",
             "doc-ziad.jpg", "CARDIOTHORACIC PACS • MANARA", [DayOfWeek.Tuesday, DayOfWeek.Thursday]),

            // ── Clinical Laboratory (5) ──
            ("laila.pathologist@gmail.com", "Laila", "Kamel", "Female", "Clinical Laboratory",
             "Chief Clinical Pathologist", "MD, FRCPath", "Lab Central", 350, 100, 15, 4.88m, 118, false,
             "Directs MANARA's two-step laboratory verification gate, hematology profiling, and molecular biomarker assurance.",
             "doc-laila-v2.jpg", "CLINICAL PATHOLOGY • MANARA", [DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday]),
            ("yasser.pathologist@gmail.com", "Yasser", "Ibrahim", "Male", "Clinical Laboratory",
             "Consultant Clinical Biochemist", "MD, PhD", "Lab Diagnostic 2", 360, 110, 10, 4.76m, 64, false,
             "Specializes in clinical chemistry panels, autoimmune assays, and precision metabolic diagnostics.",
             "doc-yasser.jpg", "DIAGNOSTIC LAB • MANARA", [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday]),
            ("maya.pathologist@gmail.com", "Maya", "Haddad", "Female", "Clinical Laboratory",
             "Consultant Hematopathologist", "MD, FRCPath", "Lab Diagnostic 3", 390, 120, 12, 4.86m, 73, false,
             "Oversees flow cytometry, coagulation cascades, anemia workups, and critical value escalation.",
             "doc-maya.jpg", "HEMATOPATHOLOGY • MANARA", [DayOfWeek.Sunday, DayOfWeek.Wednesday]),
            ("fady.pathologist@gmail.com", "Fady", "Youssef", "Male", "Clinical Laboratory",
             "Consultant Medical Microbiologist", "MD, MSc", "Lab Diagnostic 4", 370, 110, 11, 4.81m, 59, false,
             "Leads antimicrobial susceptibility panels, molecular PCR testing, and hospital infection-control surveillance.",
             "doc-fady.jpg", "MICROBIOLOGY LAB • MANARA", [DayOfWeek.Monday, DayOfWeek.Thursday]),
            ("samar.pathologist@gmail.com", "Samar", "Nossier", "Female", "Clinical Laboratory",
             "Clinical Endocrinology & Cytogenetics Lead", "MD, PhD", "Lab Diagnostic 5", 400, 130, 13, 4.90m, 84, false,
             "Specializes in hormone immunoassay calibration, prenatal biomarker screening, and genetic precision.",
             "doc-samar.jpg", "MOLECULAR LAB • MANARA", [DayOfWeek.Tuesday, DayOfWeek.Thursday]),

            // ── Neurology (5) ──
            ("sara.neurologist@gmail.com", "Sara", "El-Gamal", "Female", "Neurology",
             "Senior Consultant Neurologist", "MD, FEBN", "Clinic 401", 650, 220, 13, 4.93m, 151, true,
             "European Board neurologist specializing in migraine management, epilepsy care, and neuro-vascular recovery.",
             "doc-sara.jpg", "NEUROSCIENCE • MANARA", [DayOfWeek.Sunday, DayOfWeek.Tuesday]),
            ("mostafa.neurologist@gmail.com", "Mostafa", "Zidan", "Male", "Neurology",
             "Consultant Neurophysiologist", "MD, PhD", "Clinic 403", 750, 250, 21, 4.87m, 112, false,
             "Renowned authority in EEG diagnostics, nerve conduction studies, and movement disorder therapeutics.",
             "doc-mostafa.jpg", "NEUROPHYSIOLOGY • MANARA", [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Thursday]),
            ("tamer.neurologist@gmail.com", "Tamer", "El-Khouly", "Male", "Neurology",
             "Stroke & Cerebrovascular Consultant", "MD, FEBN", "Clinic 404", 680, 220, 16, 4.89m, 104, false,
             "Leads secondary stroke prevention, carotid Doppler integration, and post-ischemic cognitive rehabilitation.",
             "doc-tamer.jpg", "NEUROVASCULAR • MANARA", [DayOfWeek.Sunday, DayOfWeek.Wednesday]),
            ("aliaa.neurologist@gmail.com", "Aliaa", "Darwish", "Female", "Neurology",
             "Movement & Neuromuscular Neurologist", "MD, PhD", "Clinic 405", 620, 200, 14, 4.91m, 89, false,
             "Specialist in tremor modulation, peripheral neuropathy diagnostics, and EMG-guided therapy.",
             "doc-alia.jpg", "NEUROMUSCULAR • MANARA", [DayOfWeek.Monday, DayOfWeek.Thursday]),
            ("mazen.neurologist@gmail.com", "Mazen", "El-Refai", "Male", "Neurology",
             "Clinical Headache & Sleep Neurologist", "MD, MSc", "Clinic 406", 580, 190, 9, 4.84m, 67, false,
             "Focuses on refractory migraine biologics, cluster headaches, and polysomnography sleep disorders.",
             "doc-mazen.jpg", "HEADACHE CLINIC • MANARA", [DayOfWeek.Tuesday, DayOfWeek.Thursday]),

            // ── Dermatology (5) ──
            ("yasmin.dermatologist@gmail.com", "Yasmin", "Zaki", "Female", "Dermatology",
             "Consultant Clinical Dermatologist", "MD, FAAD", "Clinic 208", 500, 150, 10, 4.91m, 173, false,
             "Fellow of the American Academy of Dermatology focused on digital dermoscopy, skin barrier restoration, and allergic skin conditions.",
             "doc-yasmin.jpg", "DERM SANCTUARY • MANARA", [DayOfWeek.Sunday, DayOfWeek.Wednesday]),
            ("khaled.dermatologist@gmail.com", "Khaled", "Hassan", "Male", "Dermatology",
             "Consultant Dermatologic Surgeon", "MD, PhD", "Clinic 210", 550, 180, 14, 4.79m, 89, false,
             "Specialist in precision dermatologic procedures, laser therapeutics, and restorative skin medicine.",
             "doc-khaled.jpg", "DERM SURGERY • MANARA", [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday]),
            ("salma.naguib.dermatologist@gmail.com", "Salma", "Naguib", "Female", "Dermatology",
             "Pediatric & Restorative Dermatologist", "MD, FAAD", "Clinic 211", 490, 150, 9, 4.88m, 95, false,
             "Dedicated to gentle pediatric atopic dermatitis care, barrier therapy, and rosacea restoration.",
             "doc-salma-n.jpg", "SKIN BARRIER • MANARA", [DayOfWeek.Sunday, DayOfWeek.Tuesday]),
            ("fouad.dermatologist@gmail.com", "Fouad", "Abdelaziz", "Male", "Dermatology",
             "Immunodermatology & Trichology Consultant", "MD, PhD", "Clinic 212", 520, 170, 17, 4.85m, 107, false,
             "Specializes in autoimmune blistering disorders, trichoscopy alopecia diagnostics, and patch testing.",
             "doc-fouad.jpg", "IMMUNODERM • MANARA", [DayOfWeek.Monday, DayOfWeek.Thursday]),
            ("reem.dermatologist@gmail.com", "Reem", "El-Shenawy", "Female", "Dermatology",
             "Digital Dermoscopy & Pigment Specialist", "MD, MSc", "Clinic 214", 470, 140, 8, 4.87m, 82, false,
             "Uses polarized digital mole mapping for early pigment evaluation and sun-damage preservation.",
             "doc-reem.jpg", "DERMOSCOPY • MANARA", [DayOfWeek.Wednesday, DayOfWeek.Thursday]),

            // ── Pharmacy (5) ──
            ("lamia.pharmacist@gmail.com", "Lamia", "El-Gamal", "Female", "Pharmacy",
             "Chief Clinical Pharmacotherapy Specialist", "PharmD, BCPS", "Pharmacy Suite 1", 350, 100, 14, 4.92m, 121, true,
             "Board-certified pharmacotherapy specialist reviewing polypharmacy regimens, drug-drug interactions, and chronic therapy optimization.",
             "doc-mona-pharm.jpg", "PHARMACOTHERAPY • MANARA", [DayOfWeek.Sunday, DayOfWeek.Tuesday, DayOfWeek.Thursday]),
            ("hazem.pharmacist@gmail.com", "Hazem", "El-Kady", "Male", "Pharmacy",
             "Clinical Pharmacogenomics Specialist", "PharmD, PhD", "Pharmacy Suite 2", 380, 110, 11, 4.86m, 74, false,
             "Matches cardiac, neuropsychiatric, and pain therapy dosages to individual metabolic profiles for maximum safety.",
             "doc-hazem-pharm.jpg", "PHARMACOGENOMICS • MANARA", [DayOfWeek.Monday, DayOfWeek.Wednesday]),
            ("nesma.pharmacist@gmail.com", "Nesma", "Reda", "Female", "Pharmacy",
             "Anticoagulation & Oncology Pharmacist", "PharmD, BCOP", "Pharmacy Suite 3", 360, 110, 10, 4.89m, 88, false,
             "Directs warfarin/DOAC INR clinics and supportive medication counseling for outpatient patients.",
             "doc-nesma-pharm.jpg", "ANTICOAGULATION • MANARA", [DayOfWeek.Sunday, DayOfWeek.Wednesday]),
            ("adel.pharmacist@gmail.com", "Adel", "El-Gendy", "Male", "Pharmacy",
             "Antimicrobial Stewardship Pharmacist", "PharmD, BCIDP", "Pharmacy Suite 4", 350, 100, 13, 4.83m, 65, false,
             "Coordinates targeted antibiotic regimens with Clinical Laboratory susceptibility profiles to preserve efficacy.",
             "doc-adel-pharm.jpg", "STEWARDSHIP • MANARA", [DayOfWeek.Monday, DayOfWeek.Thursday]),
            ("hend.pharmacist@gmail.com", "Hend", "Mostafa", "Female", "Pharmacy",
             "Critical Care & Metabolic Pharmacist", "PharmD, BCCCP", "Pharmacy Suite 5", 370, 110, 12, 4.87m, 79, false,
             "Provides inpatient therapeutic drug monitoring, insulin titration education, and discharge medication counseling.",
             "doc-hend-pharm.jpg", "CLINICAL PHARMACY • MANARA", [DayOfWeek.Tuesday, DayOfWeek.Thursday])
        };

        var docIndex = 1;
        Doctor? firstCardiologist = null;
        foreach (var docDef in doctorCatalog)
        {
            var existingUser = await userManager.FindByEmailAsync(docDef.Email);
            Doctor? doc = existingUser as Doctor;
            var dept = depts[docDef.DeptName];
            var spec = specs[docDef.DeptName];

            if (doc == null && existingUser == null)
            {
                doc = new Doctor
                {
                    UserName = docDef.Email,
                    Email = docDef.Email,
                    EmailConfirmed = true,
                    FirstName = docDef.First,
                    LastName = docDef.Last,
                    Gender = docDef.Gender,
                    EmployeeNumber = $"MNR-DR-{docIndex:00}",
                    Salary = 24000 + docIndex * 500,
                    SessionPrice = docDef.Fee,
                    FollowUpFee = docDef.FollowUpFee,
                    ExperienceYears = docDef.ExpYears,
                    Rating = docDef.Rating,
                    ReviewCount = docDef.Reviews,
                    IsTopRated = docDef.TopRated,
                    Credentials = docDef.Credentials,
                    Title = docDef.Title,
                    Bio = docDef.Bio,
                    SealEmblem = docDef.Seal,
                    SlotMinutes = 30,
                    Specialization = spec,
                    Department = dept,
                    ClinicRoom = docDef.Room,
                    Image = docDef.Img,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var created = await userManager.CreateAsync(doc, "Doctor@2026");
                if (created.Succeeded)
                {
                    await userManager.AddToRoleAsync(doc, Role.Doctor.ToString());
                    if (docDef.DeptName == "Clinical Laboratory")
                    {
                        await userManager.AddToRoleAsync(doc, Role.LabTechnician.ToString());
                    }
                    if (docDef.DeptName == "Radiology & Imaging")
                    {
                        await userManager.AddToRoleAsync(doc, Role.Radiologist.ToString());
                    }
                    if (docDef.DeptName == "Pharmacy")
                    {
                        await userManager.AddToRoleAsync(doc, Role.Pharmacist.ToString());
                    }
                }
            }
            else if (doc != null)
            {
                doc.FirstName = docDef.First;
                doc.LastName = docDef.Last;
                doc.Gender = docDef.Gender;
                doc.Image = docDef.Img;
                doc.ClinicRoom = docDef.Room;
                doc.SessionPrice = docDef.Fee;
                doc.FollowUpFee = docDef.FollowUpFee;
                doc.ExperienceYears = docDef.ExpYears;
                doc.Rating = docDef.Rating;
                doc.ReviewCount = docDef.Reviews;
                doc.IsTopRated = docDef.TopRated;
                doc.Credentials = docDef.Credentials;
                doc.Title = docDef.Title;
                doc.Bio = docDef.Bio;
                doc.SealEmblem = docDef.Seal;
                doc.IsActive = true;
                doc.Department = dept;
                doc.Specialization = spec;
                await userManager.UpdateAsync(doc);
            }

            if (doc != null)
            {
                firstCardiologist ??= doc;
                foreach (var day in docDef.Days)
                {
                    var hasSched = await dbContext.Schedules.AnyAsync(s => s.DoctorId == doc.Id && s.Day == day, cancellationToken);
                    if (!hasSched)
                    {
                        dbContext.Schedules.Add(new Schedule
                        {
                            DoctorId = doc.Id,
                            ShiftId = morningShift.ShiftId,
                            Day = day,
                            Status = Status.Assigned
                        });
                    }
                }
            }

            docIndex++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // 6. Ensure Ahmed Hassan (ahmed.patient@gmail.com) has rich completed visit + Rx + care notes for My Care / Follow-Up!
        if (ahmedPatient != null && firstCardiologist != null)
        {
            var hasCompletedVisit = await dbContext.Appointments.AnyAsync(a => a.PatientID == ahmedPatient.Id && a.Status == AppointStatus.Completed, cancellationToken);
            if (!hasCompletedVisit)
            {
                var pastDate = DateTime.UtcNow.Date.AddDays(-5);
                var encNum = $"ENC-MNR-{Random.Shared.Next(100, 999)}";
                var encounter = new Encounter
                {
                    Number = encNum,
                    PatientId = ahmedPatient.Id,
                    DoctorId = firstCardiologist.Id,
                    DepartmentId = firstCardiologist.DepartmentId,
                    Type = EncounterType.Outpatient,
                    Status = EncounterStatus.Signed,
                    ChiefComplaint = "Routine blood pressure & cardiovascular follow-up evaluation",
                    RegisteredAtUtc = pastDate.AddHours(10),
                    TriagedAtUtc = pastDate.AddHours(10).AddMinutes(8),
                    StartedAtUtc = pastDate.AddHours(10).AddMinutes(15),
                    CompletedAtUtc = pastDate.AddHours(10).AddMinutes(45),
                    SignedAtUtc = pastDate.AddHours(11),
                    SignedByUserId = firstCardiologist.Id
                };
                encounter.Diagnoses.Add(new Diagnosis
                {
                    Code = "I10",
                    Description = "Essential (primary) hypertension — improving with therapy",
                    Type = DiagnosisType.Primary,
                    RecordedByUserId = firstCardiologist.Id,
                    RecordedAtUtc = pastDate.AddHours(10).AddMinutes(30)
                });
                encounter.Notes.Add(new ClinicalNote
                {
                    Content = "Resting BP 124/78 mmHg. Patient tolerating therapy well with zero dizziness or peripheral edema. Advised 30-minute daily walking and reduced dietary sodium. Scheduled follow-up in 14 days.",
                    AuthorUserId = firstCardiologist.Id,
                    AuthorRole = Role.Doctor.ToString(),
                    Status = NoteStatus.Signed,
                    CreatedAtUtc = pastDate.AddHours(10).AddMinutes(35),
                    SignedAtUtc = pastDate.AddHours(11),
                    SignedByUserId = firstCardiologist.Id
                });
                dbContext.Encounters.Add(encounter);
                await dbContext.SaveChangesAsync(cancellationToken);

                dbContext.Appointments.Add(new Appointment
                {
                    Number = $"APT-MNR-{Random.Shared.Next(100, 999)}",
                    AppointmentDate = pastDate,
                    SlotMinutes = 600,
                    DurationMinutes = 30,
                    Status = AppointStatus.Completed,
                    PatientID = ahmedPatient.Id,
                    DoctorID = firstCardiologist.Id,
                    EncounterId = encounter.Id,
                    Notes = "Completed Cardiology Consultation — Eligible for Follow-Up Fee (200 EGP)",
                    CreatedAtUtc = pastDate.AddDays(-2),
                    CompletedAtUtc = pastDate.AddHours(11)
                });

                var meds = await dbContext.Medications.Take(2).ToListAsync(cancellationToken);
                if (meds.Count > 0)
                {
                    var rx = new Prescription
                    {
                        Number = $"RX-MNR-{Random.Shared.Next(100, 999)}",
                        EncounterId = encounter.Id,
                        PatientId = ahmedPatient.Id,
                        PrescribedByUserId = firstCardiologist.Id,
                        Status = PrescriptionStatus.Reviewed,
                        DoctorInstructions = "Take medications consistently at the exact scheduled hours with food. Log home blood pressure twice weekly and bring your log to your follow-up visit.",
                        NextFollowUpDate = DateTime.UtcNow.Date.AddDays(14),
                        CreatedAtUtc = pastDate.AddHours(10).AddMinutes(40),
                        Items = meds.Select((m, idx) => new PrescriptionItem
                        {
                            MedicationId = m.Id,
                            Dose = idx == 0 ? "1 tablet (20 mg)" : "1 capsule (500 mg)",
                            Frequency = idx == 0 ? "Once daily" : "Twice daily",
                            Duration = "30 days",
                            Route = "Oral",
                            Quantity = idx == 0 ? 30 : 60,
                            Instructions = idx == 0 ? "Do not skip evening dose" : "Take with full glass of water after meal",
                            IntakeSchedule = idx == 0 ? "09:00 PM before sleep" : "08:00 AM after breakfast • 08:00 PM after dinner"
                        }).ToList()
                    };
                    dbContext.Prescriptions.Add(rx);
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        // 7. Ensure Nadia Mahmoud (nadia.patient@gmail.com) has full multi-modal updates, released lab, PACS radiology, Rx, consultations, and reviews
        if (nadiaPatient != null && firstCardiologist != null)
        {
            var labStaff = await userManager.FindByEmailAsync("hossam.labtech@gmail.com");
            var radDoc = await userManager.FindByEmailAsync("amr.radiologist@gmail.com") ?? firstCardiologist;

            var hasNadiaVisit = await dbContext.Appointments.AnyAsync(a => a.PatientID == nadiaPatient.Id, cancellationToken);
            Encounter? nadiaEncounter = await dbContext.Encounters.FirstOrDefaultAsync(e => e.PatientId == nadiaPatient.Id, cancellationToken);

            if (!hasNadiaVisit || nadiaEncounter is null)
            {
                var pastDate = DateTime.UtcNow.Date.AddDays(-3);
                nadiaEncounter = new Encounter
                {
                    Number = $"ENC-MNR-{Random.Shared.Next(400, 799)}",
                    PatientId = nadiaPatient.Id,
                    DoctorId = firstCardiologist.Id,
                    DepartmentId = firstCardiologist.DepartmentId,
                    Type = EncounterType.Outpatient,
                    Status = EncounterStatus.Signed,
                    ChiefComplaint = "Seasonal bronchial tightness & preventive cardiac clearance",
                    RegisteredAtUtc = pastDate.AddHours(11),
                    TriagedAtUtc = pastDate.AddHours(11).AddMinutes(8),
                    StartedAtUtc = pastDate.AddHours(11).AddMinutes(16),
                    CompletedAtUtc = pastDate.AddHours(11).AddMinutes(45),
                    SignedAtUtc = pastDate.AddHours(12),
                    SignedByUserId = firstCardiologist.Id
                };
                nadiaEncounter.Diagnoses.Add(new Diagnosis
                {
                    Code = "J45.20",
                    Description = "Mild intermittent bronchial reactivity — well controlled",
                    Type = DiagnosisType.Primary,
                    RecordedByUserId = firstCardiologist.Id,
                    RecordedAtUtc = pastDate.AddHours(11).AddMinutes(30)
                });
                nadiaEncounter.Notes.Add(new ClinicalNote
                {
                    Content = "Oxygen saturation 99% on room air. Heart sounds S1/S2 normal with no murmur. Chest radiograph clear with zero focal infiltrate. Continue breathing hygiene and schedule 10-day review.",
                    AuthorUserId = firstCardiologist.Id,
                    AuthorRole = Role.Doctor.ToString(),
                    Status = NoteStatus.Signed,
                    CreatedAtUtc = pastDate.AddHours(11).AddMinutes(35),
                    SignedAtUtc = pastDate.AddHours(12),
                    SignedByUserId = firstCardiologist.Id
                });
                dbContext.Encounters.Add(nadiaEncounter);
                await dbContext.SaveChangesAsync(cancellationToken);

                dbContext.Appointments.Add(new Appointment
                {
                    Number = $"APT-MNR-{Random.Shared.Next(400, 799)}",
                    AppointmentDate = pastDate,
                    SlotMinutes = 660,
                    DurationMinutes = 30,
                    Status = AppointStatus.Completed,
                    PatientID = nadiaPatient.Id,
                    DoctorID = firstCardiologist.Id,
                    EncounterId = nadiaEncounter.Id,
                    Notes = "Completed Cardiopulmonary Wellness Evaluation — Follow-Up Eligible",
                    CreatedAtUtc = pastDate.AddDays(-2),
                    CompletedAtUtc = pastDate.AddHours(12)
                });

                dbContext.Appointments.Add(new Appointment
                {
                    Number = $"APT-MNR-{Random.Shared.Next(800, 999)}",
                    AppointmentDate = DateTime.UtcNow.Date.AddDays(4),
                    SlotMinutes = 600,
                    DurationMinutes = 30,
                    Status = AppointStatus.Approved,
                    PatientID = nadiaPatient.Id,
                    DoctorID = firstCardiologist.Id,
                    Notes = "Follow-Up Cardiopulmonary Check",
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
                });

                var cbcTest = await dbContext.LabTests.FirstOrDefaultAsync(t => t.Code == "CBC", cancellationToken)
                              ?? await dbContext.LabTests.FirstOrDefaultAsync(cancellationToken);
                if (cbcTest is not null)
                {
                    dbContext.LabOrders.Add(new LabOrder
                    {
                        Number = $"LAB-MNR-{Random.Shared.Next(400, 799)}",
                        PatientId = nadiaPatient.Id,
                        EncounterId = nadiaEncounter.Id,
                        OrderedByUserId = firstCardiologist.Id,
                        ClinicalNotes = "Baseline Hematology Screening",
                        Status = LabOrderStatus.Released,
                        SpecimenId = $"SPC-MNR-{Random.Shared.Next(400, 799)}",
                        CollectedAtUtc = pastDate.AddHours(12).AddMinutes(10),
                        ReceivedAtUtc = pastDate.AddHours(12).AddMinutes(25),
                        ReleasedAtUtc = pastDate.AddHours(15),
                        ReleasedByUserId = labStaff?.Id ?? firstCardiologist.Id,
                        CreatedAtUtc = pastDate.AddHours(11).AddMinutes(40),
                        Items =
                        [
                            new LabOrderItem
                            {
                                LabTestId = cbcTest.Id,
                                ResultValue = "7.4",
                                NumericResult = 7.4m,
                                ResultPrice = cbcTest.Price,
                                Flag = DAL.Entities.Diagnostics.ResultFlag.Normal,
                                EnteredByUserId = labStaff?.Id ?? firstCardiologist.Id,
                                EnteredAtUtc = pastDate.AddHours(14),
                                VerifiedByUserId = labStaff?.Id ?? firstCardiologist.Id,
                                VerifiedAtUtc = pastDate.AddHours(15),
                                Comment = "White cell count & haemoglobin strictly within healthy Alexandrian reference intervals."
                            }
                        ]
                    });
                }

                dbContext.ImagingOrders.Add(new ImagingOrder
                {
                    Number = $"IMG-MNR-{Random.Shared.Next(400, 799)}",
                    PatientId = nadiaPatient.Id,
                    EncounterId = nadiaEncounter.Id,
                    OrderedByUserId = firstCardiologist.Id,
                    Modality = "Digital Chest X-Ray (PA View)",
                    StudyName = "Chest Radiograph PA — Cardiopulmonary Screening",
                    ClinicalIndication = "Seasonal cough baseline screening",
                    Status = ImagingOrderStatus.Released,
                    ScheduledForUtc = pastDate.AddHours(12).AddMinutes(15),
                    PerformedAtUtc = pastDate.AddHours(12).AddMinutes(30),
                    RadiologistUserId = radDoc.Id,
                    VerifiedByUserId = radDoc.Id,
                    VerifiedAtUtc = pastDate.AddHours(14),
                    Findings = "Lungs are clear with symmetrical costophrenic angles. Cardiothoracic ratio normal (<0.48). No pleural effusion or consolidation.",
                    Impression = "Normal Chest Radiograph. Cleared for progressive aerobic exercise.",
                    CreatedAtUtc = pastDate.AddHours(11).AddMinutes(42)
                });

                var meds = await dbContext.Medications.Take(2).ToListAsync(cancellationToken);
                if (meds.Count > 0)
                {
                    dbContext.Prescriptions.Add(new Prescription
                    {
                        Number = $"RX-MNR-{Random.Shared.Next(400, 799)}",
                        EncounterId = nadiaEncounter.Id,
                        PatientId = nadiaPatient.Id,
                        PrescribedByUserId = firstCardiologist.Id,
                        Status = PrescriptionStatus.Dispensed,
                        DoctorInstructions = "Keep inhaler handy before chilly evening walks and complete 14-day airway support schedule.",
                        NextFollowUpDate = DateTime.UtcNow.Date.AddDays(10),
                        CreatedAtUtc = pastDate.AddHours(11).AddMinutes(45),
                        Items = meds.Select((m, idx) => new PrescriptionItem
                        {
                            MedicationId = m.Id,
                            Dose = idx == 0 ? "1 puff" : "1 tablet",
                            Frequency = idx == 0 ? "As needed" : "Daily after lunch",
                            Duration = "14 days",
                            Route = idx == 0 ? "Inhalation" : "Oral",
                            Quantity = 14,
                            DispensedQuantity = 14,
                            Instructions = "Follow MANARA pharmacist leaflet",
                            IntakeSchedule = idx == 0 ? "PRN before exercise" : "01:30 PM after lunch"
                        }).ToList()
                    });
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            if (!await dbContext.ConsultationInquiries.AnyAsync(cancellationToken))
            {
                dbContext.ConsultationInquiries.AddRange(
                    new ConsultationInquiry
                    {
                        PatientUserId = nadiaPatient.Id,
                        PatientId = null,
                        PatientName = "Nadia Mahmoud",
                        PatientEmail = "nadia.patient@gmail.com",
                        DepartmentId = firstCardiologist.DepartmentId,
                        DoctorId = null,
                        Subject = "Safe jogging pace after chest radiograph clearance",
                        Question = "Thank you Dr. Salma — since my Chest X-Ray came back normal, can I resume light morning jogging along the Corniche this week?",
                        Status = "Answered",
                        DoctorReply = "Yes Nadia! Your PA chest study and CBC parameters are completely normal. Start with 20 minutes of moderate brisk walking for 3 days before resuming gentle jogging, and keep hydrated.",
                        RepliedByUserId = firstCardiologist.Id,
                        RepliedByName = "Dr. Salma El-Sayed",
                        RepliedAt = DateTime.UtcNow.AddHours(-6),
                        PatientNotifiedByEmail = true,
                        IsReadByPatient = false,
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    },
                    new ConsultationInquiry
                    {
                        PatientUserId = ahmedPatient?.Id,
                        PatientName = "Ahmed Hassan",
                        PatientEmail = "ahmed.patient@gmail.com",
                        DepartmentId = firstCardiologist.DepartmentId,
                        Subject = "Home blood pressure evening timing",
                        Question = "Should I log my evening blood pressure before dinner or 1 hour after taking my evening tablet?",
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow.AddHours(-3)
                    });
            }

            if (!await dbContext.EmailNotificationLogs.AnyAsync(n => n.RecipientUserId == nadiaPatient.Id, cancellationToken))
            {
                dbContext.EmailNotificationLogs.AddRange(
                    new EmailNotificationLog
                    {
                        RecipientUserId = nadiaPatient.Id,
                        RecipientEmail = nadiaPatient.Email!,
                        RecipientName = "Nadia Mahmoud",
                        Category = "LabResult",
                        Subject = "Laboratory Panel Verified & Released (CBC)",
                        BodySummary = "Dr. Laila Kamel verified your Complete Blood Count result (7.4 10^9/L — Within Normal Reference Range).",
                        ActionUrl = "/Updates",
                        DeliveryStatus = "In-App & Ready",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddHours(-18)
                    },
                    new EmailNotificationLog
                    {
                        RecipientUserId = nadiaPatient.Id,
                        RecipientEmail = nadiaPatient.Email!,
                        RecipientName = "Nadia Mahmoud",
                        Category = "RadiologyResult",
                        Subject = "Radiology PACS Report Released — Chest X-Ray PA",
                        BodySummary = "Dr. Amr Fouad released your Chest Radiograph PA report: Normal cardiopulmonary silhouette, clear lung fields.",
                        ActionUrl = "/Updates",
                        DeliveryStatus = "In-App & Ready",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddHours(-16)
                    },
                    new EmailNotificationLog
                    {
                        RecipientUserId = nadiaPatient.Id,
                        RecipientEmail = nadiaPatient.Email!,
                        RecipientName = "Nadia Mahmoud",
                        Category = "ConsultationReply",
                        Subject = "Dr. Salma El-Sayed replied to your consultation",
                        BodySummary = "New clinical response regarding safe jogging clearance along the Corniche.",
                        ActionUrl = "/Updates",
                        DeliveryStatus = "In-App & Ready",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddHours(-6)
                    });
            }
        }

        if (!await dbContext.PlatformFeedbacks.AnyAsync(cancellationToken))
        {
            dbContext.PlatformFeedbacks.AddRange(
                new PlatformFeedback
                {
                    UserId = nadiaPatient?.Id,
                    AuthorName = "Nadia Mahmoud",
                    AuthorEmail = "nadia.patient@gmail.com",
                    Category = "Review",
                    TargetScope = "Doctor",
                    TargetName = "Dr. Salma El-Sayed · Cardiology",
                    Rating = 5,
                    Subject = "Exceptional serenity & same-day PACS clarity",
                    Message = "From the tranquil reception pavilion to receiving my verified Chest X-Ray notes in My Updates the same afternoon, MANARA feels unlike any clinic in Alexandria.",
                    Status = "Reviewed",
                    ReviewedAt = DateTime.UtcNow.AddDays(-2),
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new PlatformFeedback
                {
                    AuthorName = "Tariq Ziad",
                    AuthorEmail = "tariq.ziad@gmail.com",
                    Category = "Review",
                    TargetScope = "Hospital",
                    TargetName = "MANARA Medical Complex",
                    Rating = 5,
                    Subject = "Zero wait congestion & spotless diagnostic wing",
                    Message = "Check-in took less than two minutes and our family pediatrician explained every growth milestone patiently.",
                    Status = "Reviewed",
                    ReviewedAt = DateTime.UtcNow.AddDays(-4),
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                },
                new PlatformFeedback
                {
                    AuthorName = "LaylaMansour",
                    AuthorEmail = "layla.m@gmail.com",
                    Category = "Suggestion",
                    TargetScope = "Department",
                    TargetName = "Clinical Laboratory",
                    Subject = "Early Friday fasting blood draw slot",
                    Message = "Adding a 7:30 AM Saturday early fasting phlebotomy hour would help working professionals complete panels before office hours.",
                    Status = "New",
                    CreatedAt = DateTime.UtcNow.AddHours(-12)
                });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

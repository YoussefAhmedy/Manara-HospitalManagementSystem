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

        var branch = new Branch { Name = "CareAxis Central", Code = "CAX-01", City = "Alexandria", Phone = "+20 3 000 0000" };
        dbContext.Branches.Add(branch);

        var departments = new List<Department>
        {
            new() { Name = "Internal Medicine", Code = "IM", Branch = branch },
            new() { Name = "Cardiology", Code = "CARD", Branch = branch },
            new() { Name = "Paediatrics", Code = "PAED", Branch = branch },
            new() { Name = "Laboratory", Code = "LAB", Branch = branch },
            new() { Name = "Radiology", Code = "RAD", Branch = branch },
            new() { Name = "Pharmacy", Code = "PHM", Branch = branch }
        };
        dbContext.Departments.AddRange(departments);

        var specializations = new List<Specialization>
        {
            new() { Name = "Internal Medicine" },
            new() { Name = "Cardiology" },
            new() { Name = "Paediatrics" }
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
            ("admin", "Amina", "Farouk", Role.HospitalAdmin),
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

        // 1. Ensure all 8 Departments with rich clinical metadata
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
             "Digital Dermoscopy · Allergy Patching · Eczema Care · Minor Surgery")
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
            return user;
        }

        await EnsureUserAsync("omar.admin@gmail.com", "Admin@2026", "Omar", "Ahmedy", "Male", [Role.Admin.ToString(), Role.HospitalAdmin.ToString()]);
        await EnsureUserAsync("nourhan.receptionist@gmail.com", "Recep@2026", "Nourhan", "Adel", "Female", [Role.Receptionist.ToString()]);
        await EnsureUserAsync("hossam.labtech@gmail.com", "Lab@2026", "Hossam", "Gamal", "Male", [Role.LabTechnician.ToString()]);
        await EnsureUserAsync("ramez.radiologist@gmail.com", "Rad@2026", "Ramez", "Mansour", "Male", [Role.Radiologist.ToString()]);
        await EnsureUserAsync("mona.pharmacist@gmail.com", "Pharm@2026", "Mona", "El-Gamal", "Female", [Role.Pharmacist.ToString()]);
        await EnsureUserAsync("sherif.billing@gmail.com", "Bill@2026", "Sherif", "Kamel", "Male", [Role.BillingOfficer.ToString()]);

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

        // 5. Seed the 16 Unique-Portrait Specialist Doctors across all 8 Departments (2 Doctors each)
        var doctorCatalog = new (
            string Email, string First, string Last, string Gender, string DeptName,
            string Title, string Credentials, string Room, decimal Fee, decimal FollowUpFee,
            int ExpYears, decimal Rating, int Reviews, bool TopRated, string Bio, string Img, string Seal, DayOfWeek[] Days)[]
        {
            // Cardiology
            ("salma.cardiologist@gmail.com", "Salma", "El-Sayed", "Female", "Cardiology",
             "Senior Consultant Cardiologist", "MD, FESC", "Clinic 301", 600, 200, 14, 4.95m, 186, true,
             "Fellow of the European Society of Cardiology specializing in echocardiography, preventive heart care, and women's cardiovascular wellness.",
             "doc-salma.jpg", "CARDIAC EXCELLENCE • MANARA", [DayOfWeek.Sunday, DayOfWeek.Tuesday, DayOfWeek.Thursday]),
            ("hisham.cardiologist@gmail.com", "Hisham", "El-Masry", "Male", "Cardiology",
             "Consultant Interventional Cardiologist", "MD, FACC", "Clinic 302", 700, 220, 16, 4.85m, 119, false,
             "Specialist in coronary risk stratification, complex hypertension therapy, and non-invasive cardiac diagnostics.",
             "doc-hisham.jpg", "HEART INSTITUTE • MANARA", [DayOfWeek.Monday, DayOfWeek.Wednesday]),

            // Internal Medicine
            ("karim.internist@gmail.com", "Karim", "Mansour", "Male", "Internal Medicine",
             "Head of Internal Medicine", "MD, MRCP", "Clinic 204", 450, 150, 18, 4.88m, 142, false,
             "Royal College of Physicians fellow leading comprehensive metabolic screening, diabetes care, and adult chronic disease programs.",
             "doc-karim.jpg", "INTERNAL MEDICINE • MANARA", [DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Wednesday]),
            ("mariam.internist@gmail.com", "Mariam", "Soliman", "Female", "Internal Medicine",
             "Consultant Endocrinologist", "MD, PhD", "Clinic 205", 500, 160, 12, 4.84m, 97, false,
             "Dedicated to thyroid disorders, hormonal longevity, insulin resistance, and personalized executive health plans.",
             "doc-mariam.jpg", "METABOLIC CARE • MANARA", [DayOfWeek.Tuesday, DayOfWeek.Thursday]),

            // Orthopedics
            ("tarek.orthopedist@gmail.com", "Tarek", "Nabil", "Male", "Orthopedics",
             "Consultant Orthopedic Surgeon", "MD, FRCS", "Clinic 112", 550, 180, 19, 4.92m, 164, true,
             "Senior orthopedic reconstructive specialist focused on minimally invasive joint arthroscopy and spine preservation.",
             "doc-tarek.jpg", "JOINT & SPINE • MANARA", [DayOfWeek.Sunday, DayOfWeek.Tuesday]),
            ("sherif.orthopedist@gmail.com", "Sherif", "El-Shazly", "Male", "Orthopedics",
             "Sports Injury & Joint Specialist", "MD, FAAOS", "Clinic 114", 600, 200, 13, 4.78m, 88, false,
             "Sports medicine consultant guiding athletes and active adults through ligament recovery and precision biomechanics.",
             "doc-sherif.jpg", "SPORTS ORTHOPEDICS • MANARA", [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Thursday]),

            // Pediatrics
            ("nadia.pediatrician@gmail.com", "Nadia", "Farouk", "Female", "Pediatrics",
             "Senior Specialist Pediatrician", "MD, MRCPCH", "Clinic 105", 400, 120, 11, 4.94m, 158, true,
             "Gentle family pediatrician focusing on early childhood milestones, respiratory wellness, and preventive immunization.",
             "doc-nadia.jpg", "CHILD WELLNESS • MANARA", [DayOfWeek.Sunday, DayOfWeek.Wednesday]),
            ("hana.pediatrician@gmail.com", "Hana", "Abdelrahman", "Female", "Pediatrics",
             "Consultant Adolescent Pediatrician", "MD, DCH", "Clinic 107", 380, 120, 9, 4.86m, 134, false,
             "Warm, attentive pediatric specialist dedicated to developmental nutrition, pediatric allergy management, and newborn follow-up.",
             "doc-hana.jpg", "PEDIATRIC SANCTUARY • MANARA", [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday]),

            // Radiology & Imaging
            ("amr.radiologist@gmail.com", "Amr", "Fouad", "Male", "Radiology & Imaging",
             "Consultant Diagnostic Radiologist", "MD, FRCR", "Imaging Suite 1", 450, 150, 17, 4.85m, 92, false,
             "Fellow of the Royal College of Radiologists overseeing 3T MRI interpretation, cardiac CT, and rapid PACS verification.",
             "doc-amr.jpg", "DIAGNOSTIC PACS • MANARA", [DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday]),
            ("farida.radiologist@gmail.com", "Farida", "El-Khatib", "Female", "Radiology & Imaging",
             "Consultant Musculoskeletal Radiologist", "MD, EDiR", "Imaging Suite 2", 480, 160, 11, 4.90m, 105, false,
             "European Diploma radiologist specializing in high-resolution joint MRI, breast imaging, and vascular Doppler ultrasound.",
             "doc-farida.jpg", "PRECISION IMAGING • MANARA", [DayOfWeek.Sunday, DayOfWeek.Wednesday, DayOfWeek.Thursday]),

            // Clinical Laboratory
            ("laila.pathologist@gmail.com", "Laila", "Kamel", "Female", "Clinical Laboratory",
             "Chief Clinical Pathologist", "MD, FRCPath", "Lab Central", 350, 100, 15, 4.83m, 76, false,
             "Directs MANARA's two-step laboratory verification gate, hematology profiling, and molecular biomarker assurance.",
             "doc-laila.jpg", "CLINICAL PATHOLOGY • MANARA", [DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday]),
            ("yasser.pathologist@gmail.com", "Yasser", "Ibrahim", "Male", "Clinical Laboratory",
             "Consultant Clinical Biochemist", "MD, PhD", "Lab Diagnostic 2", 360, 110, 10, 4.76m, 64, false,
             "Specializes in clinical chemistry panels, autoimmune assays, and precision metabolic diagnostics.",
             "doc-yasser.jpg", "DIAGNOSTIC LAB • MANARA", [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday]),

            // Neurology
            ("sara.neurologist@gmail.com", "Sara", "El-Gamal", "Female", "Neurology",
             "Senior Consultant Neurologist", "MD, FEBN", "Clinic 401", 650, 220, 13, 4.93m, 151, true,
             "European Board neurologist specializing in migraine management, epilepsy care, and neuro-vascular recovery.",
             "doc-sara.jpg", "NEUROSCIENCE • MANARA", [DayOfWeek.Sunday, DayOfWeek.Tuesday]),
            ("mostafa.neurologist@gmail.com", "Mostafa", "Zidan", "Male", "Neurology",
             "Consultant Neurophysiologist", "MD, PhD", "Clinic 403", 750, 250, 21, 4.87m, 112, false,
             "Renowned authority in EEG diagnostics, nerve conduction studies, and movement disorder therapeutics.",
             "doc-mostafa.jpg", "NEUROPHYSIOLOGY • MANARA", [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Thursday]),

            // Dermatology
            ("yasmin.dermatologist@gmail.com", "Yasmin", "Zaki", "Female", "Dermatology",
             "Consultant Clinical Dermatologist", "MD, FAAD", "Clinic 208", 500, 150, 10, 4.91m, 173, false,
             "Fellow of the American Academy of Dermatology focused on digital dermoscopy, skin barrier restoration, and allergic skin conditions.",
             "doc-yasmin.jpg", "DERM SANCTUARY • MANARA", [DayOfWeek.Sunday, DayOfWeek.Wednesday]),
            ("khaled.dermatologist@gmail.com", "Khaled", "Hassan", "Male", "Dermatology",
             "Consultant Dermatologic Surgeon", "MD, PhD", "Clinic 210", 550, 180, 14, 4.79m, 89, false,
             "Specialist in precision dermatologic procedures, laser therapeutics, and restorative skin medicine.",
             "doc-khaled.jpg", "DERM SURGERY • MANARA", [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday])
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
    }
}

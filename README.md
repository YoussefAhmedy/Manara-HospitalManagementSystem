# CareAxis — hospital management platform

A role-based hospital information system built on ASP.NET Core MVC: patient records, scheduling,
clinical encounters, laboratory, radiology, pharmacy, admissions and beds, billing with insurance,
parameterised reporting, and an append-only audit trail — in English and Arabic, with a permission
model enforced on every request.

> **Synthetic data only.** This repository is a working platform, not a certified medical device. It
> has **not** been assessed for HIPAA, GDPR or any other regulatory regime, and no such claim is made.
> Read `docs/hospital-system/commercial-readiness-assessment.md` before considering any deployment
> involving real patients.

---

## What this is

| | |
|---|---|
| **Stack** | .NET 10 · ASP.NET Core MVC · C# 14 · EF Core 10 (SQL Server) · Identity · Hangfire |
| **Roles** | 14 (11 operational + 3 legacy), each with a least-privilege permission grant |
| **Permissions** | 43, enforced as authorisation policies on every action |
| **Modules** | Patients · Appointments · Encounters · Laboratory · Radiology · Pharmacy · Admissions & beds · Billing · Reports · Audit |
| **Reports** | 18, all backed by live queries, with paging, CSV export and print |
| **Languages** | English (LTR) and Arabic (RTL), switchable at runtime |
| **Tests** | 70 unit tests across nine suites, run on every push |

## Try it

1. Start the app (see *Running locally*). Demo data is seeded automatically in non-production
   environments when `Demo:Seed` is enabled.
2. For security hygiene, demo account credentials are **not** shown on the public website UI.
   Use the tables below (or open the offline PDF reference [`docs/MANARA_Demo_Accounts.pdf`](docs/MANARA_Demo_Accounts.pdf))
   to sign in at `/Account/LogIn`.

### Seeded Demo Accounts Directory

#### Core Hospital Administration, Staff & Patients

| Full Name | Role | Email | Password | Department / Scope |
|---|---|---|---|---|
| **Omar Ahmedy** | Hospital Manager / Admin | `omar.admin@gmail.com` | `Admin@2026` | Full hospital command, staff, schedules, RBAC & audit |
| **Ahmed Hassan** | Patient (Returning Follow-Up) | `ahmed.patient@gmail.com` | `Patient@2026` | My Care, prescriptions, follow-ups, PACS & invoices |
| **Nadia Mahmoud** | Patient | `nadia.patient@gmail.com` | `Patient@2026` | Patient self-service booking, medical history & records |
| **Nourhan Adel** | Front Desk Receptionist | `nourhan.receptionist@gmail.com` | `Recep@2026` | Patient intake, conflict-checked scheduling & check-in |
| **Hossam Gamal** | Laboratory Specialist | `hossam.labtech@gmail.com` | `Lab@2026` | Specimen tracking, 7-stage verification safety gate |
| **Ramez Mansour** | Radiology Specialist | `ramez.radiologist@gmail.com` | `Rad@2026` | Imaging order queue, PACS viewer & report release |
| **Mona El-Gamal** | Clinical Pharmacist | `mona.pharmacist@gmail.com` | `Pharm@2026` | Rx review, transactional batch dispensing & stock alerts |
| **Sherif Kamel** | Billing & Insurance Officer | `sherif.billing@gmail.com` | `Bill@2026` | Invoicing, claims, payment collection & approvals |

#### Attending Medical Faculty (16 Specialists across 8 Departments)

| Full Name | Role | Email | Password | Department / Specialty |
|---|---|---|---|---|
| **Dr. Salma El-Sayed** | Doctor (Top Rated) | `salma.cardiologist@gmail.com` | `Doctor@2026` | Cardiology (600 EGP / 200 EGP Follow-Up) |
| **Dr. Hisham El-Masry** | Doctor | `hisham.cardiologist@gmail.com` | `Doctor@2026` | Cardiology (700 EGP / 220 EGP Follow-Up) |
| **Dr. Karim Mansour** | Doctor | `karim.internist@gmail.com` | `Doctor@2026` | Internal Medicine (450 EGP / 150 EGP Follow-Up) |
| **Dr. Mariam Soliman** | Doctor | `mariam.internist@gmail.com` | `Doctor@2026` | Internal Medicine (500 EGP / 160 EGP Follow-Up) |
| **Dr. Tarek Nabil** | Doctor (Top Rated) | `tarek.orthopedist@gmail.com` | `Doctor@2026` | Orthopedics (550 EGP / 180 EGP Follow-Up) |
| **Dr. Sherif El-Shazly** | Doctor | `sherif.orthopedist@gmail.com` | `Doctor@2026` | Orthopedics (600 EGP / 200 EGP Follow-Up) |
| **Dr. Nadia Farouk** | Doctor (Top Rated) | `nadia.pediatrician@gmail.com` | `Doctor@2026` | Pediatrics (400 EGP / 120 EGP Follow-Up) |
| **Dr. Hana Abdelrahman** | Doctor | `hana.pediatrician@gmail.com` | `Doctor@2026` | Pediatrics (380 EGP / 120 EGP Follow-Up) |
| **Dr. Amr Fouad** | Doctor / Radiologist | `amr.radiologist@gmail.com` | `Doctor@2026` | Radiology & Imaging (450 EGP / 150 EGP Follow-Up) |
| **Dr. Farida El-Khatib** | Doctor | `farida.radiologist@gmail.com` | `Doctor@2026` | Radiology & Imaging (480 EGP / 160 EGP Follow-Up) |
| **Dr. Laila Kamel** | Doctor / Pathologist | `laila.pathologist@gmail.com` | `Doctor@2026` | Clinical Laboratory (350 EGP / 100 EGP Follow-Up) |
| **Dr. Yasser Ibrahim** | Doctor / Biochemist | `yasser.pathologist@gmail.com` | `Doctor@2026` | Clinical Laboratory (360 EGP / 110 EGP Follow-Up) |
| **Dr. Sara El-Gamal** | Doctor (Top Rated) | `sara.neurologist@gmail.com` | `Doctor@2026` | Neurology (650 EGP / 220 EGP Follow-Up) |
| **Dr. Mostafa Zidan** | Doctor | `mostafa.neurologist@gmail.com` | `Doctor@2026` | Neurology (750 EGP / 250 EGP Follow-Up) |
| **Dr. Yasmin Zaki** | Doctor | `yasmin.dermatologist@gmail.com` | `Doctor@2026` | Dermatology (500 EGP / 150 EGP Follow-Up) |
| **Dr. Khaled Hassan** | Doctor | `khaled.dermatologist@gmail.com` | `Doctor@2026` | Dermatology (550 EGP / 180 EGP Follow-Up) |

> Demo seeding is intended strictly for Development/Evaluation environments (`Demo:Seed = true`). Keep it disabled in Production.

## Running locally

Requirements: .NET SDK 10.0.100+ and a reachable SQL Server instance.

```bash
# 1. Point at a database. The application refuses to start without a connection string.
export ConnectionStrings__DefaultConnection="Server=localhost;Database=CareAxis;Trusted_Connection=True;TrustServerCertificate=True"

# 2. Run. In non-production the schema is created from the model if no migration files exist,
#    then demo data is seeded.
dotnet run --project HospitalManagement/HospitalManagement.csproj
```

Create the first real administrator (no default account exists):

```bash
dotnet run --project HospitalManagement/HospitalManagement.csproj -- --bootstrap-admin
```

In **Production** the application will not create a schema from the model. Apply the pipeline's
idempotent script first:

```bash
dotnet ef migrations script --idempotent \
  --project DAL/Hospital.DAL.csproj \
  --startup-project HospitalManagement/HospitalManagement.csproj \
  --output careaxis-schema.sql
```

## How authorisation works

Roles carry **permission claims**; controllers authorise on policies built from those permission
names. A role is a bundle, not a hard-coded identity check, so privilege can change without editing
controllers.

```csharp
[Authorize(Policy = "perm:" + Permission.LabRelease)]
public async Task<IActionResult> Transition(int id, LabOrderStatus target, ...)
```

The grant map lives in one file — `Hospital.BLL/Permissions/RolePermissions.cs` — and the documented
matrix in `docs/hospital-system/user-roles-permissions.md` is machine-verified against it.

### Safety properties that are enforced, not merely intended

| Property | How |
|---|---|
| A clinician cannot be double-booked | Overlap rules + filtered unique index `UX_Appointments_DoctorSlot` |
| A bed cannot hold two patients | Filtered unique index `UX_BedAssignments_ActiveOccupancy` |
| An unverified laboratory result is never "final" | `LabRules.IsClinicallyFinal`; the transition graph cannot skip verification |
| A signed clinical record cannot be edited | `EncounterRules.IsEditable`; corrections become amendments with their own identity |
| Nobody approves their own refund | `ApproveRefundAsync` rejects the recording officer |
| Front desk cannot author clinical content | Receipt/permission separation, asserted in tests |

## Documentation

All in [`docs/hospital-system/`](docs/hospital-system):

| Document | Contents |
|---|---|
| `current-system-audit.md` | What the original system did, with evidence |
| `module-inventory.md` | Every capability: original state, decision, **delivered state** |
| `technical-architecture.md` | As-is and target architecture |
| `improvement-roadmap.md` | The ten phases and their gates |
| `user-roles-permissions.md` | Roles, 43 permissions, verified grant matrix, segregation of duties |
| `workflow-mapping.md` | Every clinical and financial workflow with its state machine |
| `design-system.md` | Tokens, components, breakpoints, RTL, accessibility intent |
| `data-architecture.md` | Entities, invariants, lifecycle, concurrency, classification |
| `reporting-specification.md` | All 18 reports and the shared contract |
| `patient-examination-report.md` | Draft / reviewed / signed / finalized and what each permits |
| `security-privacy-review.md` | Control inventory, ASVS 5.0 mapping, risk register |
| `healthcare-standards-mapping.md` | FHIR R5 alignment and the explicit non-conformance position |
| `test-plan.md` | Test strategy, inventory, defects found, what was **not** run |
| `patient-examination-report.md` | Draft / reviewed / signed / finalized and what each permits |
| `commercial-readiness-assessment.md` | Implemented vs demo vs backend-required vs legal review |
| `project-examination-report.md` | A reviewer's assessment, including the weaknesses |
| `final-report.md` | The closing report and verdict |

Toolchain artefacts are in [`toolkit-output/`](toolkit-output).

## Testing

```bash
dotnet test HospitalManagement.sln --configuration Release
```

CI runs restore → build → test on every push, then generates the EF migration and an idempotent SQL
script as a build artifact. Failed tests are surfaced as build annotations with their assertion
messages.

## Security posture in one paragraph

HTTPS with HSTS, a strict `script-src 'self'` Content-Security-Policy backed by the absence of any
inline script (enforced by a test), `nosniff`/`DENY`/`no-referrer` headers, global anti-forgery
validation, per-IP rate limiting on authentication, signature-checked uploads stored outside the web
root, `no-store` on every clinical surface, and an append-only audit trail of who did what.
**Missing and required before real data:** multi-factor authentication, encryption at rest, tested
backups, retention and erasure workflows, tenant isolation, and an independent penetration test.

## Licence and status

Internal hospital software, developed as a transformation of the existing repository. The design,
workflows and every documented property are traceable to code; where something is not built, the
documentation says so explicitly rather than describing it as complete.

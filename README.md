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

### Design System & UI References
- **Executive Dark Dashboard Reference:** Inspired by clean flat SaaS fintech precision and editorial typography ([Algorithmic Trading Dashboard — Awwwards](https://www.awwwards.com/sites/algorithmic-trading-dashboard))
- **Color Palette System:**
  - **Staff Executive Workspace (100% Dark Mode):** Racing Green Charcoal base `#18181A`, Deep Surface `#232326`, Border `#313136`, Blue Orchid primary `#2563EB`, Ice Gray crisp foreground `#F3F3F5`
  - **Public Patient Sanctuary (Light Mode):** Alabaster Sanctuary Canvas `#FFFFFF` / `#F8FAFC`, Deep Maritime Ink `#0F172A`, Greco-Roman Serif Headers + Inter Sans UI
  - **Split-Screen Auth Pages:** Pure Alabaster White + Curved Royal Blue `#1E3A8A` → `#2563EB` accent sculpture
- **Offline Reference:** [`docs/MANARA_Demo_Accounts.pdf`](docs/MANARA_Demo_Accounts.pdf)

### Seeded Demo Accounts Directory

#### Core Hospital Administration, Staff & Patients

| Full Name | Role | Email | Password | Department / Scope |
|---|---|---|---|---|
| **Youssef El-Masry** | Hospital Manager | `youssef.manager@gmail.com` | `Manager@2026` | Full hospital command, staff, schedules, feedback & audit |
| **Nadia Mahmoud** | Patient (Full Demo Hub) | `nadia.patient@gmail.com` | `Patient@2026` | My Updates, released Lab & Radiology PACS, Rx, follow-up & Q&A |
| **Ahmed Hassan** | Patient (Returning Follow-Up) | `ahmed.patient@gmail.com` | `Patient@2026` | My Care, prescriptions, follow-ups & clinical history |
| **Nourhan Adel** | Front Desk Receptionist | `nourhan.receptionist@gmail.com` | `Recep@2026` | Patient intake, conflict-checked scheduling & check-in |
| **Hossam Gamal** | Laboratory Specialist | `hossam.labtech@gmail.com` | `Lab@2026` | Specimen tracking, 7-stage verification safety gate |
| **Ramez Mansour** | Radiology Specialist | `ramez.radiologist@gmail.com` | `Rad@2026` | Imaging order queue, PACS viewer & report release |
| **Mona El-Gamal** | Clinical Pharmacist | `mona.pharmacist@gmail.com` | `Pharm@2026` | Rx review, transactional batch dispensing & stock alerts |
| **Sherif Kamel** | Billing & Insurance Officer | `sherif.billing@gmail.com` | `Bill@2026` | Invoicing, claims, payment collection & approvals |

#### Attending Medical Faculty (45 Specialists across 9 Departments — 5 per Department)

| Full Name | Role | Email | Password | Department / Specialty |
|---|---|---|---|---|
| **Dr. Salma El-Sayed** | Doctor (Top Rated) | `salma.cardiologist@gmail.com` | `Doctor@2026` | Cardiology (600 / 200 EGP) |
| **Dr. Hisham El-Masry** | Doctor | `hisham.cardiologist@gmail.com` | `Doctor@2026` | Cardiology (700 / 220 EGP) |
| **Dr. Dina El-Sharif** | Doctor | `dina.cardiologist@gmail.com` | `Doctor@2026` | Cardiology (550 / 180 EGP) |
| **Dr. Omar Khaled** | Doctor | `omar.khaled.cardiologist@gmail.com` | `Doctor@2026` | Cardiology (720 / 240 EGP) |
| **Dr. Helena Kostas** | Doctor | `helena.cardiologist@gmail.com` | `Doctor@2026` | Cardiology (640 / 210 EGP) |
| **Dr. Karim Mansour** | Doctor | `karim.internist@gmail.com` | `Doctor@2026` | Internal Medicine (450 / 150 EGP) |
| **Dr. Mariam Soliman** | Doctor | `mariam.internist@gmail.com` | `Doctor@2026` | Internal Medicine (500 / 160 EGP) |
| **Dr. Bassem Tawfik** | Doctor | `bassem.internist@gmail.com` | `Doctor@2026` | Internal Medicine (480 / 160 EGP) |
| **Dr. Rana El-Gohary** | Doctor | `rana.internist@gmail.com` | `Doctor@2026` | Internal Medicine (440 / 140 EGP) |
| **Dr. Sameh Mikhail** | Doctor | `sameh.internist@gmail.com` | `Doctor@2026` | Internal Medicine (520 / 170 EGP) |
| **Dr. Tarek Nabil** | Doctor (Top Rated) | `tarek.orthopedist@gmail.com` | `Doctor@2026` | Orthopedics (550 / 180 EGP) |
| **Dr. Sherif El-Shazly** | Doctor | `sherif.orthopedist@gmail.com` | `Doctor@2026` | Orthopedics (600 / 200 EGP) |
| **Dr. Ashraf Abdelmalak** | Doctor | `ashraf.orthopedist@gmail.com` | `Doctor@2026` | Orthopedics (620 / 200 EGP) |
| **Dr. Yara Hegazy** | Doctor | `yara.orthopedist@gmail.com` | `Doctor@2026` | Orthopedics (520 / 170 EGP) |
| **Dr. Ramy El-Banna** | Doctor | `ramy.orthopedist@gmail.com` | `Doctor@2026` | Orthopedics (580 / 190 EGP) |
| **Dr. Nadia Farouk** | Doctor (Top Rated) | `nadia.pediatrician@gmail.com` | `Doctor@2026` | Pediatrics (400 / 120 EGP) |
| **Dr. Hana Abdelrahman** | Doctor | `hana.pediatrician@gmail.com` | `Doctor@2026` | Pediatrics (380 / 120 EGP) |
| **Dr. Noura El-Wakeel** | Doctor | `noura.pediatrician@gmail.com` | `Doctor@2026` | Pediatrics (420 / 130 EGP) |
| **Dr. Hatem Shoukry** | Doctor | `hatem.pediatrician@gmail.com` | `Doctor@2026` | Pediatrics (460 / 150 EGP) |
| **Dr. Ines Mansour** | Doctor | `ines.pediatrician@gmail.com` | `Doctor@2026` | Pediatrics (430 / 140 EGP) |
| **Dr. Amr Fouad** | Doctor / Radiologist | `amr.radiologist@gmail.com` | `Doctor@2026` | Radiology & Imaging (450 / 150 EGP) |
| **Dr. Farida El-Khatib** | Doctor / Radiologist | `farida.radiologist@gmail.com` | `Doctor@2026` | Radiology & Imaging (480 / 160 EGP) |
| **Dr. Walid El-Sawy** | Doctor / Radiologist | `walid.radiologist@gmail.com` | `Doctor@2026` | Radiology & Imaging (520 / 170 EGP) |
| **Dr. Christine Dimitri** | Doctor / Radiologist | `christine.radiologist@gmail.com` | `Doctor@2026` | Radiology & Imaging (540 / 180 EGP) |
| **Dr. Ziad El-Araby** | Doctor / Radiologist | `ziad.radiologist@gmail.com` | `Doctor@2026` | Radiology & Imaging (490 / 160 EGP) |
| **Dr. Laila Kamel** | Doctor / Pathologist | `laila.pathologist@gmail.com` | `Doctor@2026` | Clinical Laboratory (350 / 100 EGP) |
| **Dr. Yasser Ibrahim** | Doctor / Biochemist | `yasser.pathologist@gmail.com` | `Doctor@2026` | Clinical Laboratory (360 / 110 EGP) |
| **Dr. Maya Haddad** | Doctor / Pathologist | `maya.pathologist@gmail.com` | `Doctor@2026` | Clinical Laboratory (390 / 120 EGP) |
| **Dr. Fady Youssef** | Doctor / Pathologist | `fady.pathologist@gmail.com` | `Doctor@2026` | Clinical Laboratory (370 / 110 EGP) |
| **Dr. Samar Nossier** | Doctor / Pathologist | `samar.pathologist@gmail.com` | `Doctor@2026` | Clinical Laboratory (400 / 130 EGP) |
| **Dr. Sara El-Gamal** | Doctor (Top Rated) | `sara.neurologist@gmail.com` | `Doctor@2026` | Neurology (650 / 220 EGP) |
| **Dr. Mostafa Zidan** | Doctor | `mostafa.neurologist@gmail.com` | `Doctor@2026` | Neurology (750 / 250 EGP) |
| **Dr. Tamer El-Khouly** | Doctor | `tamer.neurologist@gmail.com` | `Doctor@2026` | Neurology (680 / 220 EGP) |
| **Dr. Aliaa Darwish** | Doctor | `aliaa.neurologist@gmail.com` | `Doctor@2026` | Neurology (620 / 200 EGP) |
| **Dr. Mazen El-Refai** | Doctor | `mazen.neurologist@gmail.com` | `Doctor@2026` | Neurology (580 / 190 EGP) |
| **Dr. Yasmin Zaki** | Doctor | `yasmin.dermatologist@gmail.com` | `Doctor@2026` | Dermatology (500 / 150 EGP) |
| **Dr. Khaled Hassan** | Doctor | `khaled.dermatologist@gmail.com` | `Doctor@2026` | Dermatology (550 / 180 EGP) |
| **Dr. Salma Naguib** | Doctor | `salma.naguib.dermatologist@gmail.com` | `Doctor@2026` | Dermatology (490 / 150 EGP) |
| **Dr. Fouad Abdelaziz** | Doctor | `fouad.dermatologist@gmail.com` | `Doctor@2026` | Dermatology (520 / 170 EGP) |
| **Dr. Reem El-Shenawy** | Doctor | `reem.dermatologist@gmail.com` | `Doctor@2026` | Dermatology (470 / 140 EGP) |
| **Dr. Lamia El-Gamal** | Doctor / Pharmacist (Top Rated) | `lamia.pharmacist@gmail.com` | `Doctor@2026` | Pharmacy (350 / 100 EGP) |
| **Dr. Hazem El-Kady** | Doctor / Pharmacist | `hazem.pharmacist@gmail.com` | `Doctor@2026` | Pharmacy (380 / 110 EGP) |
| **Dr. Nesma Reda** | Doctor / Pharmacist | `nesma.pharmacist@gmail.com` | `Doctor@2026` | Pharmacy (360 / 110 EGP) |
| **Dr. Adel El-Gendy** | Doctor / Pharmacist | `adel.pharmacist@gmail.com` | `Doctor@2026` | Pharmacy (350 / 100 EGP) |
| **Dr. Hend Mostafa** | Doctor / Pharmacist | `hend.pharmacist@gmail.com` | `Doctor@2026` | Pharmacy (370 / 110 EGP) |

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

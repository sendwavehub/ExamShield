# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Skills

The `solid` skill is installed at `.claude/solid-skills-main/skills/solid/SKILL.md`. Invoke `/solid` for any coding, refactoring, architecture design, or code review task. It enforces:

- **TDD** — Red-Green-Refactor; write a failing test before any production code
- **SOLID** — applied to every class and function (see `references/solid-principles.md`)
- **Value objects** — mandatory for all domain primitives: `ExamId`, `StudentId`, `DeviceId`, `Hash`, `Signature`, `Email`, etc.
- **Clean code** — methods < 10 lines, classes < 50 lines, no `else` when early return works, Law of Demeter
- **Code smell detection** — auto-detect and refactor (see `references/code-smells.md`)

The skill's examples use TypeScript/NestJS but its principles apply equally to this project's C# backend, Flutter mobile app, and React dashboard.

---

## Project Status

This is an **actively scaffolded greenfield project**. The full product specification lives in `prompt.md`; the RBAC design is in `user-roles.md`. Implementation is underway — the backend skeleton exists under `src/`.

## What This Is

ExamShield is an enterprise-grade, AI-powered secure exam scanning and anti-tampering system. Invigilators photograph paper answer sheets using a Flutter mobile app; the image is immediately hashed, digitally signed, and stored immutably. An OCR pipeline then reads the answers, with low-confidence results routed to a human review queue. Every action is recorded in an append-only audit log.

## Architecture

The system follows **Clean Architecture** with **CQRS** and **DDD** principles:

```
ExamShield/
├── src/
│   ├── ExamShield.Domain/          # Entities, value objects, domain events, interfaces
│   ├── ExamShield.Application/     # Use cases, commands/queries (MediatR), DTOs
│   ├── ExamShield.Infrastructure/  # EF Core, storage, OCR, crypto, messaging
│   ├── ExamShield.Api/             # ASP.NET Core 9 Web API, OpenAPI
│   ├── ExamShield.Mobile/          # Flutter app (Android + iOS)
│   └── ExamShield.Dashboard/       # React + TypeScript admin UI
├── tests/
│   ├── ExamShield.UnitTests/
│   └── ExamShield.IntegrationTests/
├── infra/
│   ├── docker-compose.yml
│   └── k8s/
└── docs/
```

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core 9, C#, EF Core |
| Mobile | Flutter (Android + iOS) |
| Admin UI | React, TypeScript, Tailwind CSS, shadcn/ui |
| Database | PostgreSQL |
| Cache | Redis |
| Messaging | RabbitMQ |
| Object Storage | MinIO (dev) / AWS S3 Object Lock / Azure Immutable Blob (prod) |
| Image Processing | OpenCV, ML Kit, Tesseract, EasyOCR |
| Auth | JWT (ECDSA P-256 or Ed25519 device keys, RBAC) |
| Container | Docker Compose (dev), Kubernetes (prod) |

## Core Security Invariants

These must never be compromised in any implementation:

1. **Immutability** — stored images and audit logs are append-only; no update or delete path should exist in the domain or infrastructure layer.
2. **Hash-then-sign** — SHA-256 of the raw image bytes is computed on-device before any network call; the server re-verifies on receipt and rejects mismatches.
3. **Digital signatures** — each capture is signed with the device's private key (ECDSA P-256 or Ed25519); the server verifies against the registered public key.
4. **Invisible watermark** — embedded in every stored image (exam ID, timestamp, nonce, hash, scanner ID); watermark destruction signals tampering.
5. **Read-only review** — manual reviewers can only record their interpreted answer; they cannot modify or replace the original image.
6. **Chain of custody** — every state transition (Student → Invigilator → Device → Server → OCR → Reviewer → Score) must be recorded and digitally signed.

## Domain Model (planned tables)

**Core:** `Exam`, `Student`, `AnswerSheet`, `Capture`, `OCRResult`, `Question`, `Answer`, `Score`, `AuditLog`, `Device`, `Signature`, `Verification`, `ManualReview`

**RBAC:** `User`, `Role`, `Permission`, `RolePermissions`, `UserRoles`, `UserClaims`, `DeviceCertificates`

**Security & Audit:** `SecurityEvents`, `LoginHistory`, `SessionTokens`

## API Shape

```
# Core exam pipeline
POST /capture           # Register a new capture with hash + signature
POST /upload            # Upload image bytes (must match registered capture)
GET  /verify/{id}       # Re-verify hash and signature
GET  /audit             # Query audit log (append-only)
POST /ocr               # Trigger OCR pipeline
POST /score             # Finalize scoring
GET  /results           # Published results
GET  /statistics        # Dashboard stats

# Authentication
POST /auth/login        # JWT login
POST /auth/refresh      # Refresh token
POST /auth/logout       # Revoke session
POST /auth/mfa/verify   # MFA verification

# RBAC management
GET/POST   /users                   # List / create users
GET/PUT    /users/{id}              # Get / update user
GET/POST   /roles                   # List / create roles
GET/PUT    /roles/{id}              # Get / update role
POST       /roles/{id}/permissions  # Assign permissions to role
POST       /users/{id}/roles        # Assign roles to user

# Device management
POST /devices/register  # Register a trusted device
PUT  /devices/{id}      # Approve / disable device

# Security & audit
GET /security/events    # Security event log
GET /security/sessions  # Active sessions
GET /audit/logs         # Full audit trail

# Public verification (anonymous)
GET /public/verify      # Verify QR / hash / signature
```

## RBAC Security Principles

The RBAC system enforces **Zero Trust Architecture**, **Least Privilege**, **Separation of Duties (SoD)**, and **Defense in Depth**. No single user should be capable of committing undetected examination fraud.

### User Roles

| # | Role | Key Capabilities | Hard Restrictions |
|---|------|-----------------|-------------------|
| 1 | **Super Administrator** | Manage users, configure system, view all reports | Cannot modify answer sheets, OCR results, or scores |
| 2 | **Security Administrator** | Verify signatures, rotate certificates, configure HSM, investigate alerts | Cannot edit exams, publish results, or score |
| 3 | **System Administrator** | Restart services, configure storage, backup database | Cannot view student answers or change exam data |
| 4 | **Exam Manager** | Manage exams, subjects, sessions, candidates, schedules | Cannot modify scanned images or scores |
| 5 | **Device Manager** | Register/approve/disable devices, monitor health, manage QR pairing | Cannot upload answer sheets or review exams |
| 6 | **Invigilator** *(mobile)* | Login, capture, upload, retry, view own history | Cannot delete/replace/edit images or view scores |
| 7 | **OCR Engine** *(system)* | Read images, detect bubbles, extract answers, calculate confidence | Cannot modify originals or publish results |
| 8 | **Manual Reviewer** | View original image, review OCR prediction, select answer interpretation | Cannot paint, erase, modify pixels, or delete sheets |
| 9 | **Review Supervisor** | Approve/reject review decisions, escalate disputes | Cannot edit images |
| 10 | **Scoring Engine** *(system)* | Calculate scores, generate rankings, produce statistics | Cannot modify answers or replace sheets |
| 11 | **Result Publisher** | Publish results, schedule release, generate official reports | Cannot change scores or edit answer sheets |
| 12 | **Auditor** *(read-only)* | Inspect audit logs, security logs, chain of custody, hash/sig verification, login/device history | Cannot modify any data |
| 13 | **Investigation Officer** | Compare images/hashes/signatures, download forensic reports, review timelines | Cannot modify evidence |
| 14 | **Student** | View personal results, download certificates, verify authenticity, submit review requests | Cannot view other students |
| 15 | **Public Verification User** *(anonymous)* | Verify QR code, SHA-256 hash, signature, timestamp, chain-of-custody status | Cannot access personal student information |

### Separation of Duties (SoD) Invariants

These pipeline constraints are enforced at the authorization layer — no role overlap allowed:

- Invigilators **capture** but never review
- Reviewers **review** but never score
- Scoring services **score** but never publish
- Publishers **publish** but never modify results
- Security Administrators **verify integrity** but never influence exam outcomes
- Auditors **inspect everything** but cannot modify anything

### Authentication Requirements

- JWT Authentication with Refresh Tokens
- Multi-Factor Authentication (MFA) for privileged roles
- OAuth2 / OpenID Connect support
- Device Registration + Device Certificates + Trusted Devices
- Session expiration and password policies

### Authorization Model

- Role-Based Access Control (RBAC) + Permission-Based + Policy-Based + Claims-Based
- Tenant isolation and fine-grained resource authorization
- Every privilege escalation requires approval

## Alert Triggers

Hash mismatch, invalid signature, duplicate upload, unexpected image replacement, suspicious login, excessive failed uploads → notify via Email, LINE Notify, Slack, Microsoft Teams, or configurable webhook.

## UI/UX Design System

The dashboard must feel like a **cybersecurity platform** (Microsoft Defender / Azure Portal / Grafana), not a school management system.

### Tech Stack (Dashboard)

React + TypeScript + Tailwind CSS + **shadcn/ui** — with reusable components, dark/light themes, and WCAG AA accessibility.

### Visual Style

- Dark Mode + Light Mode (default dark)
- Blue / Cyan accent colors, subtle glassmorphism, rounded cards, soft shadows
- Large high-contrast status indicators for verification and security events
- Severity colors: Green → Yellow → Orange → Red

### Global Layout

```
+----------------------------------------------------------+
| Top Nav: Logo | Search | Notifications | Help | Profile  |
+----------------------------------------------------------+
| Sidebar        | Main Content                 | Insights |
| (collapsible)  |                              | Alerts   |
|                |                              | Activity |
+----------------------------------------------------------+
```

### Sidebar Modules

`Dashboard` · `Examinations` · `Answer Sheets` · `Capture Sessions` · `OCR Queue` · `Manual Review` · `Scoring` · `Results` · `Audit Logs` · `Security Center` · `Device Management` · `Users` · `Roles & Permissions` · `Reports` · `Settings`

Sidebar supports: collapse, pin, favorites, search, recently visited.

### Key Screens

| Screen | Purpose | Notable Constraints |
|--------|---------|---------------------|
| **Dashboard** | Executive health overview — KPI cards, charts, quick actions | Read-only aggregates |
| **Manual Review** | Two-panel: original image + OCR predictions | Pixel Lock — no image editing; every decision creates immutable audit record |
| **Security Center** | SOC-style dashboard — tampering alerts, hash/sig failures, threat timeline | Read-only |
| **Audit Center** | Timeline view of full chain of custody | Append-only; export PDF / Excel / CSV |
| **Device Management** | Grid: device, owner, OS, trust status, last sync, location | Approve / Disable / Blacklist actions |
| **Role Management** | Tree view of roles + CRUD permission matrix (checkbox grid) | |
| **Student Portal** | Simple: result, certificate, answer-sheet verification, QR | No cross-student visibility |
| **Public Verification** | Anonymous QR / hash / signature verification page | No personal data displayed |

### Mobile App (Flutter) — Invigilator

Bottom nav: Capture · History · Uploads · Notifications · Profile

Capture flow: camera preview → document detection + edge highlight → auto-focus capture → perspective correction → image enhancement → hash generation → digital signature → upload with progress → verification status.

Offline support required — queue and sync when network available.

### UI Design Principles

1. Read-only presentation of original evidence everywhere it appears.
2. One-click access to the audit trail from every relevant screen.
3. Consistent trust badges, verification icons, and progress indicators.
4. Optimized for managing thousands of answer sheets — virtualized tables, bulk actions.
5. Keyboard-accessible navigation throughout.

### Core UI Components

`StatCard` · `AuditTimeline` · `DataGrid` · `KanbanQueue` · `ImageViewer` (zoom / rotate / brightness / contrast) · `VerificationBadge` · `CommandPalette` · `QRViewer` · `ProgressRing` · `Stepper` · `StatusChip` · `ToastNotifications`

---

## Development Setup (to be created)

Once the backend is scaffolded, the expected local stack will be:

```bash
docker compose up -d          # PostgreSQL, Redis, RabbitMQ, MinIO
dotnet build                  # Backend
dotnet test                   # All tests
dotnet run --project src/ExamShield.Api
```

For Flutter:
```bash
flutter pub get
flutter run                   # Mobile app
```

For the React dashboard:
```bash
npm install
npm run dev
```

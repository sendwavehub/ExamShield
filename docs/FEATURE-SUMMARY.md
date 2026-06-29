# ExamShield — Feature Summary

ExamShield is an enterprise-grade, AI-powered secure exam scanning and anti-tampering platform. Invigilators photograph paper answer sheets using a Flutter mobile app; every image is immediately hashed, digitally signed, encrypted, and stored immutably. An AI OCR pipeline reads the answers, low-confidence results are routed to a human review queue, and every state change is recorded in an append-only audit log.

---

## Core Security Guarantees

| Guarantee | How It Is Enforced |
|---|---|
| **Immutability** | Object storage is append-only; no update or delete path exists in the domain layer |
| **Hash-then-sign** | SHA-256 computed on-device before any network call; server re-verifies on receipt |
| **Digital signatures** | Each capture signed with the device's ECDSA P-256 private key; server verifies against registered public key |
| **Encryption at rest** | AES-256-GCM per-image DEK envelope-encrypted with a master key; raw MinIO/S3 access yields only ciphertext |
| **Invisible watermark** | Exam ID, timestamp, nonce, hash, and scanner ID embedded in every stored image; destroyed if image is edited |
| **Read-only review** | Reviewers select an interpreted answer; they cannot modify or replace the original image |
| **Chain of custody** | Every state transition signed and recorded: Student → Invigilator → Device → Server → OCR → Reviewer → Score |
| **Answer image allowlist** | Only 6 roles (Operator, Invigilator, Supervisor, ManualReviewer, ReviewSupervisor, InvestigationOfficer) may retrieve raw image bytes; admins get 403 |
| **MFA step-up** | InvestigationOfficer image access requires an MFA-verified JWT (`amr: mfa` claim) |

---

## Implemented Features

### 1. Authentication & Access Control

- JWT authentication with refresh tokens
- Multi-Factor Authentication (TOTP — Google Authenticator compatible)
  - Setup flow: generates QR code scannable by authenticator apps
  - Verify flow: 6-digit TOTP code with replay-attack prevention
  - Disable flow: requires re-verification
  - MFA step-up enforcement for InvestigationOfficer image access
- 15-role RBAC hierarchy (see Role Matrix below)
- Policy-based authorization — 11 named policies enforced at the API layer
- Invigilator scope enforcement — Invigilators and Operators can only access their own captures
- Session management — list active sessions, revoke individual sessions, revoke all sessions
- Password management — change password, forgot password / reset via email token
- Separated Duties enforced at the authorization layer

### 2. Device Management

- Device registration with public key (ECDSA P-256 SubjectPublicKeyInfo format)
- Admin approval / disable / blacklist workflow
- Device health heartbeat
- Device certificate lifecycle (issue, revoke)
- QR-code pairing flow from the mobile app

### 3. Exam Management

- Create, update, activate, close, delete exams
- Set total questions and optional metadata
- Enroll / unenroll students (individually or in bulk)
- Set and retrieve answer keys
- Exam submission status tracking

### 4. Capture Pipeline

- `POST /capture` — register a capture: validates ExamId, StudentId, DeviceId, page number, SHA-256 hash (hex), ECDSA signature; stamps InvigilatorId from JWT
- `POST /upload` — upload raw image bytes; server re-verifies hash and signature; embeds invisible watermark; AES-256-GCM encrypts before storing
- `GET /verify/{id}` — server-side re-verification of stored image integrity
- `GET /captures` — paginated list with filters (examId, status, deviceId, studentId); Invigilator/Operator sees only their own
- `GET /captures/{id}` — capture detail with hash, signature, status, storage key
- `GET /captures/{id}/image` — raw decrypted image bytes (ImageViewer policy + scope + optional MFA)
- `GET /captures/{id}/chain-of-custody` — full chain-of-custody timeline
- Flag capture as tampered (Administrator only)
- Export captures as CSV

### 5. OCR Pipeline

- Trigger OCR for a single capture (`POST /ocr`)
- Trigger batch OCR for all captures in an exam (`POST /ocr/batch`)
- OCR queue view with confidence scores and status
- Low-confidence results automatically routed to Manual Review queue
- Public verification endpoint re-verifies watermark and hash integrity

### 6. Manual Review Workflow

- Review queue — pending items with OCR confidence and original image
- Two-panel review UI: original image (read-only, zoom/rotate/brightness/contrast) + OCR predictions
- Submit review decision (select interpreted answer)
- Approve / reject review decisions (Review Supervisor)
- Escalate disputed decisions
- Review requests from students
- Every decision creates an immutable audit record

### 7. Scoring & Results

- Score a capture from finalized OCR answers (manual or auto)
- Batch scoring for entire exams
- Answer key comparison with per-question breakdown
- Exam rankings and statistics
- Result publication workflow (Result Publisher role)
- Student results portal — personal results, certificate download
- Exam statistics and report generation
- Rankings page with percentile data

### 8. Audit & Security

- Append-only audit log — every state transition recorded with userId, IP, device, action, timestamp, digital signature
- Audit log query with filters; export as PDF/Excel/CSV
- Security event log — hash mismatches, invalid signatures, suspicious logins, excessive failures
- Security Center dashboard (SOC-style)
- Login history
- Full chain-of-custody per capture
- Public verification endpoint — anonymous QR / hash / signature verification (no personal data)

### 9. Alert System

- Alert triggers: hash mismatch, invalid signature, duplicate upload, suspicious login, excessive failed uploads
- Configured per-channel; delivery channels partially wired (infrastructure stubs exist for Email, Slack, LINE Notify, Teams, webhook)
- Test alert endpoint for channel verification

### 10. Admin Dashboard (React + TypeScript + Tailwind + shadcn/ui)

| Screen | Key Actions |
|---|---|
| Dashboard | KPI cards, capture pipeline health, recent activity |
| Examinations | CRUD, activate/close, enroll students, answer keys |
| Answer Sheets | Paginated capture list, image viewer, hash/sig status |
| Capture Sessions | Filter by exam/device/status, export CSV |
| OCR Queue | Confidence-sorted queue, trigger OCR, view results |
| Manual Review | Two-panel image + OCR; submit, approve, reject, escalate |
| Scoring | Score individual or batch; view breakdowns |
| Rankings | Exam leaderboard with percentiles |
| Results | Published results; student lookup |
| Audit Logs | Timeline view; export |
| Security Center | SOC dashboard; security events; threat indicators |
| Device Management | Register, approve, disable, blacklist devices |
| Users | CRUD; role assignment; deactivate/reactivate |
| Roles | Role list; permission matrix |
| Reports | Summary reports; export |
| Settings | Notification channels; system settings |
| Student Portal | Personal results, certificate, review requests |
| Public Verification | Anonymous hash / QR / signature verification |
| Profile / MFA | TOTP setup; password change; session list |

### 11. Mobile App (Flutter — Android & iOS)

| Screen | Feature |
|---|---|
| Login | Email + password; TOTP MFA |
| MFA Setup | QR code pairing with authenticator app |
| Capture | Camera preview; auto-capture; hash + ECDSA signature on-device |
| History | Upload history per invigilator |
| Sync | Offline queue — captures queued locally and synced when network available |
| Device Registration | Public key generation and server pairing |
| Profile | View profile; logout |
| Notifications | Push notification history |

---

## Role Matrix

| Role | Capture | Upload | View Image | OCR | Review | Score | Publish | Audit | Manage Users | Manage Devices | Manage Exams |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| Super Administrator | — | — | — | — | — | — | — | R | ✓ | ✓ | ✓ |
| Security Administrator | — | — | — | — | — | — | — | R | — | ✓ | — |
| System Administrator | — | — | — | — | — | — | — | R | — | ✓ | — |
| Exam Manager | — | — | — | — | — | — | — | — | — | — | ✓ |
| Device Manager | — | — | — | — | — | — | — | — | — | ✓ | — |
| **Invigilator** | ✓ | ✓ | Own only | — | — | — | — | — | — | — | — |
| Operator | ✓ | ✓ | Own only | — | — | — | — | — | — | — | — |
| OCR Engine | — | — | ✓ | ✓ | — | — | — | — | — | — | — |
| Manual Reviewer | — | — | ✓ | — | ✓ | — | — | — | — | — | — |
| Review Supervisor | — | — | ✓ | — | Approve | — | — | — | — | — | — |
| Scoring Engine | — | — | — | — | — | ✓ | — | — | — | — | — |
| Result Publisher | — | — | — | — | — | — | ✓ | — | — | — | — |
| Supervisor | — | — | ✓ | — | — | — | — | R | ✓ | — | — |
| Auditor | — | — | — | — | — | — | — | R | — | — | — |
| **Investigation Officer** | — | — | ✓ (MFA) | — | — | — | — | R | — | — | — |
| Student | — | — | — | — | — | — | — | — | — | — | — |
| Public | — | — | — | — | — | — | — | — | — | — | — |

R = read-only; ✓ = write; — = denied

---

## Separation of Duties Invariants

These pipeline constraints are enforced at the authorization layer — no role overlap is permitted:

1. Invigilators **capture** but never review
2. Reviewers **review** but never score
3. Scoring services **score** but never publish
4. Publishers **publish** but never modify results
5. Security Administrators **verify integrity** but never influence exam outcomes
6. Auditors **inspect everything** but cannot modify anything

---

## Technology Stack

| Layer | Technology |
|---|---|
| Backend API | ASP.NET Core 10, C#, Clean Architecture, CQRS (MediatR), DDD |
| Database | PostgreSQL + EF Core migrations |
| Object Storage | MinIO (dev) / AWS S3 Object Lock / Azure Immutable Blob (prod) |
| Messaging | RabbitMQ (OCR dispatch) |
| Cache | Redis |
| Image Encryption | AES-256-GCM per-image DEK + master key (prod: KMS/Vault) |
| Auth | JWT (HS256), TOTP MFA, ECDSA P-256 device keys |
| Mobile | Flutter (Android + iOS) |
| Admin UI | React + TypeScript + Tailwind CSS + shadcn/ui |
| Containerisation | Docker Compose (dev), Kubernetes (prod) |
| Tests | xUnit + FluentAssertions (1,289 unit + 520 integration) + Vitest (397 frontend) |

---

## API Surface (101 endpoints)

```
POST   /auth/login                          # Login (returns JWT + refresh token)
POST   /auth/refresh                        # Refresh access token
POST   /auth/logout                         # Revoke current session
POST   /auth/forgot-password                # Request password reset email
POST   /auth/reset-password                 # Complete password reset

POST   /mfa/setup                           # Begin TOTP setup (returns QR URI)
POST   /mfa/verify                          # Verify TOTP code to activate MFA
POST   /mfa/login                           # Login with TOTP (returns amr:mfa JWT)
POST   /mfa/disable                         # Disable MFA for account

GET    /sessions                            # List active sessions
DELETE /sessions/{id}                       # Revoke a session
DELETE /sessions                            # Revoke all sessions

GET    /profile                             # Get own profile
PUT    /profile                             # Update profile
POST   /profile/change-password             # Change password

POST   /capture                             # Register capture (hash + signature)
POST   /upload                              # Upload image bytes
GET    /captures                            # List captures (scoped for Invigilator)
GET    /captures/{id}                       # Capture detail
GET    /captures/{id}/image                 # Raw image bytes (ImageViewer policy)
GET    /captures/{id}/chain-of-custody      # Chain-of-custody timeline
GET    /captures/export                     # Export as CSV
POST   /capture/{id}/verify                 # Re-verify integrity
POST   /captures/{id}/flag-tampered         # Flag as tampered (Administrator)

GET    /verify/{id}                         # Server-side integrity verification
GET    /public/verify                       # Anonymous public verification

POST   /ocr                                 # Trigger OCR for a capture
POST   /ocr/batch                           # Batch OCR for an exam
GET    /ocr/queue                           # OCR queue
GET    /ocr/{captureId}                     # OCR result for a capture

GET    /reviews/pending                     # Manual review queue
GET    /reviews/{id}                        # Review detail with image
POST   /reviews/{id}/submit                 # Submit review decision
POST   /reviews/{id}/approve                # Approve decision (Supervisor)
POST   /reviews/{id}/reject                 # Reject decision (Supervisor)
POST   /reviews/{id}/escalate               # Escalate dispute

GET    /review-requests                     # Student review requests
POST   /review-requests                     # Submit a review request (Student)
POST   /review-requests/{id}/resolve        # Resolve request
POST   /review-requests/{id}/reject         # Reject request

POST   /score                               # Score a capture
POST   /score/batch                         # Batch score an exam
GET    /scoring/queue                       # Scoring queue
GET    /score/{captureId}/breakdown         # Per-question score breakdown

POST   /results/publish                     # Publish exam results
GET    /results                             # Published results (with filters)
GET    /results/rankings                    # Exam rankings
GET    /results/student                     # Student's own results

GET    /exams                               # List exams
POST   /exams                               # Create exam
GET    /exams/{id}                          # Exam detail
PUT    /exams/{id}                          # Update exam
DELETE /exams/{id}                          # Delete exam
PUT    /exams/{id}/activate                 # Activate exam
PUT    /exams/{id}/close                    # Close exam
GET    /exams/{id}/candidates               # Enrolled students
POST   /exams/{id}/enroll                   # Enroll a student
DELETE /exams/{id}/unenroll/{studentId}     # Unenroll a student
POST   /exams/{id}/bulk-enroll              # Bulk enroll students
GET    /exams/{id}/answer-key               # Get answer key
POST   /exams/{id}/answer-key               # Set answer key
GET    /exams/{id}/statistics               # Exam statistics
GET    /exams/{id}/report                   # Exam report
GET    /exams/{id}/submission-status        # Upload/OCR/scoring status per student

GET    /devices                             # List devices
POST   /devices                             # Register device
GET    /devices/{id}                        # Device detail
PUT    /devices/{id}/approve                # Approve device
PUT    /devices/{id}/disable                # Disable device
PUT    /devices/{id}/blacklist              # Blacklist device
POST   /devices/{id}/heartbeat              # Device heartbeat
GET    /devices/{id}/certificates           # Device certificates
POST   /devices/{id}/certificates           # Issue certificate
DELETE /devices/{id}/certificates/{certId} # Revoke certificate

GET    /users                               # List users
POST   /users                               # Create user
GET    /users/{id}                          # User detail
PUT    /users/{id}/role                     # Change user role
POST   /users/{id}/deactivate              # Deactivate user
POST   /users/{id}/reactivate              # Reactivate user

GET    /roles                               # List roles
GET    /audit                               # Audit log
GET    /audit/export                        # Export audit log
GET    /audit/verify                        # Verify audit chain integrity

GET    /security/events                     # Security event log
GET    /security/sessions                   # All active sessions (admin)
GET    /security/login-history              # Login history
POST   /alerts/test                         # Test alert delivery channel

GET    /certificates/{studentId}            # Issue/view certificate
GET    /statistics                          # System-wide statistics
GET    /dashboard                           # Dashboard KPIs
GET    /reports/summary                     # Report summary
GET    /reports/export/users                # Export users
GET    /reports/export/exams                # Export exams
GET    /reports/export/scores               # Export scores

GET    /settings                            # System settings
PUT    /settings                            # Update settings
GET    /settings/notifications              # Notification channel settings
PUT    /settings/notifications              # Update notification settings
```

# ExamShield — Auditor & Investigation Officer Guide

**Roles covered:** Auditor, Investigation Officer
**Platform:** React web dashboard
**Version:** 1.0

---

## Overview

Auditors and Investigation Officers have read-only access to ExamShield's full audit infrastructure. Neither role can modify any data. The difference is scope:

| Role | Primary Function | Image Access | Forensic Reports |
|---|---|---|---|
| **Auditor** | Routine compliance inspection — audit logs, security events, chain of custody | No | No |
| **Investigation Officer** | Active fraud investigation — image comparison, hash/signature analysis, forensic timeline | Yes (with MFA) | Yes |

---

## 1. Logging In

Standard login for both roles. Investigation Officers **must complete MFA** before accessing answer sheet images — the system enforces this at the API level. If you attempt to view an image without an MFA-verified session, you will see a 403 error with the message "mfa_required". Re-authenticate using your TOTP code to obtain an MFA-qualified session.

---

## 2. Auditor Workflow

### 2.1 Audit Log

Go to **Audit Logs** in the sidebar.

The audit log is an append-only ledger of every action taken in the system. Each entry contains:

| Field | Description |
|---|---|
| Timestamp | Millisecond-precision UTC timestamp |
| Actor | User ID, name, and role |
| IP Address | Source IP of the request |
| Device ID | Device used (if applicable) |
| Action | What was done (e.g., ImageCaptured, SignatureVerified, ReviewSubmitted) |
| Resource | The entity affected (Capture ID, Exam ID, User ID, etc.) |
| Result | Success or Failure |
| Signature | Digital signature of this log entry (verifiable) |

**Filters:**
- Date range (from / to)
- Actor (search by user ID or email)
- Action type
- Resource type (exam, capture, user, device)
- Result (Success / Failure)

**Exporting:**
Click **Export** to download as PDF, Excel, or CSV. The export includes all currently filtered records.

**Chain Verification:**
Click **Verify Audit Chain** to run a cryptographic check that no log entries have been modified or deleted. This computes a rolling hash over the log sequence and compares it to the stored chain hash. A "Chain Valid" result means the log is intact.

### 2.2 Security Events

Go to **Security Center**.

Security events are triggered automatically when the system detects anomalies:

| Event Type | Trigger |
|---|---|
| Hash Mismatch | Stored image hash no longer matches the original |
| Signature Invalid | ECDSA verification failed for a capture |
| Duplicate Upload | The same capture was uploaded twice |
| Excessive Failures | Too many failed upload attempts from one device or account |
| Suspicious Login | Multiple failed logins or login from a new geography |
| MFA Bypass Attempt | MFA-required endpoint accessed without valid `amr: mfa` claim |

Each event has a **Severity** (Critical / High / Medium / Low), affected resource, source IP, and timestamp.

**Filtering by severity** is available. Use the **Critical** filter to focus on active threats.

### 2.3 Login History

Go to **Security Center → Login History**.

Inspect authentication events for any user:
- Successful logins (timestamp, IP, device)
- Failed login attempts
- MFA completions and failures
- Session revocations

Use this to answer questions like: "Was this account accessed from an unusual location on the date in question?"

### 2.4 Chain of Custody

To inspect the full chain of custody for a specific answer sheet:

1. Go to **Answer Sheets**, find the capture by Capture ID or student/exam.
2. Click the capture row, then click **Chain of Custody**.

The chain-of-custody timeline shows every event in order:

```
[1] ImageCaptured     — 2026-06-15 08:14:22 UTC — InvigilatorId: X — Device: Y
[2] Uploaded          — 2026-06-15 08:14:25 UTC — HashVerified: true — SigVerified: true
[3] OcrStarted        — 2026-06-15 08:15:00 UTC — Engine: TriggerOcr
[4] OcrCompleted      — 2026-06-15 08:15:04 UTC — Confidence: 91%
[5] ManualReview      — 2026-06-15 09:45:11 UTC — Reviewer: Z — Submitted
[6] ReviewApproved    — 2026-06-15 10:02:33 UTC — Supervisor: W
[7] Scored            — 2026-06-15 10:05:00 UTC — Score: 87/100
[8] ResultsPublished  — 2026-06-15 14:00:00 UTC
```

Each event references its audit log ID for cross-referencing.

---

## 3. Investigation Officer Workflow

Investigation Officers have all Auditor capabilities plus the ability to access raw image bytes for forensic comparison, as long as they have an MFA-verified session.

### 3.1 MFA Requirement

Before accessing any image:
1. Log in normally.
2. When prompted, complete your TOTP code to elevate to an MFA-verified session.
3. The system adds `amr: mfa` to your JWT. This session remains valid for the JWT expiration window (typically 60 minutes).
4. After expiry, you must re-authenticate with MFA to regain image access.

### 3.2 Viewing Answer Sheet Images

1. Go to **Answer Sheets**.
2. Find the capture under investigation.
3. Click **View Image**.
4. The original, decrypted answer sheet image loads in the ImageViewer.

ImageViewer controls available to Investigation Officers:
- Zoom (up to 800%)
- Rotate
- Brightness / Contrast / Sharpen
- Fullscreen

> The image is served as decrypted bytes directly from the server. It is never re-encrypted or modified for display.

### 3.3 Hash & Signature Verification

From the capture detail page:

1. Click **Verify Integrity**.
2. The server re-computes the SHA-256 hash from the stored (decrypted) image and compares it to the registered hash.
3. The ECDSA signature is re-verified against the device's registered public key.

**Interpretation of results:**

| Result | Meaning |
|---|---|
| Hash Valid + Signature Valid | Image is authentic and unmodified |
| Hash Invalid + Signature Valid | Image was replaced after upload but signature record was preserved — evidence of targeted tampering |
| Hash Valid + Signature Invalid | Signature record was altered — evidence of record manipulation |
| Both Invalid | Widespread tampering — treat as a critical incident |

### 3.4 Forensic Timeline

For complex cases involving multiple captures or users:

1. Go to **Audit Logs**.
2. Filter by the relevant Capture ID(s), date range, and actors.
3. Export the filtered log as PDF for your formal report.

The exported report includes:
- A header with the export timestamp and your user ID (for chain of custody of the report itself)
- All filtered audit events in chronological order
- The audit chain hash at the time of export

### 3.5 Comparing Captures

If you suspect a student's sheet was substituted:

1. Get the Capture ID from the audit log at the time of upload.
2. Retrieve the capture's **registered hash** (stored in the `Captures` table, visible on the capture detail page).
3. Use **Verify Integrity** to re-verify the currently stored image against that registered hash.
4. If the hash no longer matches, a security event has already been raised. The timestamp of that event tells you when the mismatch was first detected.

### 3.6 Device Forensics

To investigate a specific device:

1. Go to **Device Management**, find the device.
2. Check the **Certificate History** — any certificate revocations or re-issues.
3. In **Audit Logs**, filter by Device ID to see all captures made by this device.
4. In **Security Center**, filter events by Device ID to see any anomalies.

---

## 4. Common Investigation Scenarios

### Scenario A: Student Claims Their Sheet Was Modified

1. Retrieve the capture via **Answer Sheets → Capture Detail**.
2. Run **Verify Integrity** — if hash is valid, the image has not been modified since upload.
3. Check the **Chain of Custody** to confirm no anomalous access or events.
4. Download the capture's registration record (hash + signature + timestamp) as evidence.
5. If all checks pass, document that the sheet is cryptographically intact.

### Scenario B: A Hash Mismatch Alert Was Raised

1. Go to **Security Center**, find the hash mismatch event.
2. Note the Capture ID, timestamp, and source of detection.
3. Go to the capture detail — the Status field will show "Tampered".
4. Run **Verify Integrity** to confirm.
5. Check **Audit Logs** for any access events between upload and the mismatch detection.
6. Escalate to Security Administrator for incident response.

### Scenario C: Suspicious Invigilator Activity

1. Go to **Audit Logs**, filter by the invigilator's user ID.
2. Look for patterns: uploads late at night, duplicate upload attempts, rapid sequential captures.
3. Go to **Capture Sessions**, filter by the invigilator's ID and date.
4. Cross-reference capture timestamps against the official exam schedule.
5. For each suspicious capture, verify integrity and view the image (requires MFA session).

### Scenario D: Device Lost / Possibly Cloned

1. Go to **Device Management**, find the device.
2. Check the device's **Public Key** — this is the ECDSA P-256 public key registered at device pairing.
3. In **Audit Logs**, check for captures using this device after the suspected loss date.
4. Any captures from the device after the reported loss should have their signatures re-verified.
5. Recommend that the Device Manager **Blacklist** the device immediately.

---

## 5. Report Generation

Investigation Officers can download forensic report packages:

1. Go to **Reports → Forensic Report**.
2. Enter the Capture ID(s) or Exam ID under investigation.
3. Select date range.
4. Click **Generate Report**.

The report package includes:
- Capture registration records (hash, signature, timestamp, device)
- Audit log extract for the relevant period
- Integrity verification results
- Security events associated with the captures
- Chain-of-custody timeline

The report is exported as a PDF with a unique report ID, generation timestamp, and your user ID as the generating officer.

---

## 6. What You Cannot Do

As an Auditor or Investigation Officer:
- You **cannot** modify any data — captures, OCR results, scores, audit records, or user accounts.
- You **cannot** delete, overwrite, or suppress any record.
- Every action you take — including viewing images — is itself recorded in the audit log.
- You are **audited while auditing**.

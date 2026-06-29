# ExamShield — Administrator & Supervisor Dashboard Manual

**Roles covered:** Administrator, Supervisor, Exam Manager, Operator, Result Publisher, System Administrator
**Platform:** React web dashboard
**Version:** 1.0

---

## Overview

The ExamShield dashboard is a cybersecurity-style web application for managing the full examination lifecycle — from exam creation through capture sessions, OCR processing, scoring, and result publication. It provides real-time visibility into pipeline health, security events, and audit trails.

---

## 1. Logging In

1. Open the dashboard URL in your browser.
2. Enter your **Email** and **Password**, then click **Sign In**.
3. If MFA is enabled on your account, enter the 6-digit TOTP code from your authenticator app.
4. You are taken to the **Dashboard** home screen.

### Session Management

- Your session expires automatically. You will be prompted to re-authenticate.
- To see and manage your active sessions, go to **Profile → Active Sessions**.
- To revoke all other sessions (e.g. after a suspected account compromise), click **Revoke All Other Sessions**.

---

## 2. Global Layout

```
+────────────────────────────────────────────────────────────┐
│ Logo   Search   Notifications   Help   Profile             │
+──────────────────+─────────────────────────────────────────┤
│ Sidebar          │ Main Content                            │
│ Dashboard        │                                         │
│ Examinations     │                                         │
│ Answer Sheets    │                                         │
│ Capture Sessions │                                         │
│ OCR Queue        │                                         │
│ Manual Review    │                                         │
│ Scoring          │                                         │
│ Rankings         │                                         │
│ Results          │                                         │
│ Audit Logs       │                                         │
│ Security Center  │                                         │
│ Device Mgmt      │                                         │
│ Users            │                                         │
│ Roles            │                                         │
│ Reports          │                                         │
│ Settings         │                                         │
└──────────────────┴─────────────────────────────────────────┘
```

The sidebar is collapsible. Click the hamburger icon to collapse it for more reading space. Module access is role-restricted — you will only see sections your role permits.

---

## 3. Dashboard (Home)

The home screen shows a real-time operational overview:

**KPI Cards:**
- Total captures today / this week
- Verified vs. pending vs. failed uploads
- OCR queue depth (items awaiting processing)
- Manual review queue (items awaiting human decision)
- Active security alerts
- Scoring completion percentage

**Charts:**
- Captures per hour (line chart)
- Verification status breakdown (doughnut)
- Top exams by capture volume (bar chart)
- Recent security events (timeline)

**Quick Actions:**
- Trigger batch OCR
- View pending reviews
- Jump to security events

---

## 4. Examinations

### 4.1 Viewing Exams

Go to **Examinations** to see all exams in a sortable, filterable table:
- Name, status (Draft / Active / Closed), total questions, candidate count, creation date
- Use the search bar to filter by name

### 4.2 Creating an Exam

1. Click **+ New Exam**.
2. Enter **Exam Name** (required), optional description, and **Total Questions**.
3. Click **Create**. The exam starts in **Draft** status.

### 4.3 Activating an Exam

Captures can only be registered against an **Active** exam.

1. Open the exam detail page.
2. Click **Activate Exam**.
3. Confirm. The exam status changes to **Active** and invigilators can begin capturing.

> You cannot activate an exam without setting its total question count.

### 4.4 Setting the Answer Key

1. Open the exam, go to the **Answer Key** tab.
2. Enter the correct answer (A/B/C/D) for each question number.
3. Click **Save Answer Key**.

The answer key is used by the scoring engine. It can be set before or after captures are uploaded, but must be set before batch scoring runs.

### 4.5 Enrolling Students

**Individual enrollment:**
1. Open the exam, go to the **Candidates** tab.
2. Click **Enroll Student**, enter the Student ID, click **Enroll**.

**Bulk enrollment:**
1. Click **Bulk Enroll**.
2. Paste or upload a list of Student IDs (one per line, or CSV).
3. Click **Enroll All**.

### 4.6 Closing an Exam

Once all captures are uploaded and scored, click **Close Exam** to prevent further uploads. Closed exams remain readable.

### 4.7 Exam Status & Submission Report

Open the **Submission Status** tab to see per-student progress:
- Captured / Not Captured
- Uploaded / Pending
- OCR status
- Scoring status

Use this to identify students whose sheets are missing or failed.

---

## 5. Answer Sheets

Go to **Answer Sheets** to browse all captures across all exams.

### Filtering

Use the filter bar to narrow results:
- **Exam** — select from dropdown
- **Status** — Pending, Uploaded, Verified, OCR Complete, Scored, Tampered
- **Device** — filter by device ID
- **Student** — filter by student ID

### Viewing a Capture

Click any row to open the capture detail panel:
- Hash (SHA-256, hex)
- ECDSA signature
- Verification status
- Timestamp, device, invigilator
- Storage key

Click **View Image** to open the raw decrypted image in the ImageViewer (if your role is in the ImageViewer allowlist).

> Administrators and Auditors do **not** have image access. This is by design — see the RBAC security model.

### Exporting

Click **Export CSV** to download the filtered capture list.

---

## 6. Capture Sessions

**Capture Sessions** shows captures grouped by upload session (by date, device, or invigilator).

Use this view to:
- Verify that all expected sheets for a session were uploaded
- Identify gaps (students whose sheets are missing)
- Export session data for offline reconciliation

---

## 7. OCR Queue

The OCR Queue shows captures that have been uploaded and are waiting for, or have completed, OCR processing.

**Status values:**

| Status | Meaning |
|---|---|
| Pending | Capture uploaded; OCR not yet triggered |
| Processing | OCR pipeline running |
| Complete | Answers extracted with high confidence |
| Low Confidence | Sent to Manual Review queue |
| Failed | OCR error; may need manual review |

### Triggering OCR

**Single capture:** Click the capture row, then click **Trigger OCR**.

**Batch (entire exam):** Click **Batch OCR**, select an exam, click **Run**. All uploaded captures for that exam are queued.

### Viewing OCR Results

Click a completed row to see:
- Per-question detected answer and confidence score (0–100%)
- Overall confidence rating
- Whether the capture was sent to manual review

---

## 8. Manual Review (Supervisor / Reviewer)

The Manual Review screen handles captures where OCR confidence fell below the configured threshold.

### Review Queue

The queue shows pending items sorted by age (oldest first). Each item shows:
- Capture ID and student
- Exam name
- OCR confidence score
- Time in queue

### Conducting a Review

1. Click a queue item to open the two-panel view:
   - **Left panel:** Original answer sheet image (read-only). You can zoom, rotate, and adjust brightness/contrast but cannot modify pixels.
   - **Right panel:** OCR predictions — for each question, the detected answer and confidence.
2. For each question where the OCR answer is uncertain, select the correct answer from the dropdown.
3. Add an optional **Note** explaining your interpretation.
4. Click **Submit Review**.

> Every submitted review creates an immutable audit record with your user ID, timestamp, and the answers you selected.

### Review Supervisor Actions

If you have the **Review Supervisor** role:
- **Approve** — confirms the reviewer's interpretation; sends to scoring queue.
- **Reject** — sends back to the reviewer with a comment.
- **Escalate** — marks for senior review or investigation.

---

## 9. Scoring

### 9.1 Score a Single Capture

1. Go to **Scoring**, click **Score Capture**.
2. Enter the Capture ID (or search by student/exam).
3. Click **Score**. The system compares the finalized OCR answers against the answer key.
4. The result shows: total correct, total wrong, blank, score percentage.

### 9.2 Batch Scoring

1. Click **Batch Score Exam**.
2. Select the exam from the dropdown.
3. Click **Run Batch Score**.
4. All captures with finalized OCR answers (reviewed or auto-passed) are scored in one operation.

### 9.3 Viewing Score Breakdown

Click any scored capture in the queue to see a per-question breakdown:
- Question number, student's answer, correct answer, result (Correct / Wrong / Blank)

---

## 10. Rankings

The **Rankings** page shows the leaderboard for a completed, scored exam:

1. Select the exam from the dropdown.
2. The table shows: Rank, Student ID, Score, Percentage, Percentile.
3. Use the search bar to find a specific student.
4. Export the rankings as CSV.

---

## 11. Results

### Publishing Results

1. Go to **Results**, select the exam.
2. Confirm all students have been scored (check the Submission Status if unsure).
3. Click **Publish Results**.
4. Results become visible to students in their portal and to public verification.

> Publishing is irreversible via normal workflow. Only an Administrator can unpublish (which generates an audit event).

### Viewing Published Results

After publication, the results table shows each student's score and rank. Filter by exam or search by student ID.

---

## 12. Audit Logs

Go to **Audit Logs** to inspect the append-only event timeline.

**Filters:**
- Date range
- User
- Action type (Captured, Uploaded, Verified, OCR Started, Manual Review, Scored, Published, etc.)
- Exam or capture

**Each log entry shows:**
- Timestamp
- Actor (user ID + role)
- IP address
- Device ID
- Action
- Resource (capture ID, exam ID, etc.)
- Result (Success / Failure)
- Digital signature (verifiable)

**Export:** Click **Export** to download as PDF, Excel, or CSV.

**Chain verification:** Click **Verify Audit Chain** to run a cryptographic check that no log entry has been modified or deleted since it was written.

---

## 13. Security Center

The Security Center provides a SOC-style view of security events.

**Event types:**
- Hash mismatch — stored hash no longer matches the image
- Signature invalid — ECDSA verification failed
- Duplicate upload attempt
- Suspicious login — multiple failed attempts or login from new location
- Excessive failed uploads from a device
- MFA bypass attempt

**Each event shows:** severity (Critical / High / Medium / Low), timestamp, affected resource, source IP, device, and action taken.

**Threat timeline:** A chronological view of all security events, useful during an investigation.

---

## 14. Device Management

### Registering a Device

1. Go to **Device Management**, click **+ Register Device**.
2. Enter the device name.
3. The invigilator will pair from the mobile app (they generate the public key; you approve).

### Approving a Device

New devices arrive in **Pending Approval** status.
1. Click the device row.
2. Review the device name, owner, and public key.
3. Click **Approve**. The invigilator's app will be notified.

### Disabling / Blacklisting a Device

- **Disable** — temporarily blocks the device. Can be re-enabled.
- **Blacklist** — permanently blocks the device. All future upload attempts from this device are rejected.

Use **Blacklist** immediately if a device is lost or stolen.

### Device Certificates

Each device can have certificates issued for additional trust verification. Go to the **Certificates** tab on the device detail page to issue or revoke certificates.

---

## 15. User Management

### Viewing Users

Go to **Users** to see all accounts in a searchable table: name, email, role, status (Active / Inactive).

### Creating a User

1. Click **+ New User**.
2. Enter name, email, and select a role.
3. Click **Create**. The user receives a password reset email to set their initial password.

### Changing a User's Role

1. Open the user detail page.
2. Click **Change Role**, select the new role, click **Save**.

> Role changes take effect on the user's next login. Active sessions retain the old role.

### Deactivating / Reactivating

- **Deactivate** — blocks login immediately; existing sessions are revoked.
- **Reactivate** — restores login access.

---

## 16. Roles

Go to **Roles** to see all 15 system roles and their descriptions. This page is read-only — the role hierarchy is enforced at the API level and cannot be modified through the UI.

---

## 17. Reports

The **Reports** module provides summary reports for administrators and supervisors:

- **Exam Summary** — capture count, OCR completion, manual review count, scoring completion, result publication status
- **Export Users** — full user list as CSV/Excel
- **Export Exams** — exam list with statistics
- **Export Scores** — all scores for an exam as CSV

---

## 18. Settings

### System Settings

Global settings accessible to Administrator roles:
- Confidence threshold for OCR auto-pass vs. manual review (default: 85%)
- JWT expiration window
- Session timeout
- Max upload size

### Notification Channels

Configure where security alerts are sent:

1. Go to **Settings → Notifications**.
2. Enable/disable individual channels: Email, Slack, LINE Notify, Microsoft Teams, Webhook.
3. Enter the relevant credentials or webhook URL for each channel.
4. Click **Test** to send a test alert and confirm delivery.

---

## 19. Profile & MFA

Click your name in the top-right corner to access your profile:

- **View Profile** — name, email, role, last login
- **Change Password** — enter current password and new password
- **MFA Setup** — enable TOTP MFA on your account (scan QR with authenticator app)
- **Active Sessions** — see and revoke your own sessions

---

## 20. Role-Based Access Reference

| Screen | Admin | Supervisor | Exam Manager | Operator | Result Publisher |
|---|:---:|:---:|:---:|:---:|:---:|
| Dashboard | ✓ | ✓ | ✓ | ✓ | ✓ |
| Examinations | ✓ | ✓ | ✓ | — | — |
| Answer Sheets | ✓ (no image) | ✓ | — | ✓ | — |
| Capture Sessions | ✓ | ✓ | — | ✓ | — |
| OCR Queue | ✓ | ✓ | — | ✓ | — |
| Manual Review | ✓ | ✓ | — | — | — |
| Scoring | ✓ | ✓ | — | ✓ | — |
| Results | ✓ | ✓ | — | — | ✓ |
| Audit Logs | ✓ | ✓ | — | — | — |
| Security Center | ✓ | ✓ | — | — | — |
| Device Management | ✓ | — | — | — | — |
| Users | ✓ | — | — | — | — |
| Reports | ✓ | ✓ | ✓ | — | ✓ |
| Settings | ✓ | — | — | — | — |

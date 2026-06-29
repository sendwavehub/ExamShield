# ExamShield — Invigilator User Manual

**Role:** Invigilator (field staff operating the mobile app)
**Platform:** Flutter mobile app (Android & iOS)
**Version:** 1.0

---

## Overview

As an Invigilator you are the first link in ExamShield's chain of custody. You use the mobile app to photograph answer sheets immediately after each student finishes. The app hashes, digitally signs, and uploads every image so the server can verify it was never modified.

You **cannot**:
- Delete or replace an uploaded image
- View scores or results
- Access other invigilators' captures

---

## 1. First-Time Setup

### 1.1 Device Registration

Your device must be registered and approved by a Device Manager before you can capture any answer sheets.

1. Open the ExamShield app.
2. On the **Device Registration** screen, tap **Register This Device**.
3. The app generates an ECDSA P-256 key pair on-device. The private key is stored in the device's secure enclave and never leaves the device.
4. A QR code is displayed — show it to your Device Manager or share the pairing code.
5. Wait for your Device Manager to approve the device in the dashboard. You will receive a push notification when approved.
6. Once approved, the **Login** screen appears.

> **Note:** If you change devices, you must register and get approval again. Captures from an unapproved device are rejected by the server.

### 1.2 Login

1. Enter your **Email** and **Password** and tap **Sign In**.
2. If your administrator has enabled MFA for your account, you are prompted to enter a 6-digit code from your authenticator app (Google Authenticator, Authy, etc.).
3. On successful login you are taken to the **Capture** tab.

### 1.3 MFA Setup (if required)

1. After first login, if MFA setup is required, a setup screen appears.
2. Tap **Show QR Code** and scan the displayed QR with your authenticator app.
3. Enter the 6-digit code from the app to confirm pairing.
4. MFA is now active on your account. Store your recovery codes safely.

---

## 2. Capturing an Answer Sheet

### 2.1 Pre-Capture Checklist

Before you begin a capture session:
- Confirm the exam is active in the system.
- Confirm your device is charged and connected (or you have started an offline session).
- Have the student's ID number ready.

### 2.2 Starting a Capture

1. From the bottom navigation, tap **Capture**.
2. Select the **Exam** from the dropdown list (only active exams appear).
3. Enter the **Student ID**. The app validates the student is enrolled in the selected exam.
4. Enter the **Page Number** (for multi-page answer sheets).
5. Tap **Open Camera**.

### 2.3 Taking the Photo

The camera preview activates with a live guide overlay:

- Hold the phone directly above the answer sheet.
- Keep the sheet within the guide corners.
- The app automatically detects the document edge when the sheet is in frame.
- A green outline appears when the document is detected correctly.
- Tap the **Capture** button (or let auto-capture trigger when the image is stable).

**Quality checks run automatically. If the image is rejected, you will see one of these messages:**

| Message | Action |
|---|---|
| "Too blurry — please retake" | Hold the phone steadier; wait for autofocus |
| "Image too dark" | Move to a better-lit area or use the flash toggle |
| "Overexposed" | Reduce light or move away from direct sunlight |
| "Corners missing — reframe and retake" | Ensure all four corners of the sheet are visible |
| "Camera moved during capture — retake" | Keep the phone still until the shutter fires |

### 2.4 Review & Confirm

After a successful capture, a preview screen shows:
- The corrected, enhanced image (perspective-corrected and contrast-adjusted)
- The SHA-256 hash (first 16 characters displayed)
- Capture timestamp

Tap **Confirm & Upload** to proceed, or **Retake** to take another photo.

> The original raw image is discarded after enhancement. The enhanced image is what gets hashed, signed, and stored.

### 2.5 What Happens After Confirmation

The app immediately:
1. Computes SHA-256 of the enhanced image bytes.
2. Signs the hash with the device's private key (ECDSA P-256).
3. Calls `POST /capture` to register the capture on the server.
4. Uploads the image bytes via `POST /upload`.
5. Displays a **Verified** badge once the server confirms hash and signature match.

The entire process takes 2–5 seconds on a good connection.

---

## 3. Offline Mode

If network connectivity is lost:

1. The app continues to capture, hash, and sign normally.
2. Each capture is saved to the local offline queue (SQLite).
3. A **Pending Sync** badge appears in the top bar showing the queue count.
4. Tap the **Sync** tab to see pending items and their status.
5. When connectivity is restored, tap **Sync Now** or wait for automatic sync.
6. Each item uploads in queue order. Successfully synced items are removed from the queue.

> Offline captures are cryptographically identical to online captures — they carry the same hash and signature. The server validates both on receipt.

---

## 4. Upload History

From the bottom navigation, tap **History** to see your captures:

- **Verified** — hash and signature confirmed by server
- **Pending** — queued for upload or upload in progress
- **Failed** — server rejected (hash mismatch, signature invalid, or duplicate); tap to see reason and retry

Tap any capture to see its detail: exam, student, timestamp, hash, verification status.

---

## 5. Retrying a Failed Upload

If an upload fails:
1. Go to **History** and tap the failed item.
2. Read the error reason. Common causes:
   - **Hash mismatch** — the image was corrupted in transit; recapture the sheet.
   - **Device not approved** — contact your Device Manager.
   - **Duplicate upload** — this sheet was already uploaded; no action needed.
3. Tap **Retry** if available, or recapture if the reason requires it.

---

## 6. Notifications

Tap the **Notifications** tab to see server-side messages:
- Upload confirmations
- OCR completion notices
- Device approval status changes
- System announcements from your administrator

---

## 7. Profile & Password

Tap **Profile** (bottom navigation):
- View your account details and role
- Log out
- Change your password

---

## 8. Security Reminders

- **Never share your password or MFA code** with anyone.
- **Do not leave the app unlocked** on an unattended device.
- **Do not photograph the same sheet twice** after a successful upload — the server will reject duplicates and raise a security alert.
- If your device is lost or stolen, immediately contact your **Device Manager** to disable the device.
- Your private key is stored in the device's secure enclave. It cannot be exported. Losing the device does not expose past captures.

---

## 9. Troubleshooting

| Problem | Action |
|---|---|
| Cannot log in | Check email/password; confirm your account is active with your supervisor |
| MFA code rejected | Ensure your phone clock is correct (TOTP is time-sensitive); use the next code |
| Camera won't open | Grant camera permission in device Settings → Apps → ExamShield |
| Upload stuck at "Uploading" | Check network; go to Sync tab and tap Sync Now |
| "Device not registered" error | Contact Device Manager to register and approve this device |
| Capture always blurry | Clean the camera lens; ensure good lighting; hold the phone 30–40 cm above the sheet |
| App crashes | Force-close and reopen; pending captures are saved and will resume |

---

## 10. End of Exam Session

At the end of a capture session:
1. Go to **History** and confirm all captures show **Verified** status.
2. If any show **Pending**, go to **Sync** and sync them before leaving.
3. If any show **Failed**, notify your supervisor before leaving.
4. Log out via **Profile → Sign Out**.

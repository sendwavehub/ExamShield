# ExamShield — Device Manager Guide

**Role:** Device Manager
**Platform:** React web dashboard
**Version:** 1.0

---

## Overview

The Device Manager role is responsible for the lifecycle of all capture devices — smartphones used by Invigilators to photograph answer sheets. Every device that uploads to ExamShield must be registered and approved before it can be used. This control prevents unauthorised devices from injecting counterfeit captures.

**You can:**
- Register new devices
- Approve or deny pending device registrations
- Disable devices temporarily (e.g., device is out for repair)
- Blacklist devices permanently (e.g., lost or stolen)
- Issue and revoke device certificates
- Monitor device health via heartbeats

**You cannot:**
- Upload answer sheets
- View answer sheet images
- Review or score exams
- Access student data

---

## 1. How Device Registration Works

ExamShield uses asymmetric cryptography to authenticate capture devices:

1. The Invigilator opens the ExamShield mobile app on a new device for the first time.
2. The app generates an **ECDSA P-256 key pair** inside the device's secure enclave. The **private key never leaves the device**.
3. The app sends the **public key** and device metadata (device name, OS version, app version) to the server via `POST /devices`.
4. The device appears in the **Device Management** screen with status **Pending Approval**.
5. You review and **Approve** it. From that point, every capture from this device includes an ECDSA signature verifiable against the registered public key.
6. The Invigilator's app receives an approval notification and can begin capturing.

---

## 2. Device Management Screen

Go to **Device Management** in the left sidebar.

The device list shows:
- **Device Name** — assigned by the Invigilator at registration
- **Owner / Invigilator** — the account that registered the device
- **OS & Version** — Android/iOS version and ExamShield app version
- **Trust Status** — Pending / Approved / Disabled / Blacklisted
- **Last Heartbeat** — last time the device pinged the server (indicates it is online and in use)
- **Captures** — total number of successful captures from this device
- **Registered** — registration date

Filter by status or search by device name or owner.

---

## 3. Approving a Device

1. Click the device row with status **Pending Approval**.
2. Review:
   - **Device Name** — matches what the Invigilator told you
   - **Invigilator account** — the correct person for this device
   - **Public Key fingerprint** — confirm with the Invigilator that the key fingerprint displayed on the app matches (prevents man-in-the-middle key substitution)
   - **App version** — should be the current approved version
3. Click **Approve**.
4. The device status changes to **Approved**. The Invigilator receives a push notification.

> Never approve a device without confirming the key fingerprint with the Invigilator in person or via a secure channel.

---

## 4. Disabling a Device

Use **Disable** for a temporary suspension — for example, when a device is sent for repair or an Invigilator is on leave.

1. Open the device detail page.
2. Click **Disable Device**.
3. Enter a reason (recorded in the audit log).
4. The device is immediately blocked. Any upload attempts from this device are rejected.
5. To re-enable, click **Enable Device**.

---

## 5. Blacklisting a Device

Use **Blacklist** for a permanent block — for example, when a device is lost, stolen, or suspected of compromise. Blacklisting cannot be undone through the UI.

1. Open the device detail page.
2. Click **Blacklist Device**.
3. Enter the reason (e.g., "Device reported lost — 2026-06-15").
4. Confirm. The device is permanently blocked.

> All existing captures from a blacklisted device remain in the system — only new uploads are blocked. Existing captures retain their signatures and hashes.

**After blacklisting a lost device:**
- The Invigilator must use a different device.
- The new device must go through the full registration and approval process.
- Review any captures made by the blacklisted device during the suspicious period using the Audit Log.

---

## 6. Device Health Monitoring

### Last Heartbeat

Devices send a periodic heartbeat (`POST /devices/{id}/heartbeat`) when the ExamShield app is running. The **Last Heartbeat** column shows when the device was last seen online.

A device that has not sent a heartbeat for more than 24 hours during an active exam period should be investigated — it may be offline, discharged, or experiencing a network issue.

### Capture Count

The **Captures** count shows total successful uploads from this device. An unusually high or low count during an exam session may indicate a problem.

---

## 7. Device Certificates

Device certificates provide an additional layer of trust for high-security deployments. A certificate is issued by ExamShield and is separate from the ECDSA key pair used for capture signatures.

### Issuing a Certificate

1. Open the device detail page.
2. Click the **Certificates** tab.
3. Click **Issue Certificate**.
4. Set an expiry date.
5. The certificate is issued and stored against the device record.

### Revoking a Certificate

1. Find the certificate in the **Certificates** tab.
2. Click **Revoke** next to the certificate.
3. The certificate is immediately marked as revoked. Any request presenting this certificate is rejected.

---

## 8. Audit Trail

Every device management action — registration, approval, disable, blacklist, certificate issuance/revocation — is recorded in the system's append-only audit log. The audit log records:
- Your user ID and role
- The timestamp of the action
- The device ID affected
- The action taken and the reason

Go to **Audit Logs** and filter by Device ID to see the full history for any device.

---

## 9. Device Manager Checklist

### Before an Exam Session

- [ ] Confirm all participating invigilators have registered devices.
- [ ] Confirm all devices are in **Approved** status.
- [ ] Confirm device app versions are up to date.
- [ ] Confirm all devices sent a heartbeat within the past hour.

### During an Exam Session

- [ ] Monitor the **Last Heartbeat** column for any devices that go offline.
- [ ] Watch for failed upload notifications in the Security Center (may indicate device issues).

### After an Exam Session

- [ ] Review capture counts per device vs. expected captures.
- [ ] Disable any devices used by invigilators who have finished their assignment.
- [ ] Blacklist any devices reported lost or stolen immediately.

### After a Device Is Reported Compromised

1. **Blacklist** the device immediately.
2. Note the date and time of the suspected compromise.
3. Go to **Audit Logs**, filter by this Device ID, and export the log for all captures made during the suspicious period.
4. Notify the Security Administrator to investigate whether any captures from this device need integrity re-verification.

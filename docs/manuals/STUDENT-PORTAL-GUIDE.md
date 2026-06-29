# ExamShield — Student Portal Guide

**Role:** Student
**Platform:** React web dashboard (Student Portal section)
**Version:** 1.0

---

## Overview

The Student Portal lets you view your own examination results, download your official certificate, verify the authenticity of your answer sheet, and submit a review request if you believe your answers were misread.

**You can only see your own results.** ExamShield enforces this at the server level — there is no way to access another student's data.

---

## 1. Logging In

1. Open the ExamShield Student Portal URL provided by your institution.
2. Enter your **Email** and **Password**, then click **Sign In**.
3. You are taken to your results dashboard.

If you have forgotten your password, click **Forgot Password**, enter your email address, and follow the instructions in the reset email.

---

## 2. Your Results

The results page shows all exams you participated in:

| Column | Description |
|---|---|
| Exam | Exam name |
| Date | Exam date |
| Score | Your raw score (correct answers) |
| Percentage | Score as a percentage of total questions |
| Rank | Your rank among all students in this exam |
| Status | Published / Pending (results not yet released) |

Click any row to open the **Exam Detail** view.

### Exam Detail

- **Per-question breakdown** — your detected answer vs. the correct answer, question by question
- **Score summary** — total correct, wrong, blank, and final percentage
- **Verification status** — whether your answer sheet's hash and signature have been verified

---

## 3. Downloading Your Certificate

Once results are published, a **Download Certificate** button appears on the exam detail page.

The certificate is a PDF containing:
- Your name and student ID
- Exam name and date
- Your score and rank
- A unique certificate ID
- A QR code for public verification

The certificate is generated and signed by ExamShield. Anyone can verify its authenticity using the Public Verification page (see Section 5).

---

## 4. Answer Sheet Verification

You can verify that your answer sheet was never tampered with after it was uploaded.

1. From the exam detail page, click **Verify Answer Sheet**.
2. The system re-checks:
   - **Hash integrity** — the stored image hash matches the original hash registered at upload
   - **Digital signature** — the ECDSA signature created by the invigilator's device is still valid
   - **Watermark** — the invisible watermark embedded in your image is intact
3. A result is shown:

| Indicator | Meaning |
|---|---|
| Hash Valid | The image has not been modified since upload |
| Signature Valid | The image was captured by a registered, approved device |
| Watermark Intact | The embedded metadata is consistent with the upload record |
| Tampered | One or more checks failed — a security alert has been raised |

> If any check fails, this is a serious finding and has already been reported to the Security team. Contact your institution's examinations office.

---

## 5. Public Verification (Anonymous)

Anyone — including third parties such as universities or employers verifying your result — can verify your answer sheet without logging in.

Open the **Public Verification** page (no login required):

You can verify by:
- **Capture ID** — paste the ID from your certificate
- **SHA-256 Hash** — paste the hash from your certificate
- **QR Code** — scan the QR printed on your certificate

The verification result shows:
- Whether the image hash is valid
- Whether the digital signature is valid
- The capture timestamp
- The exam name and date

**No personal information** (name, score, student ID) is displayed on the public verification page. Only integrity status and timing are shown.

---

## 6. Submitting a Review Request

If you believe the OCR engine misread one or more of your answers, you can submit a formal review request.

1. Go to the exam detail page.
2. Click **Request Review**.
3. In the form, specify:
   - Which question numbers you are disputing
   - What you believe the correct answer is for each
   - Any supporting explanation (e.g., "I circled B clearly; the answer key shows C")
4. Click **Submit Request**.

**What happens next:**
- Your request is queued for a Review Supervisor.
- The supervisor will examine your original answer sheet image (which you can also see) and your OCR result.
- You will receive a notification when your request is resolved (Approved or Rejected) with a reason.

**Limits:**
- You can submit one review request per exam.
- Review requests must be submitted within the review window defined by your institution (typically 5–14 days after result publication).

---

## 7. How Your Answer Sheet Is Protected

ExamShield was designed so that even system administrators cannot alter your answer sheet after it has been uploaded. Here is what happens to your sheet:

1. **Photographed** — the invigilator captures your sheet with the ExamShield mobile app.
2. **Hashed** — the app computes a SHA-256 fingerprint of the image on the device itself, before any network connection.
3. **Signed** — the app signs the hash with a device-specific private key that never leaves the device.
4. **Uploaded** — the image, hash, and signature are sent to the server.
5. **Verified** — the server re-computes the hash and verifies the signature. If either fails, the upload is rejected.
6. **Encrypted** — the image is AES-256-GCM encrypted before being stored. Direct access to storage yields only ciphertext.
7. **Watermarked** — an invisible watermark containing the exam ID, timestamp, and nonce is embedded in the image.
8. **Immutable** — once stored, the image can never be overwritten or deleted.

Any modification attempt — at any layer — would cause the hash or signature verification to fail, triggering an immediate security alert to the Security team.

---

## 8. Privacy

- Only you can see your results, scores, and answer sheet via the student portal.
- The public verification page shows only integrity status (hash/signature valid or not), not your personal information or answers.
- Audit logs record every access to your answer sheet, including who viewed it and when.

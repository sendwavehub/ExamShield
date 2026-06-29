# ExamShield — Manual Reviewer & Review Supervisor Guide

**Roles covered:** Manual Reviewer, Review Supervisor
**Platform:** React web dashboard
**Version:** 1.0

---

## Overview

The Manual Review module handles captures where the OCR engine could not read the answer sheet with sufficient confidence. As a Manual Reviewer your job is to look at the original image and determine the correct answer for each uncertain question.

**You can:**
- View the original answer sheet image (read-only)
- See the OCR engine's answer prediction and confidence score per question
- Submit your interpretation of the correct answer
- Add a note explaining your reasoning

**You cannot:**
- Modify, draw on, erase, or replace the image in any way
- Access other exam data beyond your assigned review queue
- Delete answer sheets or captures
- Change scores directly

Every action you take creates an immutable, digitally signed audit record.

---

## 1. Accessing the Review Queue

1. Log in at the dashboard URL.
2. In the left sidebar, click **Manual Review**.
3. The queue shows all captures pending review, sorted oldest-first.

**Queue columns:**
- Capture ID
- Exam name
- Student ID (anonymised as a reference number in some configurations)
- OCR confidence score (the reason this capture is here)
- Time in queue (how long it has been waiting)
- Assigned reviewer (if your supervisor assigns reviews)

---

## 2. Understanding the Two-Panel Interface

When you open a review item, the screen splits into two panels:

### Left Panel — Original Image (Read-Only)

The original answer sheet image is displayed here. Controls available:

| Control | Function |
|---|---|
| Zoom In / Out | Magnify areas of the sheet |
| Rotate | Rotate 90° clockwise or counter-clockwise |
| Brightness | Increase/decrease brightness for dark images |
| Contrast | Increase/decrease contrast to make bubbles clearer |
| Reset | Return to default view |
| Fullscreen | Expand image to the full browser window |

> These controls only change how the image is displayed on your screen. They do not modify the stored image. The original bytes are cryptographically sealed.

### Right Panel — OCR Predictions

A table listing each question with:
- **Question #** — question number on the sheet
- **Detected Answer** — what the OCR engine read (A / B / C / D / Blank / Multiple)
- **Confidence** — how certain the engine was (shown as a percentage and colour: green ≥ 90%, yellow 70–89%, red < 70%)
- **Your Answer** — a dropdown for you to select the correct answer

Only questions with **low confidence** (below the threshold) require your input. High-confidence questions are shown for context but are pre-filled and locked.

---

## 3. Conducting a Review

### Step 1 — Examine the Image

Open the image and scan the entire sheet before making decisions. Look for:
- Bubbles that are partially filled
- Erasure marks that may indicate a changed answer
- Double-marked questions (two bubbles filled)
- Stray marks that could be mistaken for selections

Use the zoom and brightness/contrast controls if any area is hard to read.

### Step 2 — Fill in Your Answers

For each question in the low-confidence list:
1. Examine the corresponding bubble area on the image.
2. Select the answer you believe is correct from the dropdown: **A, B, C, D, Blank, or Multiple Selections**.
3. If you see two filled bubbles and cannot determine intent, select **Multiple Selections** — this will trigger supervisor review.

### Step 3 — Add a Note (Optional but Recommended)

Click the **Note** field and describe anything unusual:
- "Q3 — erasure visible; B appears selected after erasure"
- "Q7 — mark is very faint; selected Blank as no clear fill"
- "Q12 — two bubbles filled; cannot determine intent"

Notes are stored in the audit record and visible to your supervisor.

### Step 4 — Submit

Click **Submit Review**. A confirmation dialog appears showing:
- Your selected answers for each low-confidence question
- Your note

Click **Confirm & Submit** to save. The capture moves to the **Review Supervisor** queue for approval.

> Once submitted, you cannot edit your review. If you made an error, contact your Review Supervisor to reject and return it.

---

## 4. Edge Cases

### No Clear Answer (Blank)

If a question has no mark at all, select **Blank**. This is a valid response — it may score as zero, but it accurately represents the sheet.

### Double-Marked Question

If two bubbles appear filled, select **Multiple Selections**. The scoring engine will treat this as an invalid response (typically scoring zero for that question) and your supervisor will be flagged to review.

### Damaged or Obscured Section

If part of the sheet is torn, water-damaged, or otherwise unreadable:
- Select **Blank** for the affected questions.
- Add a detailed note: "Questions 8–10 obscured by water damage; unable to determine answers."
- Your supervisor may escalate the case for investigation.

---

## 5. Review Supervisor Workflow

If you hold the **Review Supervisor** role, you have additional actions for submitted reviews:

### Approve

The reviewer's interpretation is correct. The capture advances to the scoring queue.

1. Open the review item.
2. Examine the image and the reviewer's answers.
3. Click **Approve**.

### Reject

The reviewer made an error. The capture returns to the review queue with your feedback.

1. Click **Reject**.
2. Enter a comment explaining what was wrong.
3. The item returns to the reviewer with your comment visible.

### Escalate

The case is disputed, complex, or requires investigation beyond normal review:

1. Click **Escalate**.
2. Select a reason: Disputed Answer / Possible Tampering / Damaged Sheet / Other.
3. Add a note.
4. The capture is flagged in the system and an Investigation Officer or senior supervisor is notified.

---

## 6. Audit Trail

Every action you take — opening a review, submitting answers, a supervisor approving or rejecting — is recorded in the system's append-only audit log. This includes:

- Your user ID and role
- The exact timestamp (millisecond precision)
- The capture ID
- Your selected answers
- Your note

The audit record is digitally signed and cannot be modified after the fact. This log forms part of the legal chain of custody for the examination.

To view the audit trail for a specific capture, click **Chain of Custody** on the capture detail page (requires Auditor or Supervisor role to access the full timeline).

---

## 7. Tips for Accurate Review

1. **Zoom in** before making a decision — what looks like a filled bubble at 100% zoom may be a stray mark at 300%.
2. **Use brightness/contrast** for photocopied sheets or dimly lit captures.
3. **Be consistent** — if your exam uses a specific pencil shade as the standard fill, apply that consistently across all reviews.
4. **When in doubt, note it** — a well-documented uncertain decision is better than a confident wrong answer.
5. **Do not rush** — review accuracy directly affects student scores. Take the time you need.
6. **Report patterns** — if you notice systematic OCR errors (e.g., a particular question number is always low confidence), tell your supervisor. It may indicate a sheet printing defect.

# ExamShield — Sequence Diagrams

All diagrams use [Mermaid](https://mermaid.js.org/) syntax and render natively in GitHub.

---

## 1. Capture & Upload Flow

The end-to-end journey from photograph to immutably stored image.

```mermaid
sequenceDiagram
    actor Inv as Invigilator
    participant App as Flutter App
    participant Crypto as On-Device Crypto
    participant API as ExamShield API
    participant DB as PostgreSQL
    participant MQ as RabbitMQ
    participant S3 as MinIO / S3
    participant AL as Audit Log

    Inv->>App: Photograph answer sheet
    App->>App: Document detection + perspective correction
    App->>App: Image enhancement (contrast / sharpen / denoise)
    App->>Crypto: SHA-256(imageBytes) → hashHex
    App->>Crypto: ECDSA-P256.Sign(hashBytes, devicePrivKey) → signature
    App->>API: POST /capture {examId, studentId, deviceId, hashHex, signature, pageNumber}
    API->>API: Verify device is registered & trusted
    API->>API: Verify ECDSA signature against device public key
    API->>DB: INSERT Captures (status=Created, hashHex, signature)
    API->>AL: APPEND CaptureRegistered event
    API-->>App: 201 {captureId}

    App->>API: POST /upload {captureId, imageBytes}
    API->>API: Re-compute SHA-256(imageBytes)
    API->>API: Assert computed hash == registered hashHex
    alt Hash mismatch
        API->>AL: APPEND HashMismatch security event
        API-->>App: 422 Hash mismatch
    else Hash OK
        API->>API: Embed invisible watermark (LSB steganography)
        API->>API: AES-256-GCM encrypt with per-image DEK
        API->>API: Envelope-encrypt DEK with master key
        API->>S3: PUT ciphertext (Object Lock COMPLIANCE)
        API->>DB: UPDATE Captures SET status=Uploaded, encryptedDek=…
        API->>AL: APPEND ImageUploaded event
        API->>MQ: Publish OcrRequested message
        API-->>App: 200 {status: "Uploaded"}
    end
```

---

## 2. OCR Pipeline

Automated answer extraction with confidence-gated human fallback.

```mermaid
sequenceDiagram
    participant MQ as RabbitMQ
    participant OCR as OCR Service (FastAPI)
    participant S3 as MinIO / S3
    participant API as ExamShield API
    participant DB as PostgreSQL
    participant MR as Manual Review Queue
    participant SQ as Scoring Queue

    MQ->>OCR: OcrRequested {captureId}
    OCR->>API: GET /captures/{captureId}/image (OCREngine role)
    API->>S3: GET ciphertext
    API->>API: AES-256-GCM decrypt → plaintext bytes
    API-->>OCR: imageBytes

    OCR->>OCR: OpenCV bubble detection
    OCR->>OCR: Per-bubble confidence scoring

    alt All bubbles confidence ≥ 0.85
        OCR->>API: POST /ocr {captureId, answers[], overallConfidence}
        API->>DB: INSERT OcrResults (status=Completed)
        API->>MQ: Publish ScoringRequested
        MQ->>SQ: Enqueue for scoring
    else Any bubble confidence < 0.85
        OCR->>API: POST /ocr {captureId, answers[], overallConfidence, requiresReview=true}
        API->>DB: INSERT OcrResults (status=LowConfidence)
        API->>MQ: Publish ReviewRequired
        MQ->>MR: Enqueue for manual review
    end

    alt Dead-letter (OCR crash / timeout)
        MQ->>MQ: DLQ after 3 retries
        MQ->>API: POST /ocr {status=Failed}
        API->>DB: UPDATE OcrResults SET status=Failed
    end
```

---

## 3. Manual Review Flow

A human reviewer inspects the original image and records an interpretation. No pixel modification is possible.

```mermaid
sequenceDiagram
    actor Rev as Manual Reviewer
    actor Sup as Review Supervisor
    participant API as ExamShield API
    participant S3 as MinIO / S3
    participant DB as PostgreSQL
    participant AL as Audit Log

    Rev->>API: GET /reviews/pending
    API-->>Rev: [{reviewId, captureId, ocrPredictions[]}]

    Rev->>API: GET /captures/{id}/image (ManualReviewer role)
    API->>S3: GET ciphertext
    API->>API: Decrypt → plaintext bytes
    API-->>Rev: imageBytes (read-only; no write path exists)

    Rev->>API: POST /reviews/{id}/submit {interpretedAnswers[]}
    API->>DB: INSERT ReviewDecision (immutable)
    API->>AL: APPEND ReviewSubmitted event (signed)
    API-->>Rev: 200

    Sup->>API: POST /reviews/{id}/approve
    API->>DB: UPDATE ManualReviews SET status=Approved
    API->>AL: APPEND ReviewApproved event
    API->>API: Publish ScoringRequested
    API-->>Sup: 200

    alt Supervisor rejects
        Sup->>API: POST /reviews/{id}/reject {reason}
        API->>DB: UPDATE ManualReviews SET status=Rejected
        API->>AL: APPEND ReviewRejected event
        API-->>Sup: 200
    end

    alt Supervisor escalates
        Sup->>API: POST /reviews/{id}/escalate
        API->>DB: SET status=Escalated
        API->>AL: APPEND Escalated event
    end
```

---

## 4. Authentication & MFA Flow

JWT issuance with TOTP step-up for privileged roles.

```mermaid
sequenceDiagram
    actor User
    participant App as Client (Browser / Mobile)
    participant API as ExamShield API
    participant DB as PostgreSQL
    participant TOTP as TOTP Service

    User->>App: Enter email + password
    App->>API: POST /auth/login {email, password}
    API->>DB: Verify bcrypt hash
    alt Invalid credentials
        API-->>App: 401
    else Valid — MFA required for role
        API-->>App: 200 {requiresMfa: true, token: ""}
        App->>User: Prompt for TOTP code
        User->>App: Enter 6-digit TOTP
        App->>API: POST /auth/mfa/verify {email, code}
        API->>TOTP: ValidateTotp(secret, code)
        alt Invalid code
            API-->>App: 401
        else Valid
            API->>API: Generate JWT + amr:"mfa" claim
            API->>DB: INSERT SessionTokens (refreshToken, expiresAt)
            API-->>App: 200 {token, refreshToken, role}
        end
    else Valid — MFA not required
        API->>API: Generate JWT (no amr claim)
        API->>DB: INSERT SessionTokens
        API-->>App: 200 {token, refreshToken, role}
    end

    Note over App,API: Token refresh (silent renewal)
    App->>API: POST /auth/refresh {refreshToken}
    API->>DB: Validate refreshToken (not revoked, not expired)
    API->>API: Issue new JWT + new refreshToken
    API->>DB: Rotate refreshToken
    API-->>App: 200 {token, refreshToken}

    Note over App,API: Logout
    App->>API: POST /auth/logout {refreshToken}
    API->>DB: DELETE SessionTokens WHERE token=refreshToken
    API-->>App: 204
```

---

## 5. Device Registration Flow

New device onboarding with cryptographic key binding.

```mermaid
sequenceDiagram
    actor Inv as Invigilator
    actor DM as Device Manager
    participant App as Flutter App
    participant API as ExamShield API
    participant DB as PostgreSQL

    Inv->>App: Tap "Register this device"
    App->>App: Generate ECDSA P-256 key pair (on-device)
    App->>App: Store private key in Secure Enclave / Keystore
    App->>API: POST /devices {deviceName, publicKeyPem, os, appVersion}
    API->>DB: INSERT Devices (status=Pending, publicKey)
    API-->>App: 201 {deviceId, status: "Pending"}

    Note over DM,API: Offline approval by Device Manager
    DM->>API: PUT /devices/{id}/approve
    API->>DB: UPDATE Devices SET status=Approved
    API-->>DM: 200

    Note over App,API: Next capture — device is trusted
    App->>API: POST /capture {…, deviceId}
    API->>DB: SELECT publicKey FROM Devices WHERE id=deviceId AND status=Approved
    API->>API: ECDSA.Verify(signature, hash, publicKey)
    API-->>App: 201 {captureId}
```

---

## 6. Public Verification Flow

Anonymous third-party verification of a capture's authenticity.

```mermaid
sequenceDiagram
    actor Public as Verifier (Anonymous)
    participant UI as Public Verification Page
    participant API as ExamShield API
    participant DB as PostgreSQL
    participant S3 as MinIO / S3

    Public->>UI: Scan QR code or enter capture ID / SHA-256 hash
    UI->>API: GET /public/verify?captureId=…  (no auth required)
    API->>DB: SELECT hashHex, signature, deviceId, status, capturedAt FROM Captures
    alt Capture not found
        API-->>UI: 404
    else Found
        API->>DB: SELECT publicKey FROM Devices WHERE id=deviceId
        API->>API: ECDSA.Verify(signature, hashHex, publicKey)
        API->>S3: GET ciphertext (internal)
        API->>API: Decrypt → plaintext bytes
        API->>API: SHA-256(plaintext) → recomputedHash
        API->>API: Assert recomputedHash == storedHashHex
        API->>API: Extract watermark → verify captureId + nonce match
        API-->>UI: {
            captureId, examId, studentId,
            hashVerified: true,
            signatureVerified: true,
            watermarkIntact: true,
            capturedAt, uploadedAt,
            chainOfCustodyUrl
        }
        UI-->>Public: ✅ Authentic — Hash, Signature, Watermark all verified
    end
```

---

## 7. Scoring & Result Publication Flow

```mermaid
sequenceDiagram
    participant MQ as RabbitMQ
    participant SE as Scoring Engine (system role)
    participant API as ExamShield API
    participant DB as PostgreSQL
    actor RP as Result Publisher

    MQ->>SE: ScoringRequested {captureId}
    SE->>API: GET /exams/{examId}/answer-key (ScoringEngine role)
    API-->>SE: {answers: [{questionId, correctAnswer}]}
    SE->>API: GET /ocr/{captureId} (ScoringEngine role)
    API-->>SE: {answers: [{questionId, selected}]}

    SE->>SE: Compare student answers to answer key
    SE->>API: POST /score {captureId, correctAnswers, totalQuestions, scorePercent, breakdown[]}
    API->>DB: INSERT Scores
    API->>DB: UPDATE Captures SET status=Scored
    API-->>SE: 201

    RP->>API: POST /results/publish {examId, releaseAt}
    API->>DB: UPDATE Results SET published=true, releasedAt=releaseAt
    API-->>RP: 200

    Note over RP,API: Students can now view their results
```

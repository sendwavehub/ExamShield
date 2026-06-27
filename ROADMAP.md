# ExamShield Roadmap

This is the living roadmap for ExamShield Community Edition. Items may shift based on community feedback and contributor availability.

> Want to influence priorities? Open a [GitHub Discussion](https://github.com/your-org/examshield/discussions) or upvote existing issues.

---

## Legend

| Symbol | Meaning |
|---|---|
| ✅ | Done |
| 🚧 | In progress |
| 📋 | Planned |
| 💡 | Under consideration |
| ❌ | Descoped |

---

## v0.1 — Core Pipeline (Target: Q3 2026)

Goal: end-to-end capture → verify → audit with a single invigilator.

| Feature | Status |
|---|---|
| Flutter capture app (camera, edge detection, perspective correction) | 🚧 |
| On-device SHA-256 hash generation | ✅ |
| ECDSA P-256 digital signature (on-device) | ✅ |
| `POST /capture` endpoint (register hash + signature) | ✅ |
| `POST /upload` endpoint (upload image, verify hash) | ✅ |
| Server-side signature verification | ✅ |
| Immutable MinIO storage | ✅ |
| Append-only audit log | ✅ |
| `GET /audit` query endpoint | ✅ |
| `GET /verify/{id}` endpoint | ✅ |
| Docker Compose dev stack | ✅ |
| Unit tests — domain + application layer | ✅ |
| Integration tests — API + repository | ✅ |

---

## v0.2 — OCR + Manual Review (Target: Q3 2026)

Goal: automated answer extraction with a human fallback queue.

| Feature | Status |
|---|---|
| Tesseract + EasyOCR bubble detection pipeline | 🚧 |
| `POST /ocr` trigger endpoint | ✅ |
| Confidence threshold configuration | 📋 |
| Low-confidence routing to review queue | 📋 |
| Manual Review UI (two-panel: image + OCR predictions) | 📋 |
| Pixel Lock — prevent image modification in reviewer UI | 📋 |
| Every review decision creates immutable audit record | 📋 |
| Review Supervisor approve/reject workflow | 📋 |
| RabbitMQ message bus for OCR job dispatch | 🚧 |

---

## v0.3 — Scoring & Result Publication (Target: Q4 2026)

Goal: end-to-end pipeline from capture to published result.

| Feature | Status |
|---|---|
| Scoring engine (calculate score from recognized answers) | 📋 |
| `POST /score` endpoint | 📋 |
| `GET /results` endpoint | 📋 |
| Result Publisher role workflow | 📋 |
| Student result portal (view own result + certificate) | 📋 |
| Public QR / hash / signature verification page | 📋 |
| PDF certificate generation | 📋 |
| `GET /statistics` dashboard aggregates | 📋 |

---

## v0.4 — Full RBAC + MFA (Target: Q4 2026)

Goal: production-grade access control with Zero Trust enforcement.

| Feature | Status |
|---|---|
| All 15 roles fully implemented | 🚧 |
| JWT + Refresh Token auth | ✅ |
| MFA (TOTP) for privileged roles | 📋 |
| OAuth2 / OpenID Connect integration | 📋 |
| Device registration + certificate lifecycle | 📋 |
| Session management (list, revoke) | 📋 |
| Separation of Duties enforcement at auth layer | 📋 |
| Role management UI (tree view + permission matrix) | 📋 |
| Fine-grained resource authorization | 📋 |

---

## v0.5 — Alerts & Security Center (Target: Q1 2027)

Goal: real-time security monitoring for examination staff.

| Feature | Status |
|---|---|
| Alert triggers: hash mismatch, duplicate upload, invalid signature | 🚧 |
| Email alert channel (MailKit / SMTP) | 🚧 |
| LINE Notify alert channel | 🚧 |
| Slack alert channel | 📋 |
| Microsoft Teams alert channel | 📋 |
| Configurable webhook alert channel | 📋 |
| Security Center UI (SOC-style dashboard) | 📋 |
| Threat timeline visualization | 📋 |
| Audit Center UI (append-only timeline, PDF/Excel/CSV export) | 📋 |

---

## v1.0 — Production-Ready (Target: Q1 2027)

Goal: deployable to real national-scale examinations.

| Feature | Status |
|---|---|
| Kubernetes manifests (all services) | 🚧 |
| Horizontal pod autoscaling | 📋 |
| Multi-region deployment support | 📋 |
| AWS S3 Object Lock / Azure Immutable Blob | 📋 |
| Invisible watermark (multi-layer steganography) | 📋 |
| HSM integration for device key storage | 📋 |
| Full CI/CD pipeline (GitHub Actions) | 📋 |
| Load testing (100,000+ sheets) | 📋 |
| Penetration testing + security audit | 📋 |
| OpenAPI documentation complete | 📋 |
| Deployment guide | 📋 |
| User manual (invigilator + admin + reviewer) | 📋 |
| 99.9% uptime SLA architecture | 📋 |

---

## Post-v1.0 — Under Consideration

These are ideas not yet committed to any milestone. Open a Discussion to advocate for one.

| Idea | Notes |
|---|---|
| GraphQL API | Complementary to REST, not a replacement |
| Offline-first mobile with local DB sync | SQLite on device, sync on reconnect |
| ML-based handwriting recognition | Beyond bubble sheets — free-form answers |
| Blockchain-anchored audit log | Timestamping via Bitcoin or Ethereum OP_RETURN |
| QR-code anti-copy watermark | Visible QR on printed sheet, verified on upload |
| Multi-tenant SaaS mode | Multiple exam boards on a single cluster |
| Biometric invigilator verification | Face match at capture start |
| Federated learning OCR model | Train without exposing raw answer sheets |

---

## How to Contribute to the Roadmap

1. **Comment on a planned item** — find its GitHub issue and add your use case.
2. **Propose a new item** — open a [Discussion → Ideas](https://github.com/your-org/examshield/discussions/categories/ideas).
3. **Pick up a task** — see issues labeled [`good first issue`](https://github.com/your-org/examshield/labels/good%20first%20issue) or [`help wanted`](https://github.com/your-org/examshield/labels/help%20wanted).

This roadmap is updated after each milestone closes.

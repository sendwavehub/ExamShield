<div align="center">

<img src="cover.png" alt="ExamShield Banner" width="100%"/>

# ExamShield

**AI-powered secure exam scanning & anti-tampering system**

[![License](https://img.shields.io/badge/License-MIT%20%7C%20Apache%202.0-blue.svg)](LICENSE)
[![Build](https://img.shields.io/github/actions/workflow/status/sendwavehub/examshield/ci.yml?branch=main&label=CI)](https://github.com/sendwavehub/examshield/actions)
[![Tests](https://img.shields.io/badge/tests-520%20passing-brightgreen)](tests/)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple)](https://dotnet.microsoft.com)
[![Flutter](https://img.shields.io/badge/Flutter-3.x-blue)](https://flutter.dev)
[![React](https://img.shields.io/badge/React-18-61DAFB)](https://react.dev)
[![Docker](https://img.shields.io/badge/Docker-ready-2496ED)](infra/docker-compose.yml)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](CONTRIBUTING.md)
[![GitHub Stars](https://img.shields.io/github/stars/sendwavehub/examshield?style=social)](https://github.com/sendwavehub/examshield/stargazers)

[Why ExamShield?](#why-examshield) · [Features](#features) · [Architecture](#architecture) · [Quick Start](#quick-start) · [API](#api) · [Roadmap](#roadmap) · [Contributing](#contributing)

</div>

---

## Why ExamShield?

Paper-based exams are still the norm for millions of students — but the digitisation process is broken.

| The old way | ExamShield |
|---|---|
| Flatbed scanners take hours | Smartphone capture in seconds |
| Scanned files can be silently replaced | Every image is **SHA-256 hashed + ECDSA signed on-device** before upload |
| No way to prove an image hasn't been edited | **Invisible forensic watermark** — destruction signals tampering |
| Audit trails are spreadsheets | **Append-only cryptographic audit log** — every state transition signed |
| Admins can override anything | **Zero Trust RBAC** — even super-admins can't touch answer sheets |

> ExamShield makes silent tampering **technically impossible**, not just policy-prohibited.

---

## Perfect For

- **National examination bodies** running 100,000+ answer sheets per session
- **Universities & colleges** that want digital chain of custody for paper exams
- **Assessment companies** needing defensible audit trails for accreditation
- **EdTech developers** building secure exam platforms on top of a proven backbone

---

## Features

### Community Edition (this repo · MIT / Apache 2.0)

| Category | Feature | Status |
|---|---|---|
| **Capture** | Flutter mobile app — camera, edge detection, perspective correction | ✅ |
| **Integrity** | On-device SHA-256 hash before any network call | ✅ |
| **Integrity** | ECDSA P-256 / Ed25519 device signing | ✅ |
| **Integrity** | Server-side re-verification on upload | ✅ |
| **Integrity** | Invisible forensic watermark (exam ID · timestamp · nonce · hash · scanner ID) | ✅ |
| **Storage** | Immutable object storage (MinIO / AWS S3 Object Lock) | ✅ |
| **Storage** | AES-256-GCM per-image encryption at rest | ✅ |
| **OCR** | Tesseract + EasyOCR bubble detection pipeline | ✅ |
| **Review** | Human review queue for low-confidence OCR results | ✅ |
| **Review** | Read-only image viewer (zoom · rotate · brightness · contrast) | ✅ |
| **Scoring** | Scoring engine + ranking + statistics | ✅ |
| **Audit** | Append-only audit log — full chain of custody | ✅ |
| **RBAC** | 15-role Zero Trust hierarchy, Separation-of-Duties enforced | ✅ |
| **Auth** | JWT + refresh tokens + MFA (TOTP) + device certificates | ✅ |
| **Dashboard** | React + TypeScript admin UI (dark/light) | ✅ |
| **API** | 101-endpoint REST API, OpenAPI / Swagger | ✅ |
| **DevOps** | Docker Compose dev stack | ✅ |
| **Tests** | 520 unit + integration tests | ✅ |

### Enterprise Edition *(coming soon — [sendwavehub.com](https://github.com/sendwavehub))*

| Feature | Description |
|---|---|
| Multi-tenant SaaS | Isolated tenants, org-level admin, custom domains |
| Enterprise SSO | SAML 2.0, Azure AD, Google Workspace |
| Hardware KMS | HashiCorp Vault Transit, AWS KMS, Azure Key Vault |
| Advanced Analytics | Cross-session trends, fraud heatmaps, confidence benchmarks |
| SLA Support | Dedicated response SLA + onboarding |
| Air-gapped Deployment | Self-hosted with no internet dependency |
| Compliance Reports | SOC 2 evidence packs, PDPA / GDPR data maps |

> Interested in the Enterprise edition? [Open a discussion →](https://github.com/sendwavehub/examshield/discussions)

---

## Demo

| Capture Flow | Security Dashboard | Audit Timeline |
|---|---|---|
| *(screenshot coming soon)* | *(screenshot coming soon)* | *(screenshot coming soon)* |

Screenshots and walkthroughs live in [docs/screenshots/](docs/screenshots/).

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                          ExamShield System                          │
│                                                                     │
│  ┌─────────────┐    ┌───────────────────────────────────────────┐   │
│  │ Flutter App │    │            ASP.NET Core 9 API             │   │
│  │  (Mobile)   │───▶│  ┌──────────┐  ┌──────────┐              │   │
│  │             │    │  │ Commands │  │ Queries  │   (CQRS)     │   │
│  │ • Capture   │    │  │(MediatR) │  │(MediatR) │              │   │
│  │ • Hash      │    │  └────┬─────┘  └─────┬────┘              │   │
│  │ • Sign      │    │       │               │                   │   │
│  │ • Upload    │    │  ┌────▼───────────────▼────┐              │   │
│  └─────────────┘    │  │       Domain Layer      │              │   │
│                     │  │  (Entities, Value Objs, │              │   │
│  ┌─────────────┐    │  │    Domain Events)       │              │   │
│  │  React      │    │  └────────────┬────────────┘              │   │
│  │  Dashboard  │───▶│               │                           │   │
│  │             │    │  ┌────────────▼────────────┐              │   │
│  │ • Monitor   │    │  │   Infrastructure Layer  │              │   │
│  │ • Review    │    │  │ EF Core · MinIO · Redis  │              │   │
│  │ • Audit     │    │  │ RabbitMQ · Tesseract     │              │   │
│  └─────────────┘    │  └─────────────────────────┘              │   │
│                     └───────────────────────────────────────────┘   │
│                                                                     │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌───────────────────┐   │
│  │PostgreSQL│  │  Redis   │  │ RabbitMQ │  │ MinIO / S3 Lock   │   │
│  └──────────┘  └──────────┘  └──────────┘  └───────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

### Capture Pipeline

```
Invigilator App          API Server              Storage
      │                      │                      │
      │─── POST /capture ───▶│                      │
      │    {hash, signature} │── verify signature   │
      │◀── 201 captureId ────│                      │
      │                      │                      │
      │─── POST /upload ────▶│                      │
      │    {image bytes}     │── re-hash bytes       │
      │                      │── compare hash        │
      │                      │── embed watermark     │
      │                      │── AES-256-GCM encrypt │
      │                      │── write immutable ──▶ │
      │                      │── append audit log    │
      │◀── 200 verified ─────│                      │
```

### Clean Architecture Layers

```
ExamShield/
├── src/
│   ├── ExamShield.Domain/          # Entities, value objects, domain events
│   ├── ExamShield.Application/     # CQRS commands/queries (MediatR)
│   ├── ExamShield.Infrastructure/  # EF Core, MinIO, OCR, crypto, messaging
│   ├── ExamShield.Api/             # ASP.NET Core 9 endpoints, OpenAPI
│   ├── ExamShield.Mobile/          # Flutter (Android + iOS)
│   └── ExamShield.Dashboard/       # React + TypeScript + Tailwind + shadcn/ui
├── tests/
│   ├── ExamShield.UnitTests/
│   └── ExamShield.IntegrationTests/
└── infra/
    ├── docker-compose.yml
    └── k8s/                        # Kubernetes manifests
```

---

## Quick Start

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) ≥ 24
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Flutter 3.x](https://docs.flutter.dev/get-started/install) *(for mobile)*
- [Node.js 20+](https://nodejs.org/) *(for dashboard)*

### 1 — Spin up infrastructure

```bash
git clone https://github.com/sendwavehub/examshield.git
cd examshield
docker compose -f infra/docker-compose.yml up -d
```

Starts **PostgreSQL**, **Redis**, **RabbitMQ**, and **MinIO**.

### 2 — Backend API

```bash
dotnet restore
dotnet build
dotnet test                              # 520 tests, all green
dotnet run --project src/ExamShield.Api
```

API → `https://localhost:5001`  ·  Swagger UI → `https://localhost:5001/swagger`

### 3 — Admin Dashboard

```bash
cd src/ExamShield.Dashboard
npm install
npm run dev
```

Dashboard → `http://localhost:5173`

### 4 — Mobile App (Flutter)

```bash
cd src/ExamShield.Mobile
flutter pub get
flutter run           # connects to localhost API by default
```

### Environment Variables

```bash
cp infra/.env.example infra/.env
```

| Variable | Description |
|---|---|
| `POSTGRES_CONNECTION` | PostgreSQL connection string |
| `REDIS_CONNECTION` | Redis connection string |
| `MINIO_ENDPOINT` | MinIO / S3 endpoint |
| `JWT_SIGNING_KEY` | ECDSA P-256 private key (PEM) |
| `ENCRYPTION__MASTERKEYBBASE64` | 32-byte AES master key (base64) |
| `ALERT_EMAIL_SMTP` | SMTP host for email alerts |

---

## API

Full OpenAPI spec at `/swagger` when running locally. 101 endpoints across 12 resource groups.

### Key Endpoints

```http
# Capture pipeline
POST   /capture              Register a new capture (hash + signature)
POST   /upload               Upload image bytes — re-verified server-side
GET    /verify/{id}          Re-verify stored image integrity
GET    /captures/{id}/chain-of-custody   Full signed custody chain

# Auth
POST   /auth/login           JWT login
POST   /auth/mfa/verify      MFA code verification (TOTP)
POST   /auth/refresh         Refresh token

# OCR & Review
POST   /ocr                  Trigger OCR pipeline
POST   /ocr/batch            Batch OCR for an entire exam
GET    /review/queue         Manual review queue

# Audit
GET    /audit                Append-only audit log (filter by entity, actor, date)

# Public (anonymous)
GET    /public/verify?hash=  Verify SHA-256 hash without login
```

---

## Security

Security is a first-class concern — not an afterthought.

| Control | Detail |
|---|---|
| Hash-then-sign | SHA-256 on-device → server re-verifies; mismatches rejected + alerted |
| Digital signatures | ECDSA P-256 per-device key; public key registered server-side |
| Encryption at rest | AES-256-GCM per-image DEK, envelope-encrypted with master key |
| Invisible watermark | Exam ID · timestamp · nonce · hash · scanner ID embedded in every image |
| Append-only audit | No UPDATE or DELETE path exists in schema or domain layer |
| Zero Trust RBAC | 15 roles, hard Separation-of-Duties — no single actor can commit undetected fraud |
| MFA step-up | Investigation Officers must present `amr: mfa` claim to access image bytes |
| Alert triggers | Hash mismatch · duplicate upload · invalid signature · suspicious login |

To report a vulnerability, see [SECURITY.md](SECURITY.md).

---

## Roadmap

See [ROADMAP.md](ROADMAP.md) for the full versioned plan.

| Version | Theme | Target |
|---|---|---|
| **v0.1** | Core pipeline — capture → verify → audit | Q3 2026 |
| **v0.2** | OCR + manual review queue | Q3 2026 |
| **v0.3** | Scoring engine + result publication | Q4 2026 |
| **v0.4** | Full RBAC + MFA + device certificates | Q4 2026 |
| **v1.0** | Production-ready + Kubernetes | Q1 2027 |

---

## Contributing

We welcome contributions of all sizes — bug fixes, new features, docs, translations.

1. Read [CONTRIBUTING.md](CONTRIBUTING.md)
2. Fork → branch (`feat/your-feature` or `fix/your-bug`)
3. Write a failing test first (TDD)
4. Implement the change
5. `dotnet test` — all green
6. Open a PR against `main`

Good first issues are labelled [`good first issue`](https://github.com/sendwavehub/examshield/issues?q=is%3Aopen+label%3A%22good+first+issue%22).

---

## Community

- **[GitHub Discussions](https://github.com/sendwavehub/examshield/discussions)** — questions, ideas, show-and-tell
- **[GitHub Issues](https://github.com/sendwavehub/examshield/issues)** — bug reports and feature requests
- **[Security disclosures](SECURITY.md)** — responsible disclosure process

---

## Contributors

Thanks to everyone who has contributed to ExamShield!

<!-- contrib.rocks — add your GitHub org/repo once public -->
<a href="https://github.com/sendwavehub/examshield/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=sendwavehub/examshield" alt="Contributors"/>
</a>

---

## License

ExamShield Community Edition is dual-licensed under **[MIT](LICENSE)** and **[Apache 2.0](LICENSE)**. You may choose either license.

---

<div align="center">

**If ExamShield saves you time or inspires your work, please [⭐ star the repo](https://github.com/sendwavehub/examshield) — it helps others find the project.**

Built with care for integrity in education · [sendwavehub](https://github.com/sendwavehub)

</div>

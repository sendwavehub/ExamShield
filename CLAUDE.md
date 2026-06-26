# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Skills

The `solid` skill is installed at `.claude/solid-skills-main/skills/solid/SKILL.md`. Invoke `/solid` for any coding, refactoring, architecture design, or code review task. It enforces:

- **TDD** — Red-Green-Refactor; write a failing test before any production code
- **SOLID** — applied to every class and function (see `references/solid-principles.md`)
- **Value objects** — mandatory for all domain primitives: `ExamId`, `StudentId`, `DeviceId`, `Hash`, `Signature`, `Email`, etc.
- **Clean code** — methods < 10 lines, classes < 50 lines, no `else` when early return works, Law of Demeter
- **Code smell detection** — auto-detect and refactor (see `references/code-smells.md`)

The skill's examples use TypeScript/NestJS but its principles apply equally to this project's C# backend, Flutter mobile app, and React dashboard.

---

## Project Status

This is a **greenfield project**. The only file currently present is `prompt.md`, which contains the full product specification. All implementation work begins from that spec.

## What This Is

ExamShield is an enterprise-grade, AI-powered secure exam scanning and anti-tampering system. Invigilators photograph paper answer sheets using a Flutter mobile app; the image is immediately hashed, digitally signed, and stored immutably. An OCR pipeline then reads the answers, with low-confidence results routed to a human review queue. Every action is recorded in an append-only audit log.

## Architecture

The system follows **Clean Architecture** with **CQRS** and **DDD** principles:

```
ExamShield/
├── src/
│   ├── ExamShield.Domain/          # Entities, value objects, domain events, interfaces
│   ├── ExamShield.Application/     # Use cases, commands/queries (MediatR), DTOs
│   ├── ExamShield.Infrastructure/  # EF Core, storage, OCR, crypto, messaging
│   ├── ExamShield.Api/             # ASP.NET Core 9 Web API, OpenAPI
│   ├── ExamShield.Mobile/          # Flutter app (Android + iOS)
│   └── ExamShield.Dashboard/       # React + TypeScript admin UI
├── tests/
│   ├── ExamShield.UnitTests/
│   └── ExamShield.IntegrationTests/
├── infra/
│   ├── docker-compose.yml
│   └── k8s/
└── docs/
```

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core 9, C#, EF Core |
| Mobile | Flutter (Android + iOS) |
| Admin UI | React, TypeScript |
| Database | PostgreSQL |
| Cache | Redis |
| Messaging | RabbitMQ |
| Object Storage | MinIO (dev) / AWS S3 Object Lock / Azure Immutable Blob (prod) |
| Image Processing | OpenCV, ML Kit, Tesseract, EasyOCR |
| Auth | JWT (ECDSA P-256 or Ed25519 device keys, RBAC) |
| Container | Docker Compose (dev), Kubernetes (prod) |

## Core Security Invariants

These must never be compromised in any implementation:

1. **Immutability** — stored images and audit logs are append-only; no update or delete path should exist in the domain or infrastructure layer.
2. **Hash-then-sign** — SHA-256 of the raw image bytes is computed on-device before any network call; the server re-verifies on receipt and rejects mismatches.
3. **Digital signatures** — each capture is signed with the device's private key (ECDSA P-256 or Ed25519); the server verifies against the registered public key.
4. **Invisible watermark** — embedded in every stored image (exam ID, timestamp, nonce, hash, scanner ID); watermark destruction signals tampering.
5. **Read-only review** — manual reviewers can only record their interpreted answer; they cannot modify or replace the original image.
6. **Chain of custody** — every state transition (Student → Invigilator → Device → Server → OCR → Reviewer → Score) must be recorded and digitally signed.

## Domain Model (planned tables)

`Exam`, `Student`, `AnswerSheet`, `Capture`, `OCRResult`, `Question`, `Answer`, `Score`, `AuditLog`, `Device`, `Signature`, `Verification`, `ManualReview`, `User`, `Role`, `Permission`

## API Shape

```
POST /capture       # Register a new capture with hash + signature
POST /upload        # Upload image bytes (must match registered capture)
GET  /verify/{id}   # Re-verify hash and signature
GET  /audit         # Query audit log (append-only)
POST /ocr           # Trigger OCR pipeline
POST /score         # Finalize scoring
GET  /results       # Published results
GET  /statistics    # Dashboard stats
```

## Dashboard Roles

`Operator` < `Supervisor` < `Auditor` < `Administrator` < `Security Officer`

## Alert Triggers

Hash mismatch, invalid signature, duplicate upload, unexpected image replacement, suspicious login, excessive failed uploads → notify via Email, LINE Notify, Slack, Microsoft Teams, or configurable webhook.

## Development Setup (to be created)

Once the backend is scaffolded, the expected local stack will be:

```bash
docker compose up -d          # PostgreSQL, Redis, RabbitMQ, MinIO
dotnet build                  # Backend
dotnet test                   # All tests
dotnet run --project src/ExamShield.Api
```

For Flutter:
```bash
flutter pub get
flutter run                   # Mobile app
```

For the React dashboard:
```bash
npm install
npm run dev
```

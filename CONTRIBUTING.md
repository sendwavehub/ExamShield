# Contributing to ExamShield

Thank you for considering a contribution. ExamShield is security-critical software — we hold contributions to a high standard, but we are welcoming and will help you get there.

---

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [How to Contribute](#how-to-contribute)
- [Development Setup](#development-setup)
- [Engineering Standards](#engineering-standards)
- [Branch & PR Conventions](#branch--pr-conventions)
- [Commit Style](#commit-style)
- [Testing Requirements](#testing-requirements)
- [Security Contributions](#security-contributions)

---

## Code of Conduct

All contributors are expected to follow our [Code of Conduct](CODE_OF_CONDUCT.md). Be kind, constructive, and respectful.

---

## How to Contribute

### Found a bug?

1. Search [existing issues](https://github.com/your-org/examshield/issues) first.
2. If none exists, open a new issue using the **Bug Report** template.
3. Include: steps to reproduce, expected vs actual behavior, environment details.

### Have a feature idea?

1. Open a **GitHub Discussion** in the [Ideas](https://github.com/your-org/examshield/discussions/categories/ideas) category first. This avoids wasted effort on a PR that doesn't align with the roadmap.
2. Once a maintainer gives a thumbs-up, create an issue and reference the discussion.

### Want to fix something yourself?

1. Comment on the issue: "I'd like to work on this."
2. A maintainer will assign it and confirm scope.
3. Fork, branch, implement, test, PR — see below.

---

## Development Setup

```bash
# 1. Fork and clone
git clone https://github.com/<your-handle>/examshield.git
cd examshield

# 2. Infrastructure (PostgreSQL, Redis, RabbitMQ, MinIO)
docker compose -f infra/docker-compose.yml up -d

# 3. Backend
dotnet restore
dotnet build
dotnet test

# 4. Dashboard
cd src/ExamShield.Dashboard && npm install && npm run dev

# 5. Mobile (optional)
cd src/ExamShield.Mobile && flutter pub get && flutter run
```

See [README.md](README.md) for full environment variable setup.

---

## Engineering Standards

ExamShield enforces **SOLID + TDD + Clean Architecture**. Every PR is reviewed against these.

### Test-Driven Development (mandatory)

Write a failing test **before** production code. The Red-Green-Refactor cycle is not optional.

```
1. Write a failing test that describes the desired behavior
2. Write the minimum code to make it pass (Green)
3. Refactor without breaking tests
```

### Clean Architecture boundaries

- `Domain` — no dependencies on any other layer.
- `Application` — depends only on `Domain`.
- `Infrastructure` — depends on `Application` and `Domain`.
- `Api` — orchestrates everything, no business logic.

Never put business logic in controllers or repositories.

### SOLID

| Principle | What we check |
|---|---|
| **S** — Single Responsibility | One reason to change per class |
| **O** — Open/Closed | Extend via interfaces, not by modifying closed types |
| **L** — Liskov Substitution | Subtypes must be substitutable for base types |
| **I** — Interface Segregation | Small, focused interfaces — no fat interfaces |
| **D** — Dependency Inversion | Depend on abstractions, not concretions |

### Code style

- Methods < 10 lines
- Classes < 50 lines (excluding generated/EF migrations)
- No `else` when an early return works
- No magic strings — use value objects (`ExamId`, `StudentId`, `Hash`, etc.)
- No comments explaining *what* the code does — name things so they're self-documenting
- Add a comment only when the *why* is non-obvious (hidden constraint, subtle invariant)

### Security invariants — never break these

- No UPDATE or DELETE path for `AuditLog`, `Capture`, or `AnswerSheet`
- Hash must be computed on-device before any network call
- Signature must be verified server-side before storage
- No reviewer may modify original image pixels
- All privilege escalation requires a second approval

---

## Branch & PR Conventions

| Branch prefix | Use for |
|---|---|
| `feat/` | New feature |
| `fix/` | Bug fix |
| `refactor/` | Refactoring (no behavior change) |
| `test/` | Adding or fixing tests only |
| `docs/` | Documentation only |
| `chore/` | Tooling, deps, CI |
| `security/` | Security-related changes |

Branch name format: `feat/ocr-confidence-threshold`

PR title format (Conventional Commits):
```
feat(ocr): add configurable confidence threshold
fix(capture): reject upload if hash is missing
```

### PR checklist (auto-applied from template)

- [ ] Tests written first (failing → passing)
- [ ] `dotnet test` passes locally
- [ ] No new compiler warnings
- [ ] No new security invariant violations
- [ ] CHANGELOG entry added (if user-visible change)
- [ ] Docs updated (if API or config changed)

---

## Commit Style

We use [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <short description>

[optional body — explain WHY, not what]

[optional footer — breaking changes, issue refs]
```

Types: `feat` · `fix` · `refactor` · `test` · `docs` · `chore` · `security` · `perf`

---

## Testing Requirements

| Layer | Minimum coverage |
|---|---|
| Domain value objects | 100% |
| Application command/query handlers | 90%+ |
| Infrastructure repositories | Integration tests against real DB |
| API endpoints | Integration tests via `WebApplicationFactory` |
| Security invariants | At least one negative-path test per invariant |

Run all tests:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## Security Contributions

If your contribution touches crypto, storage, RBAC, or the audit log — please flag it explicitly in the PR description. A security maintainer will be added as a required reviewer.

To report a vulnerability privately, see [SECURITY.md](SECURITY.md).

---

Thank you for helping make exam integrity trustworthy.

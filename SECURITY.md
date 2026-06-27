# Security Policy

ExamShield is security-critical infrastructure. We take all vulnerability reports seriously and respond as quickly as possible.

---

## Supported Versions

| Version | Supported |
|---|---|
| `main` (unreleased) | ✅ Active development |
| v1.x (once released) | ✅ Security patches |
| < v1.0 | ❌ No support |

---

## Reporting a Vulnerability

**Please do NOT open a public GitHub issue for security vulnerabilities.**

Disclosing a security bug publicly before a patch is available could put users at risk.

### How to report

1. **Email:** security@examshield.dev *(PGP key below)*
2. **GitHub Private Advisory:** [Report a vulnerability](https://github.com/your-org/examshield/security/advisories/new) via GitHub's private disclosure tool.

Include as much detail as possible:

- Affected component (API / Mobile / Dashboard / Infrastructure)
- Description of the vulnerability
- Steps to reproduce
- Potential impact
- Your suggested fix (optional but appreciated)

### What happens next

| Timeline | Action |
|---|---|
| **24 hours** | Acknowledgment of your report |
| **72 hours** | Initial severity assessment |
| **7 days** | Patch development starts (critical) |
| **30 days** | Target resolution for high/critical |
| **90 days** | Coordinated public disclosure |

We follow [Coordinated Vulnerability Disclosure (CVD)](https://vuls.cert.org/confluence/display/CVD). You will be credited (unless you prefer anonymity).

---

## Security Architecture

ExamShield is built around these non-negotiable invariants. A valid security finding is anything that violates one of them:

### 1 — Immutability

All captured images and audit log entries are **append-only**. There is no UPDATE or DELETE path in the domain or infrastructure layer. Any code path that permits modification is a critical vulnerability.

### 2 — Hash-then-Sign

The SHA-256 hash of the raw image bytes is computed **on-device** before any network call. The server re-verifies on receipt and rejects mismatches with an alert. A bypass of hash verification is a critical vulnerability.

### 3 — Digital Signatures

Each capture is signed with the device's private key (ECDSA P-256 or Ed25519). The server verifies against the registered public key. Signature forgery or bypass is a critical vulnerability.

### 4 — Invisible Watermark

Every stored image carries an invisible forensic watermark (exam ID, timestamp, nonce, SHA-256, scanner ID). Watermark destruction signals tampering. A watermark bypass is a high-severity vulnerability.

### 5 — Read-Only Review

Manual reviewers can only record their interpreted answer. They cannot modify or replace the original image. Any code path that allows pixel modification is a critical vulnerability.

### 6 — Chain of Custody

Every state transition must be recorded in the append-only audit log. A gap in the audit log is a high-severity vulnerability.

### 7 — RBAC / Separation of Duties

No single role may both capture and score, or both review and publish. Privilege escalation without approval is a critical vulnerability.

---

## Threat Model

| Threat | Mitigation |
|---|---|
| Invigilator replaces an image | Hash + signature verification; duplicate upload detection |
| Network MITM | TLS enforced end-to-end; certificate pinning on mobile |
| Compromised device key | Device registration + revocation; HSM support planned |
| SQL injection | EF Core parameterized queries; no raw SQL |
| Broken access control | RBAC enforced at API layer; policy-based authorization |
| Log tampering | Append-only DB schema; separate write and read replicas |
| Insider threat | Separation of Duties; every action in audit log |
| Stolen JWT | Short-lived access tokens (15 min); refresh token rotation |
| Brute-force login | Rate limiting; MFA required for privileged roles |
| Watermark removal | Invisible multi-layer steganography; hash re-verification |

---

## Known Limitations (Community Edition)

- HSM integration is not yet implemented (planned for v1.0)
- Watermarking strength depends on image quality — very low-resolution captures may reduce robustness
- MFA is not enforced on all roles yet (in progress)

---

## PGP Public Key

```
-----BEGIN PGP PUBLIC KEY BLOCK-----
[Replace with actual PGP key when available]
-----END PGP PUBLIC KEY BLOCK-----
```

---

## Acknowledgments

We thank all security researchers who responsibly disclose vulnerabilities. Contributors who report valid findings will be listed here (with permission).

| Researcher | Finding | Date |
|---|---|---|
| *(none yet)* | | |

---

## Bug Bounty

There is no formal bug bounty program at this time. We offer public acknowledgment and our sincere gratitude.

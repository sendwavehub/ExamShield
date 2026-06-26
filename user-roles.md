# Project Prompt: Design Enterprise Role-Based Access Control (RBAC) for ExamShield

## Objective

Design a complete enterprise-grade Role-Based Access Control (RBAC) system for **ExamShield**, an AI-Powered Secure Exam Scanning & Anti-Tampering Platform.

The platform uses smartphone cameras instead of scanners to capture answer sheets, digitally signs every uploaded image, stores files in immutable storage, performs AI OCR, verifies integrity using cryptographic hashes, and provides a complete chain of custody.

The RBAC system must follow enterprise security principles including:

* Zero Trust Architecture
* Principle of Least Privilege
* Separation of Duties (SoD)
* Defense in Depth
* Immutable Audit Logging

No single user should be capable of compromising examination integrity.

---

# Security Principles

The generated design must satisfy:

* Every action requires authorization.
* Every action is logged.
* Every role has only the minimum required permissions.
* Original answer sheet images can NEVER be modified.
* Every privilege escalation requires approval.
* Sensitive operations require dual authorization where appropriate.
* All security events are auditable.

---

# User Roles

Generate the following roles.

## 1. Super Administrator

Responsibilities

* Platform configuration
* Tenant management
* License management
* Global settings
* Disaster recovery
* Backup management
* Encryption policy

Can

* Manage all users
* Configure system
* View all reports

Cannot

* Modify answer sheets
* Change OCR results
* Modify exam scores

---

## 2. Security Administrator

Responsibilities

* Cryptographic security
* Digital certificates
* Public/private keys
* Signature verification
* Device trust
* MFA policies

Can

* Verify signatures
* Rotate certificates
* Configure HSM
* Investigate security alerts

Cannot

* Edit exams
* Publish results
* Score exams

---

## 3. System Administrator

Responsibilities

* Infrastructure
* Servers
* Database
* Storage
* Monitoring
* Queue services
* Docker/Kubernetes

Can

* Restart services
* Configure storage
* Backup database

Cannot

* View student answers
* Change exam data

---

## 4. Exam Manager

Responsibilities

Manage

* Exams
* Subjects
* Sessions
* Candidate lists
* Schedules
* Exam centers

Cannot

* Modify scanned images
* Change scores

---

## 5. Device Manager

Responsibilities

Manage trusted capture devices.

Can

* Register smartphones
* Approve devices
* Disable lost devices
* Monitor device health
* Manage QR pairing

Cannot

* Upload answer sheets
* Review exams

---

## 6. Invigilator (Mobile User)

Uses the mobile application.

Can

* Login
* Capture answer sheets
* Upload images
* Retry failed uploads
* View upload history

Cannot

* Delete images
* Replace images
* Edit images
* View scores

---

## 7. OCR Engine (System Account)

Automated service.

Can

* Read images
* Detect bubbles
* Extract answers
* Calculate confidence

Cannot

* Modify original images
* Publish results

---

## 8. Manual Reviewer

Purpose

Review only low-confidence OCR results.

Can

* View original image
* Review OCR prediction
* Select correct answer interpretation

Cannot

* Paint
* Erase
* Modify image pixels
* Delete answer sheets

Every review must generate an immutable audit record.

---

## 9. Review Supervisor

Can

* Approve manual review decisions
* Reject corrections
* Escalate disputes

Cannot

* Edit images

---

## 10. Scoring Engine

Automated service.

Can

* Calculate scores
* Generate rankings
* Produce statistics

Cannot

* Modify answers
* Replace answer sheets

---

## 11. Result Publisher

Can

* Publish results
* Schedule result release
* Generate official reports

Cannot

* Change scores
* Edit answer sheets

---

## 12. Auditor

Read-only role.

Can inspect

* Audit logs
* Security logs
* Chain of custody
* Signature verification
* Hash verification
* Login history
* Device history

Cannot modify any data.

---

## 13. Investigation Officer

Used during fraud investigations.

Can

* Compare original images
* Compare hashes
* Compare signatures
* Download forensic reports
* Review timelines

Cannot

* Modify evidence

---

## 14. Student

Can

* Login
* View personal results
* Download certificates
* Verify answer sheet authenticity
* Submit review requests

Cannot

* View other students

---

## 15. Public Verification User

Anonymous access.

Can

Verify

* QR Code
* SHA-256 Hash
* Digital Signature
* Timestamp
* Chain of Custody Status

Cannot

Access personal student information.

---

# Permission Matrix

Generate a complete permission matrix including:

Create

Read

Update

Delete

Approve

Verify

Review

Publish

Export

Audit

Investigate

Configure

Manage Devices

Manage Users

Manage Roles

Manage Exams

View Reports

Generate Reports

View Security Logs

Verify Signatures

Hash Verification

Manual Review

Result Publishing

Student Portal

Public Verification

---

# Separation of Duties

Design the system so that:

* Invigilators capture but never review.
* Reviewers review but never score.
* Scoring services score but never publish.
* Publishers publish but never modify results.
* Security administrators verify integrity but never influence exam outcomes.
* Auditors can inspect everything but cannot modify anything.

No individual role should be capable of committing undetected examination fraud.

---

# Authentication

Support

* JWT Authentication
* Refresh Tokens
* Multi-Factor Authentication (MFA)
* OAuth2 / OpenID Connect
* Device Registration
* Device Certificates
* Trusted Devices
* Session Expiration
* Password Policies

---

# Authorization

Implement

* Role-Based Access Control (RBAC)
* Permission-Based Authorization
* Policy-Based Authorization
* Claims-Based Authorization
* Tenant Isolation
* Fine-Grained Resource Authorization

---

# Audit Logging

Every action must generate an immutable audit event containing:

* User ID
* Role
* Action
* Resource
* IP Address
* Device ID
* Timestamp
* Before Value (if applicable)
* After Value (if applicable)
* Digital Signature
* Correlation ID

Audit logs must be append-only.

---

# Database Design

Generate tables for:

* Users
* Roles
* Permissions
* RolePermissions
* UserRoles
* UserClaims
* Devices
* DeviceCertificates
* AuditLogs
* SecurityEvents
* LoginHistory
* SessionTokens

Include primary keys, foreign keys, indexes, and relationships.

---

# API Design

Generate REST APIs for:

Authentication

Authorization

User Management

Role Management

Permission Management

Device Registration

Security Events

Audit Logs

Public Verification

Include request/response models and HTTP status codes.

---

# Architecture

Follow Clean Architecture.

Generate:

* Domain Layer
* Application Layer
* Infrastructure Layer
* API Layer

Include CQRS where appropriate.

---

# Deliverables

Generate:

* Complete RBAC Architecture
* Permission Matrix
* User Role Diagram
* Entity Relationship Diagram (ERD)
* Database Schema
* ASP.NET Core Authorization Policies
* JWT Authentication
* Permission Middleware
* Role Management APIs
* Audit Logging Framework
* Security Event Framework
* UML Class Diagrams
* Sequence Diagrams
* OpenAPI Documentation
* Unit Tests
* Integration Tests
* Security Checklist

The final solution should be enterprise-ready, scalable, secure, maintainable, and suitable for deployment in government examination systems, universities, certification authorities, and large educational institutions.

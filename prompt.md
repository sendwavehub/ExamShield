# Project Prompt: AI-Powered Secure Exam Scanning & Anti-Tampering System

## Objective

Build a complete enterprise-grade web application that prevents exam answer sheet tampering after scanning.

Instead of using a traditional scanner, the system uses the smartphone camera as the scanning device.

The system must ensure that once an answer sheet has been photographed, no one—including administrators or operators—can modify the image without detection.

The system should provide cryptographic proof that every uploaded image is authentic.

---

# System Overview

Workflow:

Student
↓

Paper Answer Sheet

↓

Invigilator opens Mobile App

↓

Smartphone Camera Capture

↓

Automatic Edge Detection

↓

Perspective Correction

↓

Image Enhancement

↓

Generate SHA-256 Hash

↓

Timestamp

↓

Digital Signature

↓

Upload to Server

↓

Immutable Storage

↓

OCR

↓

Answer Recognition

↓

Scoring

↓

Audit Log

↓

Administrator Dashboard

---

# Primary Goals

The system must:

* Prevent answer sheet modification
* Detect any image tampering
* Preserve original evidence
* Record every operation
* Support thousands of answer sheets
* Produce legally verifiable audit trails

---

# Mobile Application

Platforms:

* Android
* iOS

Framework:

Flutter

Features:

### Camera

Use smartphone camera instead of scanner.

Requirements:

* Auto focus
* Auto exposure
* Flash support
* High resolution capture
* Burst mode
* Offline mode

---

### AI Document Detection

Automatically detect:

* paper edges
* document boundary
* orientation

Then perform

* crop
* perspective correction
* deskew
* rotation

Similar quality to Microsoft Lens or Adobe Scan.

---

### Image Enhancement

Automatically:

* remove shadows
* improve contrast
* sharpen
* grayscale conversion
* adaptive threshold
* remove background noise

---

### Quality Check

Reject image when:

* blurry
* dark
* overexposed
* partially captured
* missing corners
* camera moved

Display:

"Please retake the photo."

---

### Metadata

Store:

Exam ID

Student ID

Page Number

Invigilator ID

Device ID

GPS (optional)

Capture Time

Camera Model

Application Version

Random Nonce

---

# Security

Immediately after capture:

Generate

SHA-256(Image)

Store:

Image

Hash

Timestamp

Metadata

---

Generate Digital Signature

Use:

ECDSA P-256

or

Ed25519

Private key stored securely.

Save:

Signature

Public Key ID

---

Server verifies:

Hash

Signature

Timestamp

Reject if invalid.

---

# Immutable Storage

Original image can NEVER be modified.

Store:

Original Image

Hash

Signature

Metadata

Audit History

Version 1

No overwrite.

No delete.

Only append.

Support:

AWS S3 Object Lock

Azure Immutable Blob

MinIO Object Lock

---

# OCR Engine

Automatically detect:

Student ID

Exam Number

Question Number

Filled Bubble

Checkbox

Multiple Choice

Support:

OpenCV

Google ML Kit

Tesseract

EasyOCR

---

# Bubble Detection

Detect:

filled bubble

empty bubble

partial fill

multiple selections

confidence score

Each answer should include confidence.

---

# AI Validation

If confidence < threshold

Send to Manual Review Queue.

Original image must remain read-only.

Reviewer cannot modify image.

Reviewer only selects interpreted answer.

Every review recorded.

---

# Anti-Tampering Verification

Every image verification:

Current SHA256

==

Stored SHA256

If mismatch:

Raise Security Alert

Log Event

Notify Administrator

Reject Image

---

# Watermark

Invisible watermark embedded containing:

Exam ID

Timestamp

Random Nonce

Hash

Scanner ID

Invisible to users.

Destroyed if edited.

---

# Audit Log

Every operation recorded.

Examples:

Image Captured

Uploaded

Verified

Viewed

OCR Started

OCR Finished

Manual Review

Score Generated

Result Published

Each log stores:

User

IP

Timestamp

Device

Action

Reason

Logs cannot be modified.

Append-only.

---

# Chain of Custody

Track:

Student

↓

Invigilator

↓

Capture Device

↓

Upload

↓

Server

↓

OCR

↓

Reviewer

↓

Final Score

Every step digitally signed.

---

# Dashboard

Roles:

Operator

Supervisor

Auditor

Administrator

Security Officer

Features:

Upload status

OCR progress

Verification status

Hash verification

Tampering alerts

Statistics

Audit history

Manual review queue

Device management

---

# Alert System

Immediately notify when:

Hash mismatch

Signature invalid

Duplicate upload

Image modified

Unexpected replacement

Suspicious login

Too many failed uploads

Send alerts via:

Email

LINE Notify

Slack

Microsoft Teams

Webhook

---

# API

REST API

GraphQL optional

Endpoints:

POST /capture

POST /upload

GET /verify

GET /audit

POST /ocr

POST /score

GET /results

GET /statistics

---

# Database

PostgreSQL

Tables:

Exam

Student

AnswerSheet

Capture

OCRResult

Question

Answer

Score

AuditLog

Device

Signature

Verification

ManualReview

User

Role

Permission

---

# Tech Stack

Frontend

Flutter

React

TypeScript

Backend

ASP.NET Core 9

C#

Entity Framework Core

PostgreSQL

Redis

RabbitMQ

Docker

OpenCV

ML Kit

Tesseract

JWT Authentication

OpenAPI

---

# Non-Functional Requirements

Support:

100,000+ answer sheets

Concurrent uploads

Offline capture

Automatic sync

Horizontal scaling

Multi-region deployment

Encryption at rest

TLS

Role-based access control

Full auditability

99.9% uptime

---

# Deliverables

Generate:

✔ Complete Solution Architecture

✔ Database Schema

✔ ER Diagram

✔ Clean Architecture

✔ ASP.NET Core Backend

✔ Flutter Mobile App

✔ React Admin Dashboard

✔ OCR Pipeline

✔ OpenCV Image Processing

✔ Hash Verification Service

✔ Digital Signature Service

✔ Immutable Storage Layer

✔ Audit Logging System

✔ Unit Tests

✔ Integration Tests

✔ Docker Compose

✔ Kubernetes Deployment

✔ CI/CD Pipeline

✔ API Documentation

✔ Threat Model

✔ Security Checklist

✔ Sequence Diagrams

✔ Deployment Guide

✔ User Manual

The generated project should follow enterprise software engineering best practices (SOLID, Clean Architecture, CQRS where appropriate, Domain-Driven Design principles, secure coding, and comprehensive automated testing).

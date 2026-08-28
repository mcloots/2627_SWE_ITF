# ITF Pulse  
**The heartbeat of the IT Factory community.**

Welcome to **ITF Pulse**, the official Software Engineering project platform used throughout this course.

This repository contains the **starter structure** for a modern full-stack social media platform where students will design, build, and evolve a scalable application using professional engineering practices.

This project is intentionally structured using:

- Domain Driven Design (DDD)
- Clean Architecture
- Reactive Angular (Signals-based)
- GitFlow branching strategy
- CI/CD-ready repository structure
- Data-intensive design principles

You are expected to **understand the structure**, not just use it.

---

# Project Overview

ITF Pulse is a **social media platform** where users can:

- Create posts  
- Share photos  
- Interact with the IT Factory community  
- Receive notifications  
- Manage profiles  

Over the coming weeks, you will incrementally design and implement the system.

You are not just writing code —  
you are **building software as engineers**.

---

# Repository Structure

```text
itf-pulse/
├─ backend/
│  ├─ src/
│  │  ├─ ITFPulse.Api/
│  │  ├─ ITFPulse.Application/
│  │  ├─ ITFPulse.Domain/
│  │  ├─ ITFPulse.Infrastructure/
│  │  └─ ITFPulse.Contracts/
│  │
│  ├─ tests/
│  │  ├─ ITFPulse.Domain.Tests/
│  │  ├─ ITFPulse.Application.Tests/
│  │  └─ ITFPulse.Architecture.Tests/
│
├─ frontend/
│  ├─ src/
│  │  ├─ app/
│  │  │  ├─ core/
│  │  │  ├─ shared/
│  │  │  ├─ features/
│  │  │  └─ layout/
│
├─ .gitignore
├─ README.md
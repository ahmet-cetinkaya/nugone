# 🏛️ NuGone Project Structure

This document describes the overall structure and organization of the NuGone project, which follows Clean Architecture principles.

---

## 🧭 Overview

NuGone is organized using Clean Architecture to ensure separation of concerns, maintainability, and testability. The main layers are:

- **Core**: Contains the domain and application logic.
  - **NuGone.Domain**: Business logic, entities, and domain interfaces.
  - **NuGone.Application**: Use cases, application services, DTOs, and interfaces for external dependencies.
- **Infrastructure**: Implementations for external systems (file system, NuGet, etc.), data access, and integrations. Additional projects can be created here for each external system if needed.
- **Presentation**: User interface layer.
  - **NuGone.Cli**: Command-line interface, user interaction, and input/output.

---

## 📂 Directory Layout

```
nugone/
├── src/
│   ├── core/
│   │   ├── NuGone.Domain/
│   │   │   ├── features/           # Domain-specific features (e.g., package analysis, project management)
│   │   │   └── shared/             # Shared domain objects, helpers, value objects
│   │   └── NuGone.Application/
│   │       ├── features/           # Application layer features (use cases, services)
│   │       └── shared/             # Shared application services, DTOs, helpers
│   ├── infrastructure/
│   │   └── ...                    # External system integrations (NuGet, file system, etc.)
│   └── presentation/
│       └── NuGone.Cli/
│           ├── features/           # CLI commands and functional features
│           └── shared/             # Shared CLI helpers, utilities, and common code
├── tests/                         # Unit and integration tests
├── docs/                          # Documentation (PRD.md, etc.)
├── README.md                      # Project overview
└── ...                            # Solution files, configs, etc.
```

Additionally, the `features` folders are used to modularize each functional feature. The `shared` folders contain code that is shared across layers or used by multiple features. In the CLI layer, this separation helps keep commands and common CLI helpers organized and maintainable.

---

## 🧩 Layer Responsibilities

### Core
- **NuGone.Domain**
  - Entities (e.g., PackageReference, Project, Solution)
  - Value Objects
  - Domain Interfaces (e.g., IPackageUsageAnalyzer)
  - Pure business logic, no dependencies on other layers
- **NuGone.Application**
  - Use cases (e.g., AnalyzeUnusedPackages, RemoveUnusedPackages)
  - Application services
  - DTOs and input/output models
  - Interfaces for infrastructure (ports)

### Infrastructure
- Implementations for NuGet, file system, config, etc.
- Data access and external service integration
- Implements interfaces defined in Application/Core
- Additional projects can be created for each external system as needed

### Presentation
- **NuGone.Cli**
  - Command-line interface (argument parsing, commands)
  - User interaction, reporting, and output formatting
  - No business logic

---

## 🧪 Testing
- Unit tests for each layer (Core, Application, Infrastructure)
- Integration tests for end-to-end scenarios

---

## 🔗 References
- [Clean Architecture (Uncle Bob)](https://8thlight.com/blog/uncle-bob/2012/08/13/the-clean-architecture.html)
- [.NET Clean Architecture Template](https://github.com/jasontaylordev/CleanArchitecture)

---

For more details, see the [TECH-STACK.md](./TECH-STACK.md) and [PRD.md](./docs/PRD.md).

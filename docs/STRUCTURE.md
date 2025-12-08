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

The directory structure below demonstrates the use of subfolders for common .NET project elements within each layer, following .NET ecosystem conventions. Folder types are chosen according to the responsibilities of each layer (e.g., no Commands or Services in Domain). Folder comments explain their intended contents:

```
nugone/
├── src/
│   ├── core/
│   │   ├── NuGone.Domain/
│   │   │   ├── features/           # Domain-specific features (e.g., packageAnalysis, projectManagement)
│   │   │   │   └── packageAnalysis/
│   │   │   │       ├── Entities/       # Core domain entities (e.g., PackageReference, Project)
│   │   │   │       ├── ValueObjects/   # Domain value objects
│   │   │   │       ├── Interfaces/     # Domain interfaces (e.g., IPackageUsageAnalyzer)
│   │   │   │       └── ...
│   │   │   └── shared/             # Shared domain objects, helpers, value objects
│   │   │       ├── ValueObjects/       # Shared value objects
│   │   │       ├── Helpers/            # Domain helper classes
│   │   │       └── ...
│   │   └── NuGone.Application/
│   │       ├── features/           # Application layer features (e.g., packageAnalysis, packageRemoval)
│   │       │   └── packageRemoval/
│   │       │       ├── Commands/       # Command objects and handlers (CQRS, MediatR, etc.)
│   │       │       ├── Services/       # Application/business services
│   │       │       ├── Models/         # DTOs and input/output models
│   │       │       ├── Interfaces/     # Application-level interfaces (ports)
│   │       │       └── ...
│   │       └── shared/             # Shared application services, DTOs, helpers
│   │           ├── DTOs/               # Shared data transfer objects
│   │           ├── Helpers/            # Application helper classes
│   │           └── ...
│   ├── infrastructure/
│   │   ├── NuGone.NuGet/           # NuGet integration and data access
│   │   │   ├── Services/               # Infrastructure services (NuGet-specific)
│   │   │   ├── Repositories/           # Data access implementations
│   │   │   ├── Models/                 # Infrastructure models
│   │   │   └── ...
│   │   ├── NuGone.FileSystem/      # File system integration and data access
│   │   │   ├── Services/
│   │   │   ├── Repositories/
│   │   │   └── ...
│   │   └── ...                     # Other external system integrations (NuGet, file system, etc.)
│   └── presentation/
│       └── NuGone.Cli/
│           ├── features/           # CLI commands and functional features (e.g., analyzeCommand, removeCommand)
│           │   └── analyzeCommand/
│           │       ├── Commands/       # CLI command implementations
│           │       ├── Services/       # CLI-specific services
│           │       ├── Models/         # CLI models
│           │       └── ...
│           └── shared/             # Shared CLI helpers, utilities, and common code
│               ├── Utilities/          # CLI utility classes
│               ├── Constants/          # CLI constants
│               └── ...
├── tests/                         # Unit and integration tests
│   ├── ...
│   └── presentation/  # Example for a test
│       └── NuGone.Cli.Tests/
│           └── features/
│               └── analyzeCommand/
│                   └── Commands/
│                       └── AnalyzeCommandTests.cs  # Example command test file
│           └── ...
├── docs/                          # Documentation (PRD.md, etc.)
├── README.md                      # Project overview
└── ...                            # Solution files, configs, etc.
```

> In the example above, each feature and shared folder is further organized by project element type, but only those appropriate for the layer (e.g., Domain contains Entities, ValueObjects, Interfaces; Application contains Commands, Services, Models, etc.). Folder comments explain their intended contents and responsibilities.

> Additionally, the `features` folders are used to modularize each functional feature. The `shared` folders contain code that is shared across layers or used by multiple features. In the CLI layer, this separation helps keep commands and common CLI helpers organized and maintainable.

---

## 🧩 Layer Responsibilities

### Core

- **NuGone.Domain**
    - Entities (e.g., packageReference, project, solution)
    - Value Objects
    - Domain Interfaces (e.g., iPackageUsageAnalyzer)
    - Pure business logic, no dependencies on other layers
- **NuGone.Application**
    - Use cases (e.g., packageAnalysis, packageRemoval)
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

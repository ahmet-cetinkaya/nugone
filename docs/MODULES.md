# NuGone Modules

This document defines the unified module structure for the NuGone project. All features and responsibilities are grouped under a single modular architecture to ensure consistency and maintainability across the project, as aligned with the PRD and RFC documents.

---

## NuGone Main Module

- **Package Analysis:** Reads `<PackageReference>` entries in project files, distinguishes between direct and transitive dependencies, and detects unused NuGet packages in the codebase.
- **Project Management:** Reads project and solution files (csproj, sln, slnx, Directory.Packages.props) and supports central package management.
- **Package Removal:** Safely removes unused packages from the project, manages backup and rollback mechanisms.
- **Reporting:** Reports used and unused packages, generates summary and detailed outputs (JSON, text).
- **Configuration:** Reads settings from `global.json` or legacy config files and applies exclude patterns.
- **CLI:** Handles all CLI commands (`analyze`, `remove`, `config`), generates outputs, and manages user interaction.

---

This unified module structure is designed to keep the project sustainable, modular, and extensible, while avoiding fragmentation and duplication across the codebase.

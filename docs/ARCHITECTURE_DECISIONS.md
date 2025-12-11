# Architecture Decisions

This document records significant architectural decisions made during NuGone's development using the Architecture Decision Record (ADR) format.

## Table of Contents

- [ADR Format](#adr-format)
- [ADR-001: Clean Architecture](#adr-001-clean-architecture)
- [ADR-002: CQRS Pattern](#adr-002-cqrs-pattern)
- [ADR-003: Spectre.Console.Cli Framework](#adr-003-spectreconsolecli-framework)
- [ADR-004: System.IO.Abstractions](#adr-004-systemioabstractions)
- [ADR-005: Multi-Target Framework Support](#adr-005-multi-target-framework-support)
- [ADR-006: LoggerMessage Source Generator](#adr-006-loggermessage-source-generator)
- [ADR-007: RFC-Driven Development](#adr-007-rfc-driven-development)
- [ADR-008: Migration from nugone.json to global.json](#adr-008-migration-from-nugonejson-to-globaljson)

## ADR Format

Each ADR follows this structure:

- **Status**: Proposed | Accepted | Deprecated | Superseded
- **Context**: What is the issue that we're seeing that is motivating this decision?
- **Decision**: What is the change that we're proposing?
- **Consequences**: What becomes easier or more difficult to do because of this change?

---

## ADR-001: Clean Architecture

**Status**: Accepted

**Context**:

- Need for clear separation of concerns between CLI, business logic, domain models, and external integrations
- Requirements to test business logic independently of frameworks
- Future need for multiple UI interfaces (CLI, potential GUI, API)
- Desire to keep the codebase maintainable as features grow

**Decision**:
Adopt Clean Architecture with four distinct layers:

1. **Presentation Layer**: CLI interface and user interaction
2. **Application Layer**: Business logic, use cases, and CQRS commands/handlers
3. **Domain Layer**: Core entities and business rules
4. **Infrastructure Layer**: External system integrations (file system, NuGet)

**Consequences**:

**Positive**:

- Clear dependency direction: Dependencies point inward
- Easy to test business logic in isolation
- Frameworks are treated as details, not core concerns
- Each layer has a single responsibility
- Enables future extension to other interfaces

**Negative**:

- Increased initial complexity for simple features
- More boilerplate code for simple operations
- Learning curve for team members unfamiliar with Clean Architecture

**Neutral**:

- Requires discipline to maintain layer boundaries
- More files to navigate for simple changes

---

## ADR-002: CQRS Pattern

**Status**: Accepted

**Context**:

- Need for clear separation between read and write operations
- Commands in CLI naturally map to command objects
- Different requirements for command execution vs. result presentation
- Future scalability requirements for complex operations

**Decision**:
Implement Command Query Responsibility Segregation (CQRS) pattern:

- Commands represent user intentions (e.g., `AnalyzePackageUsageCommand`)
- Handlers process commands and return results
- Queries are handled through repository pattern
- Results are specialized data transfer objects

**Consequences**:

**Positive**:

- Clear intent for each operation
- Easy to add new commands without affecting existing ones
- Natural fit for CLI tool architecture
- Separation of concerns between command validation, execution, and result formatting
- Enables future features like undo/redo through command pattern

**Negative**:

- More classes to maintain for simple operations
- Initial learning curve for understanding the pattern
- Potential over-engineering for very simple commands

**Neutral**:

- Consistent pattern across all operations
- Requires understanding of CQRS concepts

---

## ADR-003: Spectre.Console.Cli Framework

**Status**: Accepted

**Context**:

- Need for a robust CLI framework with modern .NET support
- Requirements for help text generation, command validation
- Desire for aesthetically pleasing console output
- Support for async command execution
- Compatibility with dependency injection

**Decision**:
Choose Spectre.Console.Cli as the CLI framework due to:

- Modern .NET design with async support
- Built-in dependency injection integration
- Automatic help generation
- Rich console output capabilities (tables, colors, progress bars)
- Strong typing and command validation

**Considered Alternatives**:

- **System.CommandLine**: Microsoft's offering but more complex DI setup
- **McMaster.Extensions.CommandLineUtils**: Older, less maintained
- **Custom CLI parsing**: Would require building all features from scratch

**Consequences**:

**Positive**:

- Professional-looking CLI output
- Automatic help and validation
- Clean integration with .NET DI container
- Active development and maintenance
- Excellent documentation and examples

**Negative**:

- Additional dependency to manage
- Learning curve for advanced features
- Some limitations in complex command hierarchies

**Neutral**:

- Follows opinionated design patterns
- Requires Spectre.Console understanding for custom styling

---

## ADR-004: System.IO.Abstractions

**Status**: Accepted

**Context**:

- Need to test file system operations without actual file I/O
- Requirements to mock file system behavior in unit tests
- Desire to isolate file system operations for better testability
- Support for different file system behaviors across platforms

**Decision**:
Use System.IO.Abstractions package to provide an abstraction layer over file system operations:

- `IFileSystem` interface for all file operations
- `IFile`, `IDirectory`, `IPath` abstractions
- Mockable implementations for testing
- Consistent API across platforms

**Consequences**:

**Positive**:

- Easy to mock file system in unit tests
- Consistent behavior across different platforms
- Can simulate various error conditions in tests
- Clear separation of file system logic from business logic
- Enables test-driven development

**Negative**:

- Additional abstraction layer to understand
- Slight performance overhead (negligible for CLI tool)
- More dependencies in the project

**Neutral**:

- Requires injection of file system abstractions
- Team needs to understand when to use abstractions vs. direct System.IO

---

## ADR-005: Multi-Target Framework Support

**Status**: Accepted

**Context**:

- .NET ecosystem moving toward .NET 8 LTS and .NET 9
- Need to support users on different .NET versions
- Desire to future-proof the tool for .NET 10
- CI/CD infrastructure supports multiple frameworks

**Decision**:
Target multiple .NET frameworks simultaneously:

- .NET 8.0 (LTS, broad enterprise support)
- .NET 9.0 (current, latest features)
- .NET 10.0 (future, preview compatibility)

Implemented via:

```xml
<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
```

**Consequences**:

**Positive**:

- Users can run on their preferred .NET version
- Future-proofed for upcoming .NET releases
- Demonstrates modern .NET development practices
- Flexibility for enterprise adoption requirements

**Negative**:

- Larger download size (multiple framework binaries)
- More complex CI/CD matrix
- Potential for framework-specific bugs
- Longer build times

**Neutral**:

- Requires testing on multiple frameworks
- Framework-specific features need careful handling

---

## ADR-006: LoggerMessage Source Generator

**Status**: Accepted

**Context**:

- Need for high-performance logging in analysis operations
- Requirement to avoid boxing allocations and string concatenations
- Desire for structured logging with proper log levels
- Modern .NET logging best practices

**Decision**:
Use LoggerMessage source generator for high-performance logging:

- Compile-time generation of logging code
- Zero allocations in hot paths
- Strongly-typed logging methods
- Integration with Microsoft.Extensions.Logging

**Consequences**:

**Positive**:

- Excellent performance for high-frequency operations
- Compile-time checking of log messages
- Structured logging support
- No boxing or string concatenation overhead

**Negative**:

- More verbose to define log messages
- Learning curve for LoggerMessage syntax
- Slightly more complex than simple Console.WriteLine

**Neutral**:

- Requires understanding of source generators
- Different pattern than traditional logging

---

## ADR-007: RFC-Driven Development

**Status**: Accepted

**Context**:

- Need to document major design decisions
- Requirements to involve community/stakeholders in design
- Desire for consistent, well-considered feature additions
- Historical record of why certain decisions were made

**Decision**:
Implement RFC (Request for Comments) process:

- Write RFCs for significant features or architectural changes
- Public review period for feedback
- Explicit acceptance/rejection with rationale
- Archive of all RFCs in version control

**Consequences**:

**Positive**:

- Well-documented design decisions
- Community involvement in major features
- Reduced risk of architectural mistakes
- Clear communication of design intent
- Historical record for future maintainers

**Negative**:

- Slower initial feature development
- Additional documentation overhead
- Requires discipline to maintain process
- Potential for RFC process to become bureaucratic

**Neutral**:

- Cultural shift required for team
- Balance needed between RFC process and agility

---

## ADR-008: Migration from nugone.json to global.json

**Status**: Accepted

**Context**:

- Need to standardize configuration with .NET ecosystem
- Requirements to support .NET SDK version pinning
- Desire to reduce configuration file proliferation
- Industry trend toward global.json for .NET tools

**Decision**:
Migrate configuration from custom `nugone.json` to standard `global.json` format:

**Old Format (nugone.json)**:

```json
{
    "excludeNamespaces": ["Microsoft.AspNetCore.Mvc"],
    "excludePackages": ["Microsoft.AspNetCore.App"],
    "excludeFiles": ["**/*.Designer.cs"]
}
```

**New Format (global.json)**:

```json
{
    "sdk": {
        "version": "8.0.100",
        "rollForward": "latestFeature"
    },
    "nugone": {
        "excludeNamespaces": ["Microsoft.AspNetCore.Mvc"],
        "excludePackages": ["Microsoft.AspNetCore.App"],
        "excludeFiles": ["**/*.Designer.cs"]
    }
}
```

**Consequences**:

**Positive**:

- Standard .NET configuration file
- SDK version management integrated
- Single configuration file for .NET settings
- Better tooling support for global.json
- Industry standard approach

**Negative**:

- Breaking change for existing users
- Migration required for v1.x users
- Less discoverable for NuGone-specific settings
- Potential conflicts with other tools using global.json

**Neutral**:

- Requires educating users about the change
- Need to provide migration tooling/guidance

---

## Evolution of Architecture

These decisions show NuGone's evolution from a simple CLI tool to a well-architected, extensible platform. Key themes:

1. **Testability**: Clean Architecture and abstractions enable comprehensive testing
2. **Performance**: LoggerMessage and multi-target support ensure efficiency
3. **Maintainability**: RFC process and documented decisions support long-term health
4. **Developer Experience**: Spectre.Console and standard configurations improve usability

## Future Considerations

Potential future architectural decisions to consider:

- Plugin architecture for custom analyzers
- Distributed analysis capabilities
- Integration with package managers
- Advanced visualization and reporting features

## Related Documentation

- [STRUCTURE.md](STRUCTURE.md) - Current architecture overview
- [RFCs](RFCS/) - Detailed feature design documents
- [API_REFERENCE.md](API_REFERENCE.md) - Public API documentation

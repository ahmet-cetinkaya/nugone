# RFC-0002: Unused Package Detection Algorithm

## Related Module

**Package Analysis**

## Summary

Define the algorithm and heuristics for detecting unused NuGet packages in .NET projects, balancing accuracy, performance, and maintainability.

## Motivation

Accurate detection of unused packages is the core value proposition of NuGone. The algorithm must minimize false positives/negatives, scale to large solutions, and support various project types.

## Detailed Design

### Package Reference Discovery

- Parse all `<PackageReference>` entries in project files and central props files.
- Distinguish between direct and transitive dependencies.
- Handle conditional references (e.g., `<Condition>` attributes).

### Usage Scanning

- For each package, scan codebase for:
    - `using` statements referencing the package's namespaces.
    - Class or method names from the package used in code.
- Exclude files/folders by user-defined patterns (e.g., `**/Generated/**`).
- Support multi-targeted projects.
- (Planned) Reflection-based usage detection for advanced scenarios.

### Performance

- Parallelized file scanning for large solutions.
- Exclude known auto-generated files (e.g., `.Designer.cs`, `.g.cs`) by default.
- Target analysis completion under 2 minutes for 5,000+ files.

### Input Validation

- Validate all project and file paths.
- Sanitize user-supplied patterns and config.

## Alternatives Considered

- Reflection-based analysis as default (deferred for performance/complexity).
- Relying solely on `using` statements (rejected for insufficient accuracy).

## Drawbacks

- Source generators and dynamic usage may cause false positives/negatives.

## Adoption

- All detection logic must follow this algorithm.
- Future improvements (e.g., reflection) must be RFC'd.

## References

- [PRD.md](../PRD.md)
- [TECH-STACK.md](../TECH-STACK.md)

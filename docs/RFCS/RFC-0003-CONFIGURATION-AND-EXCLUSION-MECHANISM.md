# RFC-0003: Configuration And Exclusion Mechanism

## Related Module

**Configuration**

## Summary

Specify how NuGone loads configuration, applies exclusion patterns, and determines precedence for settings.

## Motivation

Flexible configuration and exclusion are essential for accurate analysis and user control, especially in large or complex solutions.

## Detailed Design

### Configuration Sources
- Primary: `global.json` at solution root, under the `nugone` object.
- Fallback: Legacy config file (JSON) if `global.json` is absent.
- Defaults: Hardcoded defaults if no config is found.

### Precedence
- `global.json` > legacy config > defaults.
- Config file location can be specified via CLI flag.

### Exclusion Patterns
- Support glob patterns for files/folders (e.g., `**/*.Designer.cs`, `**/Generated/**`).
- Exclude namespaces, files, or folders as arrays in config.
- Patterns are validated and sanitized before use.

### Example Config
```json
{
  "nugone": {
    "excludeNamespaces": ["System.Text.Json"],
    "excludeFiles": ["**/*.Designer.cs", "**/Generated/**"]
  }
}
```

### Input Validation
- All config values are validated for type and format.
- Invalid config results in informative error messages and safe fallback.

## Alternatives Considered
- Only CLI flags (rejected for lack of persistence and scalability).
- XML-based config (rejected for consistency with .NET ecosystem).

## Drawbacks
- Multiple config sources may cause confusion if not documented.

## Adoption
- All configuration and exclusion logic must follow this mechanism.
- Documentation must clearly explain precedence and patterns.

## References
- [PRD.md](../PRD.md)
- [STRUCTURE.md](../STRUCTURE.md)

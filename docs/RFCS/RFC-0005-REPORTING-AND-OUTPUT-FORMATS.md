# RFC-0005: Reporting And Output Formats

## Summary

Define the structure, formats, and extensibility of NuGone's analysis reports.

## Motivation

Clear, actionable reports are essential for users to understand and act on analysis results. Support for multiple formats enables integration with CI/CD and other tools.

## Detailed Design

### Report Contents
- List of used packages
- List of unused packages (with name, version, references found)
- Summary: total unused, total scanned, percentage unused
- Project/solution name

### Output Formats
- Plain text (default, human-readable)
- JSON (machine-readable, for automation)
- Option to write report to file via `--output <file>`

### Example JSON Output
```json
{
  "unusedPackages": [
    {
      "name": "Newtonsoft.Json",
      "version": "13.0.3",
      "referencesFound": 0
    }
  ],
  "scannedPackages": 10,
  "project": "MyProject.csproj"
}
```

### Extensibility
- New formats can be added via modular report generators.
- CLI flag `--format <type>` selects output format.

### Input Validation
- Validate output file paths and format types.

## Alternatives Considered
- Only plain text output (rejected for lack of automation support).

## Drawbacks
- Supporting multiple formats adds maintenance overhead.

## Adoption
- All reporting logic must follow this structure and support at least text and JSON.

## References
- [PRD.md](../PRD.md)

#!/bin/bash

# Lint script for NuGone project
# This script runs various linting checks on the codebase

set -e

# Source common output functions
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/_common.sh"

print_header "🔍 Running NuGone project linting"

# Detect correct dotnet command (prefer dnvm over system)
DNVM_DOTNET="$HOME/.local/share/dnvm/dn/dotnet"
if [ -x "$DNVM_DOTNET" ]; then
  DOTNET_CMD="$DNVM_DOTNET"
  print_info "Using dnvm-managed dotnet"
elif command -v dotnet &>/dev/null; then
  DOTNET_CMD="dotnet"
  print_info "Using system dotnet"
else
  print_error ".NET SDK is not installed"
  exit 1
fi

# Check .NET SDK version
print_info "Checking .NET SDK version..."
if DOTNET_VERSION=$($DOTNET_CMD --version 2>/dev/null); then
  print_info "Found .NET SDK version: $DOTNET_VERSION"
  # Check if version is at least 8.0 (minimum required by the project)
  MAJOR_VERSION=$(echo "$DOTNET_VERSION" | cut -d. -f1)
  if [ "$MAJOR_VERSION" -lt 8 ]; then
    print_error ".NET SDK 8.0 or higher is required for this project"
    print_info "Download .NET 8.0+ SDK from: https://dotnet.microsoft.com/download"
    exit 1
  fi
else
  print_error ".NET SDK is not installed or not in PATH"
  exit 1
fi

# Check if we're in the right directory
if [ ! -f "NuGone.sln" ] && [ ! -f "NuGone.slnx" ]; then
  print_error "No solution file found. Please run from the project root."
  exit 1
fi

# Get the solution file - prefer .sln over .slnx
SOLUTION_FILE=""
if [ -f "NuGone.sln" ]; then
  SOLUTION_FILE="./NuGone.sln"
elif [ -f "NuGone.slnx" ]; then
  SOLUTION_FILE="./NuGone.slnx"
  print_warning "Found .slnx file which is not supported by many .NET tools yet"
  print_info "Will use individual project files for build and analysis"
fi

if [ -z "$SOLUTION_FILE" ]; then
  print_error "No solution file found in current directory."
  exit 1
fi
print_info "Found solution file: $SOLUTION_FILE"

# Check if we can use the solution file or need to use project files
USE_PROJECTS=false
if [[ "$SOLUTION_FILE" == *.slnx ]]; then
  USE_PROJECTS=true
  # Get all .csproj files
  PROJECT_FILES=$(find src -name "*.csproj" 2>/dev/null)
  if [ -z "$PROJECT_FILES" ]; then
    print_error "No .csproj files found in src/ directory"
    exit 1
  fi
  print_info "Will build individual projects instead of solution"
fi

# Track overall success
OVERALL_SUCCESS=0

print_section "📦 Restoring NuGet packages"
if [ "$USE_PROJECTS" = true ]; then
  # Restore each project individually
  RESTORE_SUCCESS=true
  while IFS= read -r project; do
    if ! $DOTNET_CMD restore "$project"; then
      RESTORE_SUCCESS=false
    fi
  done <<<"$PROJECT_FILES"

  if [ "$RESTORE_SUCCESS" = true ]; then
    print_success "Package restore completed successfully"
  else
    print_error "Package restore failed"
    OVERALL_SUCCESS=1
  fi
else
  # Restore the entire solution
  if $DOTNET_CMD restore "$SOLUTION_FILE"; then
    print_success "Package restore completed successfully"
  else
    print_error "Package restore failed"
    OVERALL_SUCCESS=1
  fi
fi

print_section "🏗️  Running dotnet build (treat warnings as errors)"
if [ "$USE_PROJECTS" = true ]; then
  # Build each project individually
  BUILD_SUCCESS=true
  while IFS= read -r project; do
    if ! $DOTNET_CMD build "$project" --no-restore --configuration Release --verbosity normal; then
      BUILD_SUCCESS=false
    fi
  done <<<"$PROJECT_FILES"

  if [ "$BUILD_SUCCESS" = true ]; then
    print_success "Build completed successfully with no warnings"
  else
    print_error "Build failed or has warnings that are treated as errors"
    OVERALL_SUCCESS=1
  fi
else
  # Build the entire solution
  if $DOTNET_CMD build "$SOLUTION_FILE" --no-restore --configuration Release --verbosity normal; then
    print_success "Build completed successfully with no warnings"
  else
    print_error "Build failed or has warnings that are treated as errors"
    OVERALL_SUCCESS=1
  fi
fi

print_section "📝 Checking CSharpier formatting"
if $DOTNET_CMD csharpier --version &>/dev/null 2>&1; then
  CSHARPIER_CMD="$DOTNET_CMD csharpier"

  if $CSHARPIER_CMD format . --skip-write >/dev/null 2>&1; then
    print_success "C# files are properly formatted"
  else
    print_error "C# files are not properly formatted"
    print_info "Run './scripts/format.sh' to fix formatting issues"
    OVERALL_SUCCESS=1
  fi
else
  print_warning "CSharpier is not installed"
  print_info "Install it with: dotnet tool install -g csharpier"
fi

print_section "📄 Checking Prettier formatting"
if command -v prettier &>/dev/null; then
  if prettier --check "**/*.{json,yml,yaml,js,ts,jsx,tsx,css,scss,html}" \
    --ignore-path=.gitignore \
    --ignore-path=.prettierignore >/dev/null 2>&1; then
    print_success "Prettier-formatted files are properly formatted"
  else
    print_error "Some files are not properly formatted"
    print_info "Run './scripts/format.sh' to fix formatting issues"
    OVERALL_SUCCESS=1
  fi
else
  print_warning "prettier is not installed"
  print_info "Install it with: npm install -g prettier"
fi

print_section "📝 Running markdownlint checks"
if command -v markdownlint-cli2 &>/dev/null; then
  if markdownlint-cli2 "##*.md" >/dev/null 2>&1; then
    print_success "Markdown files passed linting checks"
  else
    print_error "Markdown files have linting issues"
    print_info "Run 'markdownlint-cli2 \"##*.md\" --fix' to fix markdown issues"
    OVERALL_SUCCESS=1
  fi
else
  print_warning "markdownlint-cli2 is not installed"
  print_info "Install it with: npm install -g markdownlint-cli2"
fi

print_section "🔍 Running Roslynator analysis"
if [ "$USE_PROJECTS" = true ]; then
  print_warning "Skipping Roslynator analysis - .slnx format is not supported by Roslynator"
  print_info "Consider generating a .sln file or running Roslynator on individual projects"
else
  if $DOTNET_CMD tool list | grep -q "roslynator.dotnet.cli"; then
    ROSLYNATOR_CMD="$DOTNET_CMD roslynator"
  elif command -v roslynator &>/dev/null; then
    ROSLYNATOR_CMD="roslynator"
  else
    print_warning "Roslynator is not installed"
    print_info "Install it with: dotnet tool restore"
  fi

  # Only run Roslynator if it was found
  if [ -n "$ROSLYNATOR_CMD" ]; then
    print_info "Analyzing code with Roslynator..."
    # Use --severity-level warning to only report warnings and errors, not info diagnostics
    if $ROSLYNATOR_CMD analyze "$SOLUTION_FILE"; then
      print_success "Roslynator analysis completed successfully"
    else
      print_error "Roslynator found issues"
      OVERALL_SUCCESS=1
    fi
  fi
fi

print_section "🔍 Running additional code analysis"

# Check for common .NET code issues
print_info "Checking for TODO/FIXME/HACK comments..."
TODO_COUNT=$(grep -r -i -n "TODO\|FIXME\|HACK" --include="*.cs" src/ 2>/dev/null | wc -l || echo "0")
if [ "$TODO_COUNT" -gt 0 ]; then
  print_warning "Found $TODO_COUNT TODO/FIXME/HACK comments in source code"
  grep -r -i -n "TODO\|FIXME\|HACK" --include="*.cs" src/ 2>/dev/null || true
  print_info "Consider addressing these items or creating issues for them"
fi

# Check for hardcoded secrets using gitleaks
print_info "Checking for potential secrets with gitleaks..."
if command -v gitleaks &>/dev/null; then
  # Run gitleaks detect in no-git mode (for current directory scanning)
  if gitleaks detect --no-git --verbose --redact --exit-code 0 >/dev/null 2>&1; then
    print_success "No secrets detected"
  else
    print_warning "gitleaks found potential secrets"
    gitleaks detect --no-git --verbose --redact --exit-code 0 || true
    print_info "Review and remove any hardcoded secrets before committing"
  fi
else
  print_warning "gitleaks is not installed, falling back to basic pattern check"
  # Fallback to basic pattern checking
  SECRET_COUNT=$(grep -r -i -n "password\|secret\|apikey\|connectionstring" --include="*.cs" src/ 2>/dev/null | grep -vc "//.*password\|//.*secret\|//.*apikey\|//.*connectionstring" || echo "0")
  if [ "$SECRET_COUNT" -gt 0 ]; then
    print_warning "Found $SECRET_COUNT potential hardcoded secrets"
    grep -r -i -n "password\|secret\|apikey\|connectionstring" --include="*.cs" src/ 2>/dev/null | grep -v "//.*password\|//.*secret\|//.*apikey\|//.*connectionstring" || true
    print_info "Review and remove any hardcoded secrets before committing"
  fi
fi

# Check for proper XML documentation on public APIs
print_info "Checking for missing XML documentation..."
DOC_MISSING_COUNT=0
PUBLIC_API_FILES=$(find src/ -name "*.cs" -exec grep -l "public.*class\|public.*interface\|public.*enum" {} \; 2>/dev/null)
if [ -n "$PUBLIC_API_FILES" ]; then
  for file in $PUBLIC_API_FILES; do
    if [ -f "$file" ] && ! grep -q "///" "$file" 2>/dev/null; then
      print_info "Consider adding XML documentation for public APIs in: $file"
      DOC_MISSING_COUNT=$((DOC_MISSING_COUNT + 1))
    fi
  done
  if [ "$DOC_MISSING_COUNT" -gt 0 ]; then
    print_info "Found $DOC_MISSING_COUNT files with public APIs missing XML documentation"
  fi
fi

echo ""
if [ $OVERALL_SUCCESS -eq 0 ]; then
  print_success "🎉 All linting checks passed!"
else
  print_error "❌ Some linting checks failed. Please fix the issues above."
  exit 1
fi

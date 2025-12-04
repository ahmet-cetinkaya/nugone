#!/bin/bash

# Lint script for NuGone project
# This script runs various linting checks on the codebase

set -e

# Source common output functions
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/_common.sh"

print_header "🔍 Running NuGone project linting"

# Check if we're in the right directory
if [ ! -f "NuGone.sln" ] && [ ! -f "NuGone.slnx" ]; then
  print_error "No solution file found. Please run from the project root."
  exit 1
fi

# Get the solution file
SOLUTION_FILE=$(find . -maxdepth 1 -name "*.sln" -o -name "*.slnx" | head -n 1)
if [ -z "$SOLUTION_FILE" ]; then
  print_error "No solution file found in current directory."
  exit 1
fi
print_info "Found solution file: $SOLUTION_FILE"

# Track overall success
OVERALL_SUCCESS=0

print_section "🏗️  Running dotnet build (treat warnings as errors)"
if dotnet build "$SOLUTION_FILE" --no-restore --configuration Release --verbosity normal; then
  print_success "Build completed successfully with no warnings"
else
  print_error "Build failed or has warnings that are treated as errors"
  OVERALL_SUCCESS=1
fi

print_section "📝 Checking CSharpier formatting"
if command -v dotnet &>/dev/null && dotnet csharpier --version &>/dev/null 2>&1; then
  CSHARPIER_CMD="dotnet csharpier"

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
  if prettier --check "**/*.{md,json,yml,yaml,js,ts,jsx,tsx,css,scss,html}" \
    --ignore-path=.gitignore \
    --ignore-path=.prettierignore >/dev/null 2>&1; then
    print_success "Markdown and other files are properly formatted"
  else
    print_error "Some files are not properly formatted"
    print_info "Run './scripts/format.sh' to fix formatting issues"
    OVERALL_SUCCESS=1
  fi
else
  print_warning "prettier is not installed"
  print_info "Install it with: npm install -g prettier"
fi

print_section "🔍 Running Roslynator analysis"
if dotnet tool list | grep -q "roslynator.dotnet.cli"; then
    ROSLYNATOR_CMD="dotnet roslynator"
elif command -v roslynator &>/dev/null; then
    ROSLYNATOR_CMD="roslynator"
else
    print_warning "Roslynator is not installed"
    print_info "Install it with: dotnet tool restore"
fi

# Only run Roslynator if it was found
if [ -n "$ROSLYNATOR_CMD" ]; then
    print_info "Analyzing code with Roslynator..."
    if $ROSLYNATOR_CMD analyze "$SOLUTION_FILE"; then
        print_success "Roslynator analysis completed successfully"
    else
        print_error "Roslynator found issues"
        OVERALL_SUCCESS=1
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
  SECRET_COUNT=$(grep -r -i -n "password\|secret\|apikey\|connectionstring" --include="*.cs" src/ 2>/dev/null | grep -v "//.*password\|//.*secret\|//.*apikey\|//.*connectionstring" | wc -l || echo "0")
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

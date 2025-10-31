#!/bin/bash

# Lint script for NuGone project
# This script runs various linting checks on the codebase

set -e

echo "🔍 Running NuGone project linting..."

# Check if we're in the right directory
if [ ! -f "NuGone.sln" ] && [ ! -f "NuGone.slnx" ]; then
  echo "❌ Error: No solution file found. Please run from the project root."
  exit 1
fi

# Get the solution file
SOLUTION_FILE=$(find . -maxdepth 1 -name "*.sln" -o -name "*.slnx" | head -n 1)
if [ -z "$SOLUTION_FILE" ]; then
  echo "❌ Error: No solution file found in current directory."
  exit 1
fi
echo "📁 Found solution file: $SOLUTION_FILE"

# Track overall success
OVERALL_SUCCESS=0

echo ""
echo "🏗️  Running dotnet build (treat warnings as errors)..."
if dotnet build "$SOLUTION_FILE" --no-restore --configuration Release --verbosity normal; then
  echo "✅ Build completed successfully with no warnings"
else
  echo "❌ Build failed or has warnings that are treated as errors"
  OVERALL_SUCCESS=1
fi

echo ""
echo "📝 Checking CSharpier formatting..."
if command -v dotnet &>/dev/null && dotnet csharpier --version &>/dev/null 2>&1; then
  CSHARPIER_CMD="dotnet csharpier"

  if $CSHARPIER_CMD format . --skip-write >/dev/null 2>&1; then
    echo "✅ C# files are properly formatted"
  else
    echo "❌ C# files are not properly formatted"
    echo "💡 Run './scripts/format.sh' to fix formatting issues"
    OVERALL_SUCCESS=1
  fi
else
  echo "⚠️  Warning: CSharpier is not installed"
  echo "💡 Install it with: dotnet tool install -g csharpier"
fi

echo ""
echo "📄 Checking Prettier formatting..."
if command -v prettier &>/dev/null; then
  if prettier --check "**/*.{md,json,yml,yaml,js,ts,jsx,tsx,css,scss,html}" \
    --ignore-path=.gitignore \
    --ignore-path=.prettierignore >/dev/null 2>&1; then
    echo "✅ Markdown and other files are properly formatted"
  else
    echo "❌ Some files are not properly formatted"
    echo "💡 Run './scripts/format.sh' to fix formatting issues"
    OVERALL_SUCCESS=1
  fi
else
  echo "⚠️  Warning: prettier is not installed"
  echo "💡 Install it with: npm install -g prettier"
fi

echo ""
echo "🔍 Running Roslynator analysis..."
if command -v roslynator &>/dev/null; then
  ROSLYNATOR_CMD="roslynator"

  echo "  - Analyzing code with Roslynator..."
  if $ROSLYNATOR_CMD analyze "$SOLUTION_FILE"; then
    echo "✅ Roslynator analysis completed successfully"
  else
    echo "❌ Roslynator found issues"
    OVERALL_SUCCESS=1
  fi
else
  echo "⚠️  Warning: Roslynator is not installed"
  echo "💡 Install it with: dotnet tool install -g roslynator"
fi

echo ""
echo "🔍 Running additional code analysis..."

# Check for common .NET code issues
echo "  - Checking for TODO/FIXME/HACK comments..."
TODO_COUNT=$(grep -r -i -n "TODO\|FIXME\|HACK" --include="*.cs" src/ 2>/dev/null | wc -l || echo "0")
if [ "$TODO_COUNT" -gt 0 ]; then
  echo "⚠️  Warning: Found $TODO_COUNT TODO/FIXME/HACK comments in source code"
  grep -r -i -n "TODO\|FIXME\|HACK" --include="*.cs" src/ 2>/dev/null || true
  echo "💡 Consider addressing these items or creating issues for them"
fi

# Check for hardcoded secrets using gitleaks
echo "  - Checking for potential secrets with gitleaks..."
if command -v gitleaks &>/dev/null; then
  # Run gitleaks detect in no-git mode (for current directory scanning)
  if gitleaks detect --no-git --verbose --redact --exit-code 0 >/dev/null 2>&1; then
    echo "✅ No secrets detected"
  else
    echo "⚠️  Warning: gitleaks found potential secrets"
    gitleaks detect --no-git --verbose --redact --exit-code 0 || true
    echo "💡 Review and remove any hardcoded secrets before committing"
  fi
else
  echo "⚠️  Warning: gitleaks is not installed, falling back to basic pattern check"
  # Fallback to basic pattern checking
  SECRET_COUNT=$(grep -r -i -n "password\|secret\|apikey\|connectionstring" --include="*.cs" src/ 2>/dev/null | grep -v "//.*password\|//.*secret\|//.*apikey\|//.*connectionstring" | wc -l || echo "0")
  if [ "$SECRET_COUNT" -gt 0 ]; then
    echo "⚠️  Warning: Found $SECRET_COUNT potential hardcoded secrets"
    grep -r -i -n "password\|secret\|apikey\|connectionstring" --include="*.cs" src/ 2>/dev/null | grep -v "//.*password\|//.*secret\|//.*apikey\|//.*connectionstring" || true
    echo "💡 Review and remove any hardcoded secrets before committing"
  fi
fi

# Check for proper XML documentation on public APIs
echo "  - Checking for missing XML documentation..."
DOC_MISSING_COUNT=0
PUBLIC_API_FILES=$(find src/ -name "*.cs" -exec grep -l "public.*class\|public.*interface\|public.*enum" {} \; 2>/dev/null)
if [ -n "$PUBLIC_API_FILES" ]; then
  for file in $PUBLIC_API_FILES; do
    if [ -f "$file" ] && ! grep -q "///" "$file" 2>/dev/null; then
      echo "ℹ️  Consider adding XML documentation for public APIs in: $file"
      DOC_MISSING_COUNT=$((DOC_MISSING_COUNT + 1))
    fi
  done
  if [ "$DOC_MISSING_COUNT" -gt 0 ]; then
    echo "💡 Found $DOC_MISSING_COUNT files with public APIs missing XML documentation"
  fi
fi

echo ""
if [ $OVERALL_SUCCESS -eq 0 ]; then
  echo "🎉 All linting checks passed!"
else
  echo "❌ Some linting checks failed. Please fix the issues above."
  exit 1
fi

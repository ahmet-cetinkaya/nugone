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
SOLUTION_FILE=$(find . -name "*.sln" -o -name "*.slnx" | head -n 1)
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
  $ROSLYNATOR_CMD analyze "$SOLUTION_FILE" || ROSLYNATOR_FAILED=1
  if [ "${ROSLYNATOR_FAILED:-0}" -eq 0 ]; then
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
if grep -r -i -n "TODO\|FIXME\|HACK" --include="*.cs" src/ >/dev/null 2>&1; then
  echo "⚠️  Warning: Found TODO/FIXME/HACK comments in source code"
  grep -r -i -n "TODO\|FIXME\|HACK" --include="*.cs" src/ || true
fi

# Check for hardcoded secrets using gitleaks
echo "  - Checking for potential secrets with gitleaks..."
if command -v gitleaks &>/dev/null; then
  # Run gitleaks detect in no-git mode (for current directory scanning)
  if gitleaks detect --no-git --verbose --redact >/dev/null 2>&1; then
    echo "✅ No secrets detected"
  else
    echo "⚠️  Warning: gitleaks found potential secrets"
    gitleaks detect --no-git --verbose --redact || true
  fi
else
  echo "⚠️  Warning: gitleaks is not installed, falling back to basic pattern check"
  # Fallback to basic pattern checking
  if grep -r -i -n "password\|secret\|apikey\|connectionstring" --include="*.cs" src/ | grep -v "//.*password\|//.*secret\|//.*apikey\|//.*connectionstring" >/dev/null 2>&1; then
    echo "⚠️  Warning: Found potential hardcoded secrets"
    grep -r -i -n "password\|secret\|apikey\|connectionstring" --include="*.cs" src/ | grep -v "//.*password\|//.*secret\|//.*apikey\|//.*connectionstring" || true
  fi
fi

# Check for proper XML documentation on public APIs
echo "  - Checking for missing XML documentation..."
PUBLIC_API_FILES=$(find src/ -name "*.cs" -exec grep -l "public.*class\|public.*interface\|public.*enum" {} \;)
if [ -n "$PUBLIC_API_FILES" ]; then
  for file in $PUBLIC_API_FILES; do
    if ! grep -q "///" "$file"; then
      echo "ℹ️  Consider adding XML documentation for public APIs in: $file"
    fi
  done
fi

echo ""
if [ $OVERALL_SUCCESS -eq 0 ]; then
  echo "🎉 All linting checks passed!"
else
  echo "❌ Some linting checks failed. Please fix the issues above."
  exit 1
fi

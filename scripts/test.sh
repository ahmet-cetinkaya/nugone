#!/bin/bash

# Test script for NuGone project
# This script runs test compilation checks

set -e

# Source common output functions
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=_common.sh
source "$SCRIPT_DIR/_common.sh"

print_header "🧪 Running NuGone project test checks"

# Check if we're in the right directory
if [ ! -f "NuGone.sln" ] && [ ! -f "NuGone.slnx" ]; then
  print_error "No solution file found. Please run from the project root."
  exit 1
fi

# Get the solution file - prioritize .sln over .slnx (slnx requires newer SDK)
if [ -f "NuGone.sln" ]; then
  SOLUTION_FILE="./NuGone.sln"
elif [ -f "NuGone.slnx" ]; then
  SOLUTION_FILE="./NuGone.slnx"
else
  SOLUTION_FILE=$(find . -maxdepth 1 -name "*.sln" | head -n 1)
  if [ -z "$SOLUTION_FILE" ]; then
    SOLUTION_FILE=$(find . -maxdepth 1 -name "*.slnx" | head -n 1)
  fi
fi
print_info "Found solution file: $SOLUTION_FILE"

# Detect available .NET SDK version and determine target framework
DOTNET_VERSION=$(dotnet --version 2>/dev/null | cut -d'.' -f1)
if [ -z "$DOTNET_VERSION" ]; then
  print_error "Could not detect .NET SDK version"
  exit 1
fi

# Map SDK major version to target framework
case "$DOTNET_VERSION" in
  8)
    TARGET_FRAMEWORK="net8.0"
    ;;
  9)
    TARGET_FRAMEWORK="net9.0"
    ;;
  10)
    TARGET_FRAMEWORK="net10.0"
    ;;
  *)
    print_warning "Unexpected .NET version $DOTNET_VERSION, defaulting to net8.0"
    TARGET_FRAMEWORK="net8.0"
    ;;
esac

print_info "Detected .NET SDK version: $DOTNET_VERSION (targeting $TARGET_FRAMEWORK)"

# Track overall success
OVERALL_SUCCESS=0

print_section "📦 Restoring packages (single framework to avoid multi-target issues)"
# Use /p:TargetFramework to avoid restoring unsupported frameworks
if dotnet restore "$SOLUTION_FILE" /p:TargetFramework="$TARGET_FRAMEWORK" --verbosity minimal; then
  print_success "Restore completed successfully"
else
  print_error "Restore failed"
  OVERALL_SUCCESS=1
fi

print_section "🏗️  Running dotnet build (ensure tests can build)"
if dotnet build "$SOLUTION_FILE" --configuration Release --framework "$TARGET_FRAMEWORK" --no-restore --verbosity normal; then
  print_success "Build completed successfully"
else
  print_error "Build failed"
  OVERALL_SUCCESS=1
fi

print_section "🧪 Running dotnet test (compilation and execution check)"
if dotnet test "$SOLUTION_FILE" --no-build --configuration Release --framework "$TARGET_FRAMEWORK" --verbosity normal --logger "console;verbosity=minimal"; then
  print_success "Test projects compile and run successfully"
else
  print_error "Test projects have compilation or execution issues"
  OVERALL_SUCCESS=1
fi

echo ""
if [ $OVERALL_SUCCESS -eq 0 ]; then
  print_success "🎉 All test checks passed!"
else
  print_error "❌ Some test checks failed."
  exit 1
fi

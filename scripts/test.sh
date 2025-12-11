#!/bin/bash

# Test script for NuGone project
# This script runs comprehensive multi-framework validation across all supported .NET versions (8.0, 9.0, 10.0)

set -e

# Default behavior: always multi-framework test
VERBOSE=false

# Parse command line arguments
for arg in "$@"; do
  case $arg in
    --verbose | -v)
      VERBOSE=true
      shift
      ;;
    --help | -h)
      echo "Usage: $0 [OPTIONS]"
      echo "Options:"
      echo "  --verbose, -v           Enable verbose output"
      echo "  --help, -h             Show this help message"
      echo ""
      echo "This script tests NuGone across all supported .NET frameworks (8.0, 9.0, 10.0)"
      exit 0
      ;;
  esac
done

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

# Multi-framework validation functions
test_framework() {
  local framework=$1
  local configuration=$2

  print_section "🔍 Testing $framework ($configuration)"

  # Build the solution for the specific framework
  if dotnet build "$SOLUTION_FILE" \
    --configuration "$configuration" \
    --framework "$framework" \
    --no-restore \
    --verbosity minimal; then
    if [ "$VERBOSE" = true ]; then
      print_success "Build successful for $framework ($configuration)"
    fi
  else
    print_error "Build failed for $framework ($configuration)"
    return 1
  fi

  # Run tests for the specific framework
  if dotnet test "$SOLUTION_FILE" \
    --configuration "$configuration" \
    --framework "$framework" \
    --no-build \
    --verbosity minimal \
    --logger "console;verbosity=minimal" \
    --results-directory "./TestResults/${framework}-${configuration}"; then
    print_success "Tests passed for $framework ($configuration)"
  else
    print_error "Tests failed for $framework ($configuration)"
    return 1
  fi
}

test_cli_functionality() {
  local framework=$1

  print_section "🚀 Validating CLI functionality for $framework"

  # Build and publish the CLI for the specific framework
  if dotnet publish src/presentation/NuGone.Cli \
    --configuration Release \
    --framework "$framework" \
    --output "./publish/${framework}" \
    --verbosity minimal; then
    print_success "CLI published successfully for $framework"
  else
    print_error "Failed to publish CLI for $framework"
    return 1
  fi

  # Test basic CLI functionality
  if ./publish/"$framework"/nugone --help >/dev/null 2>&1; then
    print_success "CLI help command works for $framework"
  else
    print_error "CLI help command failed for $framework"
    return 1
  fi
}

run_multi_framework_validation() {
  local frameworks=("net8.0" "net9.0" "net10.0")
  local configurations=("Debug" "Release")
  local failed_tests=()
  local total_tests=0

  print_header "🌐 NuGone Multi-Framework Validation"
  echo ""

  # Restore packages once
  print_section "📦 Restoring packages for all frameworks"
  if dotnet restore "$SOLUTION_FILE" --verbosity minimal; then
    print_success "Restore completed successfully"
  else
    print_error "Restore failed"
    exit 1
  fi
  echo ""

  # Test each framework and configuration
  for framework in "${frameworks[@]}"; do
    print_header "Framework: $framework"
    echo ""

    for config in "${configurations[@]}"; do
      total_tests=$((total_tests + 1))
      if ! test_framework "$framework" "$config"; then
        failed_tests+=("$framework ($config)")
      fi
      echo ""
    done

    # Additional validation for Release builds
    test_cli_functionality "$framework"
    echo ""
  done

  # Summary
  print_header "📊 Test Results Summary"
  echo ""

  if [ ${#failed_tests[@]} -gt 0 ]; then
    print_error "❌ Failed Tests (${#failed_tests[@]}):"
    for test in "${failed_tests[@]}"; do
      print_error "  - $test"
    done
    echo ""
    print_error "Validation failed with ${#failed_tests[@]} failures."
    exit 1
  else
    print_success "🎉 All tests passed! Multi-framework support is working correctly."
    echo ""
    print_success "Total tests executed: $total_tests"
    print_success "Frameworks tested: ${#frameworks[@]}"
    print_success "Configurations tested: ${#configurations[@]}"
    echo ""
    print_info "Test results are available in ./TestResults/"
    print_info "Published CLI binaries are available in ./publish/"
  fi
}

# Main execution logic
print_header "🌐 NuGone Multi-Framework Testing"
print_info "Detected .NET SDK version: $DOTNET_VERSION"
print_info "Testing all supported .NET frameworks: 8.0, 9.0, and 10.0"
echo ""

# Always run multi-framework validation
run_multi_framework_validation

#!/bin/bash

# Test script for NuGone project
# This script runs test compilation checks

set -e

# Source common output functions
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/_common.sh"

print_header "🧪 Running NuGone project test checks"

# Check if we're in the right directory
if [ ! -f "NuGone.sln" ] && [ ! -f "NuGone.slnx" ]; then
  print_error "No solution file found. Please run from the project root."
  exit 1
fi

# Get the solution file
SOLUTION_FILE=$(find . -name "*.sln" -o -name "*.slnx" | head -n 1)
print_info "Found solution file: $SOLUTION_FILE"

# Track overall success
OVERALL_SUCCESS=0

print_section "🏗️  Running dotnet build (ensure tests can build)"
if dotnet build "$SOLUTION_FILE" --configuration Release --verbosity normal; then
  print_success "Build completed successfully"
else
  print_error "Build failed"
  OVERALL_SUCCESS=1
fi

print_section "🧪 Running dotnet test (compilation and execution check)"
if dotnet test "$SOLUTION_FILE" --no-build --configuration Release --verbosity normal --logger "console;verbosity=minimal"; then
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

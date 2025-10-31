#!/bin/bash

# Test script for NuGone project
# This script runs test compilation checks

set -e

echo "🧪 Running NuGone project test checks..."

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
echo "🏗️  Running dotnet build (ensure tests can build)..."
if dotnet build "$SOLUTION_FILE" --configuration Release --verbosity normal; then
  echo "✅ Build completed successfully"
else
  echo "❌ Build failed"
  OVERALL_SUCCESS=1
fi

echo ""
echo "🧪 Running dotnet test (compilation and execution check)..."
if dotnet test "$SOLUTION_FILE" --no-build --configuration Release --verbosity normal --logger "console;verbosity=minimal"; then
  echo "✅ Test projects compile and run successfully"
else
  echo "❌ Test projects have compilation or execution issues"
  OVERALL_SUCCESS=1
fi

echo ""
if [ $OVERALL_SUCCESS -eq 0 ]; then
  echo "🎉 All test checks passed!"
else
  echo "❌ Some test checks failed."
  exit 1
fi

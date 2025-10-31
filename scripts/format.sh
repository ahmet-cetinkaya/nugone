#!/bin/bash

# Format script for NuGone project
# This script formats C# code with CSharpier and other files with Prettier

set -e

echo "🔧 Formatting NuGone project..."

# Check if we're in the right directory
if [ ! -f "NuGone.sln" ] && [ ! -f "NuGone.slnx" ]; then
  echo "❌ Error: No solution file found. Please run from the project root."
  exit 1
fi

# Format C# files with CSharpier
echo "📝 Formatting C# files with CSharpier..."
if command -v dotnet csharpier &>/dev/null; then
  dotnet csharpier format .
  echo "✅ C# files formatted successfully!"
else
  echo "⚠️  Warning: dotnet-csharpier is not installed or not in PATH"
  echo "💡 Install it with: dotnet tool install -g csharpier"
fi

# Format other files with Prettier
echo "📄 Formatting markdown and other files with Prettier..."
if command -v prettier &>/dev/null; then
  # Find and format supported files
  prettier --write "**/*.{md,json,yml,yaml}" \
    --ignore-path=.gitignore \
    --ignore-path=.prettierignore 2>/dev/null || true
  echo "✅ Markdown and other files formatted successfully!"
else
  echo "⚠️  Warning: prettier is not installed or not in PATH"
  echo "💡 Install it with: npm install -g prettier"
fi

# Format shell scripts with shfmt
echo "🐚 Formatting shell scripts with shfmt..."
if command -v shfmt &>/dev/null; then
  shfmt -w -i 2 -ci **/*.sh 2>/dev/null || true
  echo "✅ Shell scripts formatted successfully!"
else
  echo "⚠️  Warning: shfmt is not installed or not in PATH"
  echo "💡 Install it with: go install mvdan.cc/sh/v3/cmd/shfmt@latest"
fi

echo "🎉 Formatting complete!"

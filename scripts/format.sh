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
if command -v dotnet-csharpier &> /dev/null; then
    dotnet-csharpier .
    echo "✅ C# files formatted successfully!"
else
    echo "⚠️  Warning: dotnet-csharpier is not installed or not in PATH"
    echo "💡 Install it with: dotnet tool install -g csharpier"
fi

# Format other files with Prettier
echo "📄 Formatting markdown and other files with Prettier..."
if command -v prettier &> /dev/null; then
    # Find and format supported files
    prettier --write "**/*.{md,json,yml,yaml,js,ts,jsx,tsx,css,scss,html}" \
        --ignore-path=.gitignore \
        --ignore-path=.prettierignore 2>/dev/null || true
    echo "✅ Markdown and other files formatted successfully!"
else
    echo "⚠️  Warning: prettier is not installed or not in PATH"
    echo "💡 Install it with: npm install -g prettier"
fi

echo "🎉 Formatting complete!"
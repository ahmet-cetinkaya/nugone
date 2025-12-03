#!/bin/bash

# Format script for NuGone project
# This script formats C# code with CSharpier and other files with Prettier

set -e

# Source common output functions
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/_common.sh"

print_header "🔧 Formatting NuGone project"

# Check if we're in the right directory
if [ ! -f "NuGone.sln" ] && [ ! -f "NuGone.slnx" ]; then
  print_error "No solution file found. Please run from the project root."
  exit 1
fi

# Format C# files with CSharpier
print_section "📝 Formatting C# files with CSharpier"
if command -v dotnet csharpier &>/dev/null; then
  dotnet csharpier format .
  print_success "C# files formatted successfully!"
else
  print_warning "dotnet-csharpier is not installed or not in PATH"
  print_info "Install it with: dotnet tool install -g csharpier"
fi

# Format other files with Prettier
print_section "📄 Formatting markdown and other files with Prettier"
if command -v prettier &>/dev/null; then
  # Find and format supported files
  prettier --write "**/*.{md,json,yml,yaml}" \
    --ignore-path=.gitignore \
    --ignore-path=.prettierignore 2>/dev/null || true
  print_success "Markdown and other files formatted successfully!"
else
  print_warning "prettier is not installed or not in PATH"
  print_info "Install it with: npm install -g prettier"
fi

# Format shell scripts with shfmt
print_section "🐚 Formatting shell scripts with shfmt"
if command -v shfmt &>/dev/null; then
  shfmt -w -i 2 -ci **/*.sh 2>/dev/null || true
  print_success "Shell scripts formatted successfully!"
else
  print_warning "shfmt is not installed or not in PATH"
  print_info "Install it with: go install mvdan.cc/sh/v3/cmd/shfmt@latest"
fi

print_success "🎉 Formatting complete!"

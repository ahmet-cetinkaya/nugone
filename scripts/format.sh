#!/bin/bash

# Format script for NuGone project
# This script formats C# code with CSharpier and other files with Prettier

set -e

# Source common output functions
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/_common.sh"

print_header "🔧 Formatting NuGone project"

# Detect correct dotnet command (prefer dnvm over system)
DNVM_DOTNET="$HOME/.local/share/dnvm/dn/dotnet"
if [ -x "$DNVM_DOTNET" ]; then
  DOTNET_CMD="$DNVM_DOTNET"
elif command -v dotnet &>/dev/null; then
  DOTNET_CMD="dotnet"
else
  print_error ".NET SDK is not installed"
  exit 1
fi

# Check if we're in the right directory
if [ ! -f "NuGone.sln" ] && [ ! -f "NuGone.slnx" ]; then
  print_error "No solution file found. Please run from the project root."
  exit 1
fi

# Format C# files with CSharpier
print_section "📝 Formatting C# files with CSharpier"
if $DOTNET_CMD csharpier --version &>/dev/null 2>&1; then
  $DOTNET_CMD csharpier format .
  print_success "C# files formatted successfully!"
else
  print_warning "CSharpier is not installed"
  print_info "Install it with: dotnet tool install -g csharpier"
fi

# Auto-fix static analyzer issues
print_section "🔍 Auto-fixing static analyzer violations"
if [ -f "NuGone.sln" ]; then
  SOLUTION_FILE="./NuGone.sln"
  print_info "Running dotnet format analyzers on solution..."
  if $DOTNET_CMD format analyzers "$SOLUTION_FILE" --verbosity diagnostic 2>/dev/null; then
    print_success "Static analyzer violations fixed successfully!"
  else
    print_warning "Some analyzer violations could not be auto-fixed"
    print_info "Run './scripts/lint.sh' to see remaining issues"
  fi
elif [ -f "NuGone.slnx" ]; then
  print_warning "Skipping analyzer auto-fix - .slnx format is not fully supported"
  print_info "Consider generating a .sln file for full analyzer support"
else
  print_warning "No solution file found, skipping analyzer auto-fix"
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

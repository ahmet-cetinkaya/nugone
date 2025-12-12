#!/bin/bash

# Version bump script for NuGone
# This script updates version numbers across the project

set -e

# Source common functions
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/_common.sh"

# Function to update CHANGELOG.md with new version and git logs
update_changelog() {
  local changelog_file="$1"
  local old_version="$2"
  local new_version="$3"

  # Get current date
  local current_date=$(date +%Y-%m-%d)

  # Get the latest tag
  local latest_tag=$(git tag --sort=-version:refname | head -1)

  # Get git log since latest tag
  local git_log=""
  if [[ -n "$latest_tag" ]]; then
    git_log=$(git log "$latest_tag"..HEAD --oneline --pretty=format:"%h - %s (%an)" 2>/dev/null || echo "")
  else
    git_log=$(git log --oneline --pretty=format:"%h - %s (%an)" -10 2>/dev/null || echo "")
  fi

  # Create new changelog entry
  local new_entry="## [$new_version] - $current_date

### Added

- (New features will be added here based on git commits)

### Changed

- (Changes will be added here based on git commits)

### Fixed

- (Fixes will be added here based on git commits)

"

  # Find the line with "[Unreleased]" and insert before it
  if grep -q "\[Unreleased\]" "$changelog_file"; then
    # Insert new entry before [Unreleased]
    sed -i.tmp "/## \[Unreleased\]/i\\
$new_entry" "$changelog_file" && rm -f "$changelog_file.tmp"

    # Update version comparison links at the bottom
    if [[ -n "$latest_tag" ]]; then
      local new_link="[${new_version}]: https://github.com/ahmet-cetinkaya/nugone/compare/${latest_tag}...v${new_version}"
      # Find the last version link and insert before it
      sed -i.tmp "/\[.*\]: https:\/\/github.com/ahmet-cetinkaya\/nugone\/compare\/.*/i\\
$new_link" "$changelog_file" && rm -f "$changelog_file.tmp"
    fi

    print_info "CHANGELOG.md updated with new version section"
    print_info "Please manually update the changelog entries based on git commits:"
    if [[ -n "$git_log" ]]; then
      echo "$git_log" | head -10
    fi
    return 0
  else
    print_error "Could not find [Unreleased] section in CHANGELOG.md"
    return 1
  fi
}

# Default values
VERSION_TYPE="patch"
CURRENT_VERSION=""
DRY_RUN=false
SKIP_CONFIRM=false
SKIP_GIT=false

# Usage information
usage() {
  echo "Usage: $0 [options] [version]"
  echo "Options:"
  echo "  -t, --type TYPE     Version bump type: major, minor, or patch (default: patch)"
  echo "  -c, --current VER   Current version (auto-detected if not provided)"
  echo "  -d, --dry-run       Show what would be changed without making changes"
  echo "  -y, --yes           Skip confirmation prompt"
  echo "  -g, --skip-git      Skip git commit and tag creation"
  echo "  -h, --help          Show this help message"
  echo
  echo "Examples:"
  echo "  $0                  # Bump patch version (e.g., 3.0.0 -> 3.0.1)"
  echo "  $0 -t minor         # Bump minor version (e.g., 3.0.0 -> 3.1.0)"
  echo "  $0 -t major         # Bump major version (e.g., 3.0.0 -> 4.0.0)"
  echo "  $0 3.1.0            # Set specific version"
  echo "  $0 -t patch -d      # Dry run for patch bump"
  echo "  $0 -g               # Bump version and skip git operations"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    -t | --type)
      VERSION_TYPE="$2"
      shift 2
      ;;
    -c | --current)
      CURRENT_VERSION="$2"
      shift 2
      ;;
    -d | --dry-run)
      DRY_RUN=true
      shift
      ;;
    -y | --yes)
      SKIP_CONFIRM=true
      shift
      ;;
    -g | --skip-git)
      SKIP_GIT=true
      shift
      ;;
    -h | --help)
      usage
      exit 0
      ;;
    -*)
      print_error "Unknown option: $1"
      usage
      exit 1
      ;;
    *)
      # If it's a version format, use it as the new version
      if [[ $1 =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
        NEW_VERSION="$1"
        VERSION_TYPE="custom"
      else
        print_error "Invalid version format: $1"
        usage
        exit 1
      fi
      shift
      ;;
  esac
done

# Validate version type
if [[ ! "$VERSION_TYPE" =~ ^(major|minor|patch|custom)$ ]]; then
  print_error "Invalid version type: $VERSION_TYPE. Must be: major, minor, or patch"
  exit 1
fi

# Auto-detect current version if not provided
if [[ -z "$CURRENT_VERSION" ]]; then
  CLI_PROJECT_FILE="src/presentation/NuGone.Cli/NuGone.Cli.csproj"
  if [[ -f "$CLI_PROJECT_FILE" ]]; then
    CURRENT_VERSION=$(grep -oP '<Version>\K[^<]+' "$CLI_PROJECT_FILE" || echo "")
  fi

  if [[ -z "$CURRENT_VERSION" ]]; then
    print_error "Could not auto-detect current version. Please specify with --current"
    exit 1
  fi
fi

# Calculate new version if not custom
if [[ "$VERSION_TYPE" != "custom" ]]; then
  IFS='.' read -ra VERSION_PARTS <<<"$CURRENT_VERSION"
  MAJOR="${VERSION_PARTS[0]}"
  MINOR="${VERSION_PARTS[1]}"
  PATCH="${VERSION_PARTS[2]}"

  case $VERSION_TYPE in
    major)
      MAJOR=$((MAJOR + 1))
      MINOR=0
      PATCH=0
      ;;
    minor)
      MINOR=$((MINOR + 1))
      PATCH=0
      ;;
    patch)
      PATCH=$((PATCH + 1))
      ;;
  esac

  NEW_VERSION="$MAJOR.$MINOR.$PATCH"
fi

# Validate new version format
if [[ ! $NEW_VERSION =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  print_error "Invalid new version format: $NEW_VERSION"
  exit 1
fi

print_header "Version Bump: $CURRENT_VERSION → $NEW_VERSION"

# Files to update
declare -a FILES_TO_UPDATE=(
  "src/presentation/NuGone.Cli/NuGone.Cli.csproj"
)

# Documentation files to update (optional, handled separately)
declare -a DOCS_TO_UPDATE=(
  "docs/DEPLOYMENT.md"
  "CHANGELOG.md"
)

# Check if files exist
for file in "${FILES_TO_UPDATE[@]}"; do
  if [[ ! -f "$file" ]]; then
    print_error "File not found: $file"
    exit 1
  fi
done

# Show changes to be made
print_section "Changes to be made:"

echo "Core files:"
for file in "${FILES_TO_UPDATE[@]}"; do
  if [[ -f "$file" ]]; then
    echo "  $file"
    echo "    Current: $CURRENT_VERSION"
    echo "    New:     $NEW_VERSION"
  fi
done

echo
echo "Documentation files (optional):"
for file in "${DOCS_TO_UPDATE[@]}"; do
  # Always update CHANGELOG.md, for other files check if they contain the version
  if [[ "$file" == "CHANGELOG.md" ]] || ([[ -f "$file" ]] && grep -q "$CURRENT_VERSION" "$file"); then
    if [[ "$file" == "CHANGELOG.md" ]]; then
      echo "  $file (will be updated with new version section)"
    else
      echo "  $file (contains version references)"
    fi
  fi
done

# Dry run mode
if [[ "$DRY_RUN" == "true" ]]; then
  print_info "DRY RUN: No changes will be made"
  exit 0
fi

# Confirmation prompt
if [[ "$SKIP_CONFIRM" == "false" ]]; then
  echo -e "${YELLOW}This will update version from $CURRENT_VERSION to $NEW_VERSION${NC}"
  if [[ -n "${DOCS_TO_UPDATE[*]}" ]] && grep -q "$CURRENT_VERSION" "${DOCS_TO_UPDATE[@]}" 2>/dev/null; then
    echo -e "${YELLOW}Note: Documentation files also contain version references${NC}"
  fi
  read -p "Do you want to continue? (y/N) " -n 1 -r
  echo
  if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    print_info "Operation cancelled by user"
    exit 0
  fi
fi

# Perform version updates
print_section "Updating versions..."

UPDATED_FILES=0
UPDATED_DOCS=0

# Update core files
echo "Updating core project files..."
for file in "${FILES_TO_UPDATE[@]}"; do
  print_info "Updating $file..."

  # Create backup
  backup_file="${file}.bak"
  cp "$file" "$backup_file"

  # Update version using sed
  if sed -i.tmp "s/<Version>$CURRENT_VERSION<\/Version>/<Version>$NEW_VERSION<\/Version>/" "$file"; then
    rm -f "$file.tmp"
    rm "$backup_file"
    print_success "Updated $file"
    ((UPDATED_FILES++))
  else
    print_error "Failed to update $file"
    # Restore backup
    mv "$backup_file" "$file"
    rm -f "$file.tmp"
    exit 1
  fi
done

# Update documentation files (optional)
echo
echo "Checking documentation files..."
for file in "${DOCS_TO_UPDATE[@]}"; do
  # Always update CHANGELOG.md, for other files check if they contain the version
  if [[ "$file" == "CHANGELOG.md" ]] || ([[ -f "$file" ]] && grep -q "$CURRENT_VERSION" "$file"); then
    print_info "Updating version references in $file..."

    # Create backup
    backup_file="${file}.bak"
    cp "$file" "$backup_file"

    if [[ "$file" == "CHANGELOG.md" ]]; then
      # Special handling for CHANGELOG.md
      update_changelog "$file" "$CURRENT_VERSION" "$NEW_VERSION"
      if [[ $? -eq 0 ]]; then
        rm "$backup_file"
        print_success "Updated $file"
        ((UPDATED_DOCS++))
      else
        print_error "Failed to update $file"
        # Restore backup
        mv "$backup_file" "$file"
        exit 1
      fi
    else
      # Regular documentation files
      # Update version references in documentation
      if sed -i.tmp "s/$CURRENT_VERSION/$NEW_VERSION/g" "$file"; then
        rm -f "$file.tmp"
        rm "$backup_file"
        print_success "Updated $file"
        ((UPDATED_DOCS++))
      else
        print_error "Failed to update $file"
        # Restore backup
        mv "$backup_file" "$file"
        rm -f "$file.tmp"
        exit 1
      fi
    fi
  fi
done

print_section "Version Bump Complete"
print_success "Successfully updated $UPDATED_FILES core file(s)"
if [[ $UPDATED_DOCS -gt 0 ]]; then
  print_success "Successfully updated $UPDATED_DOCS documentation file(s)"
fi
print_info "Version bumped from $CURRENT_VERSION to $NEW_VERSION"

# Show next steps
print_section "Next Steps"
echo "After version bump, consider:"
echo "1. Run tests: ./scripts/test.sh"
echo "2. Build project: dotnet build NuGone.slnx"
echo "3. Create commit: git add -A && git commit -m \"build: bump version to $NEW_VERSION\""
echo "4. Create tag: git tag v$NEW_VERSION"
echo "5. Build package: dotnet pack src/presentation/NuGone.Cli/NuGone.Cli.csproj -c Release"

# Offer to create git commit and tag automatically
if [[ "$SKIP_GIT" == "false" ]]; then
  echo
  print_section "Automatic Git Operations"
  echo -e "${YELLOW}Would you like to create git commit and tag automatically?${NC}"
  echo "This will:"
  echo "  - Stage all changes (git add -A)"
  echo "  - Create commit with version bump message"
  echo "  - Create tag v$NEW_VERSION"
  echo
  read -p "Create commit and tag? (y/N) " -n 1 -r
  echo
else
  print_section "Git Operations Skipped"
  print_info "Git commit and tag creation skipped (--skip-git flag)"
  echo
fi

if [[ "$SKIP_GIT" == "false" ]]; then
  if [[ $REPLY =~ ^[Yy]$ ]]; then
    print_section "Creating Git Commit and Tag"

    # Stage all changes
    print_info "Staging changes..."
    if git add -A; then
      print_success "Changes staged"
    else
      print_error "Failed to stage changes"
      exit 1
    fi

    # Create commit
    print_info "Creating commit..."
    commit_message="build: bump version to $NEW_VERSION

- Update project version from $CURRENT_VERSION to $NEW_VERSION
- Update CHANGELOG.md with new version section
- Update documentation version references"

    if git commit -m "$commit_message"; then
      print_success "Commit created"
    else
      print_error "Failed to create commit"
      exit 1
    fi

    # Create tag
    print_info "Creating tag..."
    if git tag "v$NEW_VERSION"; then
      print_success "Tag v$NEW_VERSION created"
    else
      print_error "Failed to create tag"
      exit 1
    fi

    echo
    print_section "Git Operations Complete"
    print_success "Successfully created commit and tag v$NEW_VERSION"
    echo
    echo "To push to remote repository:"
    echo "  git push origin main"
    echo "  git push origin v$NEW_VERSION"
  else
    print_info "Skipping git operations (manual commit and tag required)"
  fi
fi

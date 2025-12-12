#!/bin/bash

# Version bump script for NuGone
# This script updates version numbers across the project

set -e

# Source common functions
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/_common.sh"

# Function to parse conventional commits and generate changelog content
parse_conventional_commits() {
  local latest_tag="$1"
  local old_version="$2"
  local new_version="$3"

  # Get commits since latest tag
  local commits=""
  if [[ -n "$latest_tag" ]]; then
    commits=$(git log "$latest_tag"..HEAD --pretty=format:"%h|%s|%b" --no-merges)
  else
    commits=$(git log --pretty=format:"%h|%s|%b" --no-merges -20)
  fi

  # Initialize categorized sections
  local added=()
  local changed=()
  local deprecated=()
  local removed=()
  local fixed=()
  local security=()
  local ci_cd=()
  local breaking_changes=()

  # Parse each commit
  while IFS='|' read -r hash subject body; do
    # Skip if hash is empty
    [[ -z "$hash" ]] && continue

    # Parse conventional commit type and scope
    # Using case statement for more reliable parsing
    local type=""
    local scope=""
    local message=""
    local is_breaking=false

    # Check for breaking change indicator
    if [[ "$subject" =~ ! ]]; then
      is_breaking=true
    fi

    # Check body for BREAKING CHANGE (using simple string search)
    if [[ "$body" == *"BREAKING CHANGE:"* ]]; then
      breaking_desc="${body#*BREAKING CHANGE:}"
      breaking_changes+=("• ${message}${scope:+ ($scope)} - ${breaking_desc} ($hash)")
      is_breaking=true
    fi

    # Extract type, scope, and message using parameter expansion
    # Check if there's a colon
    if [[ "$subject" == *":"* ]]; then
      # Split on colon
      type_part="${subject%%:*}"
      message_part="${subject#*: }"

      # Check if type_part has parentheses (scope)
      if [[ "$type_part" == *"("* ]]; then
        type="${type_part%%(*}"
        scope="${type_part#*(}"
        scope="${scope%)}"
      else
        type="$type_part"
        scope=""
      fi

      message="$message_part"
    else
      # Skip commits that don't match conventional format
      continue
    fi

    # Categorize commit
    case "$type" in
      feat)
        added+=("• ${message}${scope:+ ($scope)} ($hash)")
        [[ "$is_breaking" == true ]] && breaking_changes+=("• ${message}${scope:+ ($scope)} - BREAKING CHANGE ($hash)")
        ;;
      fix)
        fixed+=("• ${message}${scope:+ ($scope)} ($hash)")
        ;;
      chore | build)
        changed+=("• ${message}${scope:+ ($scope)} ($hash)")
        ;;
      ci)
        ci_cd+=("• ${message}${scope:+ ($scope)} ($hash)")
        ;;
      refactor)
        changed+=("• Refactor: ${message}${scope:+ ($scope)} ($hash)")
        ;;
      perf)
        changed+=("• Performance: ${message}${scope:+ ($scope)} ($hash)")
        ;;
      docs)
        # Documentation changes don't need to be in changelog unless they affect users
        ;;
      style)
        # Style changes don't need to be in changelog
        ;;
      test)
        # Test changes don't need to be in changelog
        ;;
    esac
  done <<<"$commits"

  # Generate formatted sections
  local changelog_content=""

  # Add section if not empty
  if [[ ${#added[@]} -gt 0 ]]; then
    changelog_content+=$'\n### Added\n\n'
    changelog_content+=$(printf '%s\n' "${added[@]}")
  fi

  if [[ ${#changed[@]} -gt 0 ]]; then
    changelog_content+=$'\n\n### Changed\n\n'
    changelog_content+=$(printf '%s\n' "${changed[@]}")
  fi

  if [[ ${#deprecated[@]} -gt 0 || ${#removed[@]} -gt 0 ]]; then
    changelog_content+=$'\n\n### Removed\n\n'
    changelog_content+=$(printf '%s\n' "${deprecated[@]}" "${removed[@]}")
  fi

  if [[ ${#fixed[@]} -gt 0 ]]; then
    changelog_content+=$'\n\n### Fixed\n\n'
    changelog_content+=$(printf '%s\n' "${fixed[@]}")
  fi

  if [[ ${#security[@]} -gt 0 ]]; then
    changelog_content+=$'\n\n### Security\n\n'
    changelog_content+=$(printf '%s\n' "${security[@]}")
  fi

  if [[ ${#ci_cd[@]} -gt 0 ]]; then
    changelog_content+=$'\n\n### CI/CD\n\n'
    changelog_content+=$(printf '%s\n' "${ci_cd[@]}")
  fi

  if [[ ${#breaking_changes[@]} -gt 0 ]]; then
    changelog_content+=$'\n\n### BREAKING CHANGE\n\n'
    changelog_content+=$(printf '%s\n' "${breaking_changes[@]}")
  fi

  echo "$changelog_content"
}

# Function to update CHANGELOG.md with new version and git logs
update_changelog() {
  local changelog_file="$1"
  local old_version="$2"
  local new_version="$3"

  # Get current date
  local current_date=$(date +%Y-%m-%d)

  # Get the latest tag
  local latest_tag=$(git tag --sort=-version:refname | head -1)

  # Parse commits and generate changelog content
  local changelog_content=$(parse_conventional_commits "$latest_tag" "$old_version" "$new_version")

  # Create new changelog entry
  local new_entry="## [$new_version] - $current_date"

  # Add sections with content
  if [[ -n "$changelog_content" ]]; then
    new_entry+="$changelog_content"
  else
    # Fallback for no commits
    new_entry+=$'\n\n'
    new_entry+=$'### Changed\n\n'
    new_entry+=$'- Version bump'
  fi

  new_entry+=$'\n'

  # Find the line with "[Unreleased]" and insert before it
  if grep -q "\[Unreleased\]" "$changelog_file"; then
    # Insert new entry before [Unreleased]
    sed -i.tmp "/## \[Unreleased\]/i\\\\
$new_entry" "$changelog_file" && rm -f "$changelog_file.tmp"

    # Update version comparison links at the bottom
    if [[ -n "$latest_tag" ]]; then
      local new_link="[${new_version}]: https://github.com/ahmet-cetinkaya/nugone/compare/${latest_tag}...v${new_version}"
      # Find the last version link and insert before it
      sed -i.tmp "/\[.*\]: https:\/\/github.com\/ahmet-cetinkaya\/nugone\/compare\/.*/i\\\\
$new_link" "$changelog_file" && rm -f "$changelog_file.tmp"
    fi

    print_success "CHANGELOG.md automatically updated with commit-based entries"
    return 0
  else
    print_error "Could not find [Unreleased] section in CHANGELOG.md"
    return 1
  fi
}

# Function to compare versions
version_greater_than() {
  local version1="$1"
  local version2="$2"

  # Split versions into arrays
  IFS='.' read -ra v1_parts <<<"$version1"
  IFS='.' read -ra v2_parts <<<"$version2"

  # Compare major, minor, patch
  for i in {0..2}; do
    local v1=${v1_parts[$i]:-0}
    local v2=${v2_parts[$i]:-0}

    if [[ $v1 -gt $v2 ]]; then
      return 0
    elif [[ $v1 -lt $v2 ]]; then
      return 1
    fi
  done

  return 1 # Versions are equal
}

# Function to validate version updates
validate_version_updates() {
  local current_version="$1"
  local new_version="$2"

  # Validate new version format
  if [[ ! $new_version =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    print_error "Invalid new version format: $new_version"
    return 1
  fi

  # Check if new version is greater than current
  if ! version_greater_than "$new_version" "$current_version"; then
    print_warning "New version ($new_version) is not greater than current version ($current_version)"
    read -p "Continue anyway? (y/N) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
      print_info "Operation cancelled by user"
      exit 0
    fi
  fi

  print_success "Validation passed"
  return 0
}

# Function to create rollback checkpoint
create_rollback_checkpoint() {
  local backup_dir=".version_bump_backup_$(date +%Y%m%d_%H%M%S)"

  print_info "Creating rollback checkpoint: $backup_dir"

  mkdir -p "$backup_dir"

  # Backup files that will be modified
  local files_to_backup=(
    "src/presentation/NuGone.Cli/NuGone.Cli.csproj"
    "CHANGELOG.md"
    "docs/DEPLOYMENT.md"
    "docs/VERSION_COMPATIBILITY.md"
  )

  for file in "${files_to_backup[@]}"; do
    if [[ -f "$file" ]]; then
      cp "$file" "$backup_dir/"
    fi
  done

  # Create rollback script
  cat >"$backup_dir/rollback.sh" <<'EOF'
#!/bin/bash
echo "Rolling back version bump..."
for file in *.csproj *.md; do
  if [[ -f "$file" ]]; then
    cp "$file" "../$file"
    echo "Restored $file"
  fi
done
echo "Rollback complete. You may need to reset git changes:"
echo "  git reset --hard HEAD"
EOF
  chmod +x "$backup_dir/rollback.sh"

  print_success "Rollback checkpoint created. Use $backup_dir/rollback.sh to undo changes"
}

# Function to update documentation versions smartly
update_documentation_versions() {
  local current_version="$1"
  local new_version="$2"

  local updated_docs=0

  # Update DEPLOYMENT.md (contains actual version references)
  if [[ -f "docs/DEPLOYMENT.md" ]]; then
    print_info "Updating docs/DEPLOYMENT.md..."

    # Create backup
    backup_file="docs/DEPLOYMENT.md.bak"
    cp "docs/DEPLOYMENT.md" "$backup_file"

    # Update version references in deployment instructions
    if sed -i.tmp -E "s/<Version>[0-9]+\.[0-9]+\.[0-9]+<\/Version>/<Version>$new_version<\/Version>/g" "docs/DEPLOYMENT.md" &&
      sed -i.tmp -E "s/<AssemblyVersion>[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+<\/AssemblyVersion>/<AssemblyVersion>$new_version.0<\/AssemblyVersion>/g" "docs/DEPLOYMENT.md" &&
      sed -i.tmp -E "s/<FileVersion>[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+<\/FileVersion>/<FileVersion>$new_version.0<\/FileVersion>/g" "docs/DEPLOYMENT.md" &&
      sed -i.tmp -E "s/<PackageVersion>[0-9]+\.[0-9]+\.[0-9]+<\/PackageVersion>/<PackageVersion>$new_version<\/PackageVersion>/g" "docs/DEPLOYMENT.md" &&
      sed -i.tmp -E "s/git tag v[0-9]+\.[0-9]+\.[0-9]+/git tag v$new_version/g" "docs/DEPLOYMENT.md" &&
      sed -i.tmp -E "s/git push origin v[0-9]+\.[0-9]+\.[0-9]+/git push origin v$new_version/g" "docs/DEPLOYMENT.md" &&
      sed -i.tmp -E "s/dotnet nuget push \.\/nupkg\/NuGone\.[0-9]+\.[0-9]+\.[0-9]+\.nupkg/dotnet nuget push .\/nupkg\/NuGone.$new_version.nupkg/g" "docs/DEPLOYMENT.md" &&
      sed -i.tmp -E "s/dotnet nuget verify \.\/nupkg\/NuGone\.[0-9]+\.[0-9]+\.[0-9]+\.nupkg/dotnet nuget verify .\/nupkg\/NuGone.$new_version.nupkg/g" "docs/DEPLOYMENT.md" &&
      sed -i.tmp -E "s/dotnet tool install --local --add-source \.\/nupkg nugone --version [0-9]+\.[0-9]+\.[0-9]+/dotnet tool install --local --add-source .\/nupkg nugone --version $new_version/g" "docs/DEPLOYMENT.md"; then
      rm -f "docs/DEPLOYMENT.md.tmp"
      rm "$backup_file"
      print_success "Updated docs/DEPLOYMENT.md"
      ((updated_docs++))
    else
      print_error "Failed to update docs/DEPLOYMENT.md"
      # Restore backup
      mv "$backup_file" "docs/DEPLOYMENT.md"
      rm -f "docs/DEPLOYMENT.md.tmp"
    fi
  fi

  # Skip EXAMPLES.md (contains example versions that should not be updated)
  if [[ -f "docs/EXAMPLES.md" ]]; then
    print_info "Skipping docs/EXAMPLES.md (contains example versions)"
  fi

  # Update VERSION_COMPATIBILITY.md (only update NuGone versions, not .NET versions)
  if [[ -f "docs/VERSION_COMPATIBILITY.md" ]]; then
    print_info "Updating docs/VERSION_COMPATIBILITY.md..."

    # Create backup
    backup_file="docs/VERSION_COMPATIBILITY.md.bak"
    cp "docs/VERSION_COMPATIBILITY.md" "$backup_file"

    # Only update NuGone version patterns (e.g., v2.0.1, v2.2.0)
    if sed -i.tmp -E "s/v[0-9]+\.[0-9]+\.[0-9]+\)/v$new_version)/g" "docs/VERSION_COMPATIBILITY.md" &&
      sed -i.tmp -E "s/Planned v[0-9]+\.[0-9]+\.[0-9]+)/Planned v$new_version)/g" "docs/VERSION_COMPATIBILITY.md"; then
      rm -f "docs/VERSION_COMPATIBILITY.md.tmp"
      rm "$backup_file"
      print_success "Updated docs/VERSION_COMPATIBILITY.md"
      ((updated_docs++))
    else
      print_error "Failed to update docs/VERSION_COMPATIBILITY.md"
      # Restore backup
      mv "$backup_file" "docs/VERSION_COMPATIBILITY.md"
      rm -f "docs/VERSION_COMPATIBILITY.md.tmp"
    fi
  fi

  print_success "Updated $updated_docs documentation file(s)"
  echo "$updated_docs"
}

# Function to generate smart commit message
generate_commit_message() {
  local old_version="$1"
  local new_version="$2"
  local version_type="$3"
  local updated_docs="$4"

  local base_message="build: bump version to $new_version"
  local body=""

  # Add version bump details
  body+=$'\n\n'
  body+="- Update project version from $old_version to $new_version ($version_type)"

  # Add documentation updates if any
  if [[ $updated_docs -gt 0 ]]; then
    body+=$'\n'
    body+="- Update documentation version references in $updated_docs file(s)"
  fi

  # Add changelog mention
  if [[ -f "CHANGELOG.md" ]]; then
    body+=$'\n'
    body+="- Update CHANGELOG.md with new version section"
  fi

  # Get commit count since last tag for additional context
  local latest_tag=$(git tag --sort=-version:refname | head -1)
  local commit_count=0

  if [[ -n "$latest_tag" ]]; then
    commit_count=$(git rev-list --count "$latest_tag"..HEAD 2>/dev/null || echo "0")
  fi

  if [[ $commit_count -gt 0 ]]; then
    body+=$'\n'
    body+="- Includes $commit_count commit(s) since $latest_tag"
  fi

  echo "${base_message}${body}"
}

# Function to perform enhanced git operations
perform_git_operations() {
  local old_version="$1"
  local new_version="$2"
  local version_type="$3"
  local updated_docs="$4"
  local skip_git="$5"

  if [[ "$skip_git" == "true" ]]; then
    print_info "Git operations skipped (--skip-git flag)"
    return 0
  fi

  print_section "Creating Git Commit and Tag"

  # Check if there are changes to commit
  if ! git diff --quiet || ! git diff --cached --quiet; then
    # Stage all changes
    print_info "Staging changes..."
    if ! git add -A; then
      print_error "Failed to stage changes"
      return 1
    fi

    # Generate smart commit message
    local commit_message=$(generate_commit_message "$old_version" "$new_version" "$version_type" "$updated_docs")

    # Create commit
    print_info "Creating commit..."
    if ! git commit -m "$commit_message"; then
      print_error "Failed to create commit"
      return 1
    fi

    print_success "Commit created with intelligent message"
  else
    print_warning "No changes to commit"
    return 0
  fi

  # Create tag
  print_info "Creating tag v$new_version..."
  if ! git tag -a "v$new_version" -m "Release v$new_version

Version $new_version
$(generate_commit_message "$old_version" "$new_version" "$version_type" "$updated_docs")"; then
    print_error "Failed to create tag"
    return 1
  fi

  print_success "Tag v$new_version created with release notes"

  # Offer to push to remote
  echo
  print_section "Push to Remote Repository"
  echo -e "${YELLOW}Would you like to push the commit and tag to the remote repository?${NC}"
  echo "This will:"
  echo "  - Push commit to origin main"
  echo "  - Push tag v$new_version (triggers GitHub Actions release)"
  echo
  read -p "Push to remote? (y/N) " -n 1 -r
  echo

  if [[ $REPLY =~ ^[Yy]$ ]]; then
    print_info "Pushing to remote repository..."

    # Push commit
    if git push origin main; then
      print_success "Commit pushed to origin main"
    else
      print_error "Failed to push commit"
      return 1
    fi

    # Push tag
    if git push origin "v$new_version"; then
      print_success "Tag v$new_version pushed (release workflow triggered)"
    else
      print_error "Failed to push tag"
      return 1
    fi
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

# Validate before making changes
validate_version_updates "$CURRENT_VERSION" "$NEW_VERSION"

# Create rollback checkpoint
create_rollback_checkpoint

# Files to update
declare -a FILES_TO_UPDATE=(
  "src/presentation/NuGone.Cli/NuGone.Cli.csproj"
)

# Documentation files to update (optional, handled separately)
declare -a DOCS_TO_UPDATE=(
  "docs/DEPLOYMENT.md"
  "CHANGELOG.md"
  "docs/VERSION_COMPATIBILITY.md"
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

  # Show changelog preview
  latest_tag=$(git tag --sort=-version:refname | head -1)
  preview=$(parse_conventional_commits "$latest_tag" "$CURRENT_VERSION" "$NEW_VERSION")
  if [[ -n "$preview" ]]; then
    echo
    print_section "Changelog Preview:"
    echo "$preview"
  fi
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

# Update CHANGELOG.md
echo
echo "Updating CHANGELOG.md..."
if [[ -f "CHANGELOG.md" ]]; then
  print_info "Updating CHANGELOG.md with new version section..."

  # Create backup
  backup_file="CHANGELOG.md.bak"
  cp "CHANGELOG.md" "$backup_file"

  if update_changelog "CHANGELOG.md" "$CURRENT_VERSION" "$NEW_VERSION"; then
    rm "$backup_file"
    print_success "Updated CHANGELOG.md"
    ((UPDATED_DOCS++))
  else
    print_error "Failed to update CHANGELOG.md"
    # Restore backup
    mv "$backup_file" "CHANGELOG.md"
    exit 1
  fi
fi

# Update other documentation files with smart version detection
echo
print_section "Updating Documentation..."
UPDATED_DOCS_COUNT=$(update_documentation_versions "$CURRENT_VERSION" "$NEW_VERSION")
((UPDATED_DOCS += UPDATED_DOCS_COUNT))

print_section "Version Bump Complete"
print_success "Successfully updated $UPDATED_FILES core file(s)"
if [[ $UPDATED_DOCS -gt 0 ]]; then
  print_success "Successfully updated $UPDATED_DOCS documentation file(s)"
fi
print_info "Version bumped from $CURRENT_VERSION to $NEW_VERSION"

# Enhanced git operations
perform_git_operations "$CURRENT_VERSION" "$NEW_VERSION" "$VERSION_TYPE" "$UPDATED_DOCS" "$SKIP_GIT"

# Show comprehensive summary
print_section "Next Steps"
echo "✅ Version bump complete!"
echo
echo "Automated tasks completed:"
if [[ "$SKIP_GIT" == "false" ]]; then
  echo "  ✓ Git commit created with conventional message"
  echo "  ✓ Git tag v$NEW_VERSION created"
  if [[ $REPLY =~ ^[Yy]$ ]] && [[ -n "${REPLY}" ]]; then
    echo "  ✓ Changes pushed to remote repository"
    echo "  ✓ GitHub Actions release workflow triggered"
  fi
else
  echo "  ⚠ Git operations skipped (--skip-git flag)"
  echo "  - Run: git add -A && git commit -m \"build: bump version to $NEW_VERSION\""
  echo "  - Run: git tag v$NEW_VERSION"
  echo "  - Run: git push origin main && git push origin v$NEW_VERSION"
fi

echo
echo "Manual tasks (if needed):"
echo "  1. Review CHANGELOG.md for accuracy"
echo "  2. Run tests: ./scripts/test.sh"
echo "  3. Build project: dotnet build NuGone.slnx -c Release"
echo "  4. If not pushed: git push origin main && git push origin v$NEW_VERSION"
echo
echo "💡 Release will be automatically created by GitHub Actions when tag is pushed"

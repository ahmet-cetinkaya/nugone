#!/bin/bash

# Helper function to get the correct dotnet command
# Prefers dnvm-managed dotnet over system dotnet
get_dotnet_command() {
  local DNVM_DOTNET="/home/ac/.local/share/dnvm/dn/dotnet"

  if [ -x "$DNVM_DOTNET" ]; then
    echo "$DNVM_DOTNET"
  elif command -v dotnet &>/dev/null; then
    echo "dotnet"
  else
    echo ""
  fi
}

# Export for use in other scripts
export -f get_dotnet_command

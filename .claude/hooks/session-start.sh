#!/bin/bash
# SessionStart hook for Claude Code on the web.
# Installs the .NET SDK, PowerShell, and csharp-ls so that build, test, and
# C# LSP-driven tools work in the cloud session. Only runs in the cloud
# remote environment (CLAUDE_CODE_REMOTE=true). Local desktop CLI and GitHub
# Actions are untouched.
set -euo pipefail

# Bail out unless we are in Claude Code on the web.
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
   exit 0
fi

log() { echo "[session-start] $*" >&2; }

DOTNET_CHANNEL="10.0"
DOTNET_DIR="$HOME/.dotnet"
DOTNET_TOOLS_DIR="$DOTNET_DIR/tools"
PWSH_DIR="$HOME/.local/share/powershell"
PWSH_VERSION="7.4.6"
LOCAL_BIN="$HOME/.local/bin"

mkdir -p "$LOCAL_BIN"

# -- .NET SDK ----------------------------------------------------------------
if [ ! -x "$DOTNET_DIR/dotnet" ]; then
   log "Installing .NET SDK (channel $DOTNET_CHANNEL)..."
   tmp_installer="$(mktemp)"
   curl -fsSL https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh -o "$tmp_installer"
   chmod +x "$tmp_installer"
   "$tmp_installer" --channel "$DOTNET_CHANNEL" --install-dir "$DOTNET_DIR" --version latest >&2
   rm -f "$tmp_installer"
else
   log ".NET SDK already present at $DOTNET_DIR"
fi

# -- PowerShell --------------------------------------------------------------
if [ ! -x "$PWSH_DIR/pwsh" ]; then
   log "Installing PowerShell $PWSH_VERSION..."
   mkdir -p "$PWSH_DIR"
   tmp_pwsh="$(mktemp --suffix=.tar.gz)"
   curl -fsSL "https://github.com/PowerShell/PowerShell/releases/download/v${PWSH_VERSION}/powershell-${PWSH_VERSION}-linux-x64.tar.gz" -o "$tmp_pwsh"
   tar -xzf "$tmp_pwsh" -C "$PWSH_DIR"
   chmod +x "$PWSH_DIR/pwsh"
   rm -f "$tmp_pwsh"
fi
ln -sf "$PWSH_DIR/pwsh" "$LOCAL_BIN/pwsh"

# -- csharp-ls (C# language server) -----------------------------------------
# Needed for Claude Code's C# LSP probes and for Serena (.serena/project.yml
# is configured for csharp). Install requires the .NET SDK above.
export PATH="$DOTNET_DIR:$DOTNET_TOOLS_DIR:$PATH"
export DOTNET_ROOT="$DOTNET_DIR"
if [ ! -x "$DOTNET_TOOLS_DIR/csharp-ls" ]; then
   log "Installing csharp-ls global tool..."
   dotnet tool install --global csharp-ls >&2 || dotnet tool update --global csharp-ls >&2
fi

# -- Bootstrap test config (auto-created from .defaults on first build) ------
# C-Test and dotnet test expect src/TestUsingPluggableComponentCombinations
# to exist; only the .example/.github-ci templates are checked in.
if [ -f "$CLAUDE_PROJECT_DIR/src/TestUsingPluggableComponentCombinations.example" ] && \
   [ ! -f "$CLAUDE_PROJECT_DIR/src/TestUsingPluggableComponentCombinations" ]; then
   cp "$CLAUDE_PROJECT_DIR/src/TestUsingPluggableComponentCombinations.example" \
      "$CLAUDE_PROJECT_DIR/src/TestUsingPluggableComponentCombinations"
fi

# -- Persist environment for the session ------------------------------------
# CLAUDE_ENV_FILE values are loaded into every tool invocation in the session.
if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
   {
      echo "export DOTNET_ROOT=\"$DOTNET_DIR\""
      echo "export PATH=\"$DOTNET_DIR:$DOTNET_TOOLS_DIR:$LOCAL_BIN:\$PATH\""
      echo "export DOTNET_CLI_TELEMETRY_OPTOUT=1"
      echo "export DOTNET_NOLOGO=true"
      # Performance test config — match CI defaults so timing assertions
      # don't fail on shared cloud infra. See CLAUDE.md.
      echo "export COMPOSABLE_PERFORMANCE_TESTS_STRESS_TEST_ONLY=true"
      echo "export COMPOSABLE_MACHINE_SLOWNESS=5.0"
   } >> "$CLAUDE_ENV_FILE"
fi

log "Setup complete: $($DOTNET_DIR/dotnet --version) | pwsh $($PWSH_DIR/pwsh --version 2>/dev/null | head -1) | csharp-ls $($DOTNET_TOOLS_DIR/csharp-ls --version 2>/dev/null | head -1)"

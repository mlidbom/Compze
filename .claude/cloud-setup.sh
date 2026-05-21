#!/bin/bash
# Cloud environment setup script for Compze.
#
# This is intended to be invoked from the "Setup script" field of a Claude
# Code on the web cloud environment (https://code.claude.com/docs/en/claude-code-on-the-web#setup-scripts).
# Put this single line in that field:
#
#     bash /home/user/Compze/.claude/cloud-setup.sh
#
# Setup scripts run once as root, before Claude Code launches, and the
# resulting filesystem state is snapshotted and reused for later sessions
# (rebuilt roughly every 7 days or when the setup script changes).
#
# What this installs:
#   - .NET SDK 10  (build, test, dotnet tool)
#   - PowerShell   (DevScripts/Compze.psm1, C-Build, C-Test, etc.)
#   - csharp-ls    (C# language server — backs the csharp-lsp plugin and Serena)
#
# What this registers (in the cloud container's user-scope config only):
#   - Serena MCP server (.serena/project.yml drives it; uvx fetches Serena)
#
# The csharp-lsp plugin itself is declared in .claude/settings.json
# (enabledPlugins) so it auto-installs on session start. That path applies
# both locally and in cloud — but the binary it needs is only installed by
# this script, so locally users follow CLAUDE.workarounds.md.
#
# This script also drops a /etc/profile.d/ entry so DOTNET_ROOT and PATH are
# set for every shell in subsequent sessions (matching the pattern used by
# the base image for Node, Java, etc.).
#
# Local desktop CLI and GitHub Actions never invoke this — it lives in the
# repo for visibility and version control, but only the cloud env config
# calls it.
set -euo pipefail

DOTNET_CHANNEL="10.0"
DOTNET_DIR="/opt/dotnet"
DOTNET_TOOLS_DIR="/opt/dotnet-tools"
PWSH_DIR="/opt/powershell"
PWSH_VERSION="7.4.6"
PROFILE_FILE="/etc/profile.d/compze-cloud-env.sh"

log() { echo "[compze-cloud-setup] $*" >&2; }

# -- .NET SDK ----------------------------------------------------------------
if [ ! -x "$DOTNET_DIR/dotnet" ]; then
   log "Installing .NET SDK (channel $DOTNET_CHANNEL) to $DOTNET_DIR..."
   tmp_installer="$(mktemp)"
   curl -fsSL https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh -o "$tmp_installer"
   chmod +x "$tmp_installer"
   "$tmp_installer" --channel "$DOTNET_CHANNEL" --install-dir "$DOTNET_DIR" --version latest >&2
   rm -f "$tmp_installer"
else
   log ".NET SDK already present at $DOTNET_DIR"
fi
ln -sf "$DOTNET_DIR/dotnet" /usr/local/bin/dotnet

# -- PowerShell --------------------------------------------------------------
if [ ! -x "$PWSH_DIR/pwsh" ]; then
   log "Installing PowerShell $PWSH_VERSION to $PWSH_DIR..."
   mkdir -p "$PWSH_DIR"
   tmp_pwsh="$(mktemp --suffix=.tar.gz)"
   curl -fsSL "https://github.com/PowerShell/PowerShell/releases/download/v${PWSH_VERSION}/powershell-${PWSH_VERSION}-linux-x64.tar.gz" -o "$tmp_pwsh"
   tar -xzf "$tmp_pwsh" -C "$PWSH_DIR"
   chmod +x "$PWSH_DIR/pwsh"
   rm -f "$tmp_pwsh"
fi
ln -sf "$PWSH_DIR/pwsh" /usr/local/bin/pwsh

# -- csharp-ls (depends on .NET) --------------------------------------------
# Install as a global tool into a shared location so every session sees it.
export DOTNET_ROOT="$DOTNET_DIR"
export PATH="$DOTNET_DIR:$DOTNET_TOOLS_DIR:$PATH"
mkdir -p "$DOTNET_TOOLS_DIR"
if [ ! -x "$DOTNET_TOOLS_DIR/csharp-ls" ]; then
   log "Installing csharp-ls global tool to $DOTNET_TOOLS_DIR..."
   "$DOTNET_DIR/dotnet" tool install --tool-path "$DOTNET_TOOLS_DIR" csharp-ls >&2
fi
# csharp-ls is a .NET host that requires DOTNET_ROOT at runtime — invoke via
# a shim instead of a bare symlink so callers don't have to set it.
cat > /usr/local/bin/csharp-ls <<EOF
#!/bin/sh
export DOTNET_ROOT="$DOTNET_DIR"
exec "$DOTNET_TOOLS_DIR/csharp-ls" "\$@"
EOF
chmod +x /usr/local/bin/csharp-ls

# -- Persist env for subsequent shells / Claude Code sessions ---------------
# Two layers, because they cover different consumers:
#
# 1. /etc/profile.d/*.sh — sourced by login shells only. Covers interactive
#    SSH/tmux sessions a human opens in the container. Matches the convention
#    used by the base image (nodejs.sh, java.sh, etc.).
# 2. .claude/settings.local.json `env` block — applied by Claude Code to
#    every Bash tool invocation. Claude Code's Bash shells are non-login,
#    so the profile.d file alone does NOT reach them. settings.local.json
#    is the canonical mechanism for per-machine env, and it's gitignored so
#    these cloud-specific values never reach a contributor's laptop.
cat > "$PROFILE_FILE" <<EOF
export DOTNET_ROOT="$DOTNET_DIR"
export PATH="$DOTNET_DIR:$DOTNET_TOOLS_DIR:\$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=true
# Match CI defaults — disables wall-clock timing assertions in perf tests on
# shared cloud infra. See CLAUDE.md.
export COMPOSABLE_PERFORMANCE_TESTS_STRESS_TEST_ONLY=true
export COMPOSABLE_MACHINE_SLOWNESS=5.0
EOF
chmod 0644 "$PROFILE_FILE"

SETTINGS_LOCAL="/home/user/Compze/.claude/settings.local.json"
log "Writing cloud-specific env to $SETTINGS_LOCAL..."
cat > "$SETTINGS_LOCAL" <<EOF
{
  "env": {
    "DOTNET_ROOT": "$DOTNET_DIR",
    "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
    "DOTNET_NOLOGO": "true",
    "COMPOSABLE_PERFORMANCE_TESTS_STRESS_TEST_ONLY": "true",
    "COMPOSABLE_MACHINE_SLOWNESS": "5.0"
  }
}
EOF

# -- Claude CLI integrations (Serena MCP + csharp-lsp plugin) ----------------
# All `claude` writes here land in $HOME/.claude.json (and ~/.claude/plugins/)
# inside the cloud container only. The user's laptop is never touched.
CLAUDE_BIN="${CLAUDE_CODE_EXECPATH:-/opt/claude-code/bin/claude}"
if [ -x "$CLAUDE_BIN" ]; then
   # Serena MCP server. uv/uvx come from the base image; Serena reads
   # .serena/project.yml from the repo on launch (csharp; csharp-ls above
   # satisfies the LSP dependency).
   if ! "$CLAUDE_BIN" mcp list 2>/dev/null | grep -q "^serena"; then
      log "Registering Serena MCP server (user scope)..."
      "$CLAUDE_BIN" mcp add --scope user serena -- \
         uvx --from git+https://github.com/oraios/serena \
         serena start-mcp-server \
         --context ide-assistant \
         --project /home/user/Compze >&2 || log "warning: serena registration failed"
   else
      log "Serena MCP server already registered"
   fi

   # csharp-lsp plugin. `enabledPlugins` in .claude/settings.json is supposed
   # to auto-install on session start, but in cloud sessions that path is
   # unreliable (the marketplace registration races with the auto-install
   # and the plugin ends up missing). Install it explicitly here so the
   # snapshot always carries it.
   #
   # `plugin install <name>@<marketplace>` requires the marketplace to
   # already be known — otherwise it fails with "Plugin not found in
   # marketplace". So add the marketplace first; both commands are idempotent.
   log "Adding claude-plugins-official marketplace (user scope)..."
   "$CLAUDE_BIN" plugin marketplace add anthropics/claude-plugins-official >&2 \
      || log "warning: marketplace add failed (may already be present)"
   log "Installing csharp-lsp plugin (user scope)..."
   "$CLAUDE_BIN" plugin install csharp-lsp@claude-plugins-official >&2 \
      || log "warning: csharp-lsp plugin install failed"
else
   log "warning: claude CLI not found at $CLAUDE_BIN — skipping Serena + plugin setup"
fi

log "Setup complete: $($DOTNET_DIR/dotnet --version) | pwsh $($PWSH_DIR/pwsh --version 2>/dev/null) | csharp-ls $($DOTNET_TOOLS_DIR/csharp-ls --version 2>/dev/null)"

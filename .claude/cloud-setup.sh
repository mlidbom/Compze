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
#   - .NET SDK 10        (build, test, dotnet tool) — via apt (Noble main)
#   - .NET SDK 8         (Compze.Build.FlexRef is pinned to net8.0 in
#                         .config/dotnet-tools.json and needs MSBuild from
#                         the matching SDK — runtime alone is not enough.
#                         Without this, `flexref` and C-FlexRef-Sync fail)
#   - PowerShell         (DevScripts/Compze.psm1, C-Build, C-Test, etc.) —
#                         tarball, since pwsh isn't in the default apt repos
#   - csharp-ls          (C# language server — backs the csharp-lsp plugin
#                         and Serena)
#   - jb                 (JetBrains ReSharper CLI — `jb inspectcode`, see
#                         the ReSharper Inspections section of CLAUDE.md)
#   - docfx              (DocFX — builds the docs site in
#                         src/Websites/Website/)
#   - gh                 (GitHub CLI — read PR check runs, fetch Actions
#                         logs, etc. Auth is provided out-of-band via
#                         GH_TOKEN in the env config.)
#   - local dotnet tools (restored from .config/dotnet-tools.json — currently
#                         just flexref, used by C-FlexRef-Sync)
#
# What this registers (in the cloud container's user-scope config only):
#   - Serena MCP server (.serena/project.yml drives it; uvx fetches Serena)
#
# The csharp-lsp plugin itself is declared in .claude/settings.json
# (enabledPlugins) so it auto-installs on session start. That path applies
# both locally and in cloud — but the binary it needs is only installed by
# this script, so locally users follow .claude/upstream-bug-workarounds.md.
#
# This script also drops a /etc/profile.d/ entry so DOTNET_ROOT and PATH are
# set for every shell in subsequent sessions (matching the pattern used by
# the base image for Node, Java, etc.).
#
# Local desktop CLI and GitHub Actions never invoke this — it lives in the
# repo for visibility and version control, but only the cloud env config
# calls it.
set -euo pipefail

DOTNET_ROOT="/usr/lib/dotnet"
DOTNET_TOOLS_DIR="/opt/dotnet-tools"
PWSH_DIR="/opt/powershell"
PWSH_VERSION="7.4.6"
PROFILE_FILE="/etc/profile.d/compze-cloud-env.sh"

log() { echo "[compze-cloud-setup] $*" >&2; }

# -- apt-installed tools (.NET SDK, GitHub CLI) ------------------------------
# Noble main ships dotnet-sdk-10.0; universe ships gh. Setup scripts run as
# root on Ubuntu 24.04, so apt is the documented install path
# (https://code.claude.com/docs/en/claude-code-on-the-web).
log "Installing .NET SDK 10 + SDK 8, GitHub CLI via apt..."
apt-get update -qq >&2
apt-get install -y -qq dotnet-sdk-10.0 dotnet-sdk-8.0 gh >&2

# -- PowerShell --------------------------------------------------------------
# pwsh isn't in the default apt repos; using Microsoft's third-party apt
# repo would be more code than a single tarball extract.
log "Installing PowerShell $PWSH_VERSION to $PWSH_DIR..."
mkdir -p "$PWSH_DIR"
tmp_pwsh="$(mktemp --suffix=.tar.gz)"
curl -fsSL "https://github.com/PowerShell/PowerShell/releases/download/v${PWSH_VERSION}/powershell-${PWSH_VERSION}-linux-x64.tar.gz" -o "$tmp_pwsh"
tar -xzf "$tmp_pwsh" -C "$PWSH_DIR"
chmod +x "$PWSH_DIR/pwsh"
rm -f "$tmp_pwsh"
ln -sf "$PWSH_DIR/pwsh" /usr/local/bin/pwsh

# -- .NET global tools (csharp-ls, jb, docfx) -------------------------------
# All three are .NET global tools — none have apt packages. Install into a
# shared location so every session sees them.
log "Installing .NET global tools (csharp-ls, jb, docfx) to $DOTNET_TOOLS_DIR..."
export DOTNET_ROOT
mkdir -p "$DOTNET_TOOLS_DIR"
dotnet tool install --tool-path "$DOTNET_TOOLS_DIR" csharp-ls >&2
dotnet tool install --tool-path "$DOTNET_TOOLS_DIR" JetBrains.ReSharper.GlobalTools >&2
dotnet tool install --tool-path "$DOTNET_TOOLS_DIR" docfx >&2
# These tools are .NET hosts that require DOTNET_ROOT at runtime — invoke
# via shims instead of bare symlinks so callers don't have to set it.
# (PATH already includes $DOTNET_TOOLS_DIR, but the shims pin DOTNET_ROOT.)
for tool in csharp-ls jb docfx; do
   cat > "/usr/local/bin/$tool" <<EOF
#!/bin/sh
export DOTNET_ROOT="$DOTNET_ROOT"
exec "$DOTNET_TOOLS_DIR/$tool" "\$@"
EOF
   chmod +x "/usr/local/bin/$tool"
done

# -- Restore repo-local .NET tools ------------------------------------------
# .config/dotnet-tools.json pins flexref (used by C-FlexRef-Sync). Restoring
# now warms the NuGet cache into the snapshot so first-session use is fast
# and works offline if needed.
log "Restoring repo-local .NET tools from .config/dotnet-tools.json..."
(cd /home/user/Compze && dotnet tool restore >&2) || log "warning: dotnet tool restore failed"

# -- Restore the solution ---------------------------------------------------
# Without this, the snapshot has no project.assets.json files, so on first
# session start csharp-ls reports phantom missing-reference errors on every
# test file (FactAttribute, types from other projects, etc.) until the user
# manually builds. Doing it here bakes assets + NuGet cache into the snapshot
# so LSP works immediately and the first cold build is fast.
log "Restoring src/Compze.AllProjects.slnx..."
(cd /home/user/Compze && dotnet restore src/Compze.AllProjects.slnx >&2) || log "warning: dotnet restore failed"

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
export DOTNET_ROOT="$DOTNET_ROOT"
export PATH="$DOTNET_TOOLS_DIR:\$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=true
# Match CI defaults — disables wall-clock timing assertions in perf tests on
# shared cloud infra. See CLAUDE.md.
export COMPOSABLE_PERFORMANCE_TESTS_STRESS_TEST_ONLY=true
export COMPOSABLE_MACHINE_SLOWNESS=5.0
EOF
chmod 0644 "$PROFILE_FILE"

SETTINGS_LOCAL="/home/user/Compze/.claude/settings.local.json"
PERMISSIONS_FILE="/home/user/Compze/.claude/cloud-permissions.json"
log "Writing cloud-specific env + permissions to $SETTINGS_LOCAL..."
# Permissions live in a separate checked-in file so they're reviewable; env is
# inlined here because it interpolates $DOTNET_ROOT. jq merges the two into
# the single settings.local.json that Claude Code reads.
env_json=$(cat <<EOF
{
  "env": {
    "DOTNET_ROOT": "$DOTNET_ROOT",
    "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
    "DOTNET_NOLOGO": "true",
    "COMPOSABLE_PERFORMANCE_TESTS_STRESS_TEST_ONLY": "true",
    "COMPOSABLE_MACHINE_SLOWNESS": "5.0"
  }
}
EOF
)
echo "$env_json" | jq -s '.[0] * .[1]' - "$PERMISSIONS_FILE" > "$SETTINGS_LOCAL"

# -- Claude CLI integrations (Serena MCP + csharp-lsp plugin) ----------------
# All `claude` writes here land in $HOME/.claude.json (and ~/.claude/plugins/)
# inside the cloud container only. The user's laptop is never touched.
CLAUDE_BIN="${CLAUDE_CODE_EXECPATH:-/opt/claude-code/bin/claude}"
if [ -x "$CLAUDE_BIN" ]; then
   # Serena MCP server. uv/uvx come from the base image; Serena reads
   # .serena/project.yml from the repo on launch (csharp; csharp-ls above
   # satisfies the LSP dependency).
   log "Registering Serena MCP server (user scope)..."
   "$CLAUDE_BIN" mcp add --scope user serena -- \
      uvx --from git+https://github.com/oraios/serena \
      serena start-mcp-server \
      --context ide-assistant \
      --project /home/user/Compze >&2 || log "warning: serena registration failed"

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

   # csharp-lsp workaround per .claude/upstream-bug-workarounds.md: until claude-code#16360
   # ships `workspace/configuration`, the plugin can't tell csharp-ls which
   # solution to load via env alone. Drop a `.lsp.json` in the plugin cache
   # that pins `--solution` to ${CLAUDE_PROJECT_DIR}/${CSHARP_LSP_SOLUTION_REL}
   # (set in .claude/settings.json). Without this csharp-ls auto-discovers a
   # subset solution and `test/` projects fail to resolve their references.
   csharp_lsp_dir=$(find /root/.claude/plugins/cache/claude-plugins-official/csharp-lsp -mindepth 1 -maxdepth 1 -type d | head -n1)
   if [ -n "$csharp_lsp_dir" ]; then
      log "Writing csharp-lsp .lsp.json to $csharp_lsp_dir..."
      cat > "$csharp_lsp_dir/.lsp.json" <<'EOF'
{
  "csharp": {
    "command": "csharp-ls",
    "args": ["--solution", "${CLAUDE_PROJECT_DIR}/${CSHARP_LSP_SOLUTION_REL}", "--loglevel", "info"],
    "extensionToLanguage": { ".cs": "csharp", ".csx": "csharp" }
  }
}
EOF
   else
      log "warning: csharp-lsp plugin cache dir not found — skipping .lsp.json write"
   fi
else
   log "warning: claude CLI not found at $CLAUDE_BIN — skipping Serena + plugin setup"
fi

log "Setup complete: dotnet $(dotnet --version) | pwsh $($PWSH_DIR/pwsh --version 2>/dev/null) | csharp-ls $($DOTNET_TOOLS_DIR/csharp-ls --version 2>/dev/null) | jb $($DOTNET_TOOLS_DIR/jb --version 2>/dev/null | head -1) | docfx $($DOTNET_TOOLS_DIR/docfx --version 2>/dev/null | head -1) | $(gh --version 2>/dev/null | head -1)"

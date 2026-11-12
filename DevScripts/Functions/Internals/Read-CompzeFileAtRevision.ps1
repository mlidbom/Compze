# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Read-CompzeFileAtRevision {
    <#
    .SYNOPSIS
    Reads a file of this Compze checkout as it was at a revision: a release tag or a commit

    .DESCRIPTION
    Decodes the file as UTF-8 whatever the console's encoding, which PowerShell would otherwise decode git's output
    with, garbling the non-ASCII characters of a changelog.

    .PARAMETER Revision
    A release tag, such as Compze.Tessaging/v0.6.1-alpha, or a commit.

    .PARAMETER Path
    The file's path from the repository root, such as src/Compze.Tessaging/CHANGELOG.md.

    .OUTPUTS
    The file's lines; $null when the revision doesn't exist in this checkout or has no such file.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Revision,
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    git -C $script:CompzeRoot rev-parse --verify --quiet "$Revision^{commit}" | Out-Null
    if ($LASTEXITCODE -ne 0) { return $null }
    git -C $script:CompzeRoot cat-file -e "${Revision}:$Path" 2>$null
    if ($LASTEXITCODE -ne 0) { return $null }

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new('git')
    foreach ($argument in @('-C', $script:CompzeRoot, 'show', "${Revision}:$Path")) { $startInfo.ArgumentList.Add($argument) }
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.StandardOutputEncoding = [System.Text.UTF8Encoding]::new($false)
    $git = [System.Diagnostics.Process]::Start($startInfo)
    try {
        $errorOutput = $git.StandardError.ReadToEndAsync()
        $content = $git.StandardOutput.ReadToEnd()
        $git.WaitForExit()
        if ($git.ExitCode -ne 0) { throw "git show ${Revision}:$Path failed in $($script:CompzeRoot):`n$($errorOutput.Result)" }
    } finally {
        $git.Dispose()
    }
    return $content -split "\r?\n"
}

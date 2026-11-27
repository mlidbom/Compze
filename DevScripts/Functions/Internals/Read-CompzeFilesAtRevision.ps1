# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Read-CompzeFilesAtRevision {
    <#
    .SYNOPSIS
    Reads files of this Compze checkout as they were at a revision, a release tag or a commit, all through one git process

    .DESCRIPTION
    One git cat-file --batch reads them all, where a git process for each file costs tens of milliseconds more.

    Decodes each file as UTF-8 whatever the console's encoding, which PowerShell would otherwise decode git's output
    with, garbling the non-ASCII characters of a changelog.

    .PARAMETER Revision
    A release tag, such as Compze.Tessaging/v0.6.1-alpha, or a commit.

    .PARAMETER Path
    The files' paths from the repository root, such as src/Compze.Tessaging/CHANGELOG.md.

    .OUTPUTS
    Hashtable from each path to the file's lines: $null for a path where the revision has no file, and for every path
    when the revision doesn't exist in this checkout.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Revision,
        [Parameter(Mandatory = $true)]
        [string[]]$Path
    )

    $utf8 = [System.Text.UTF8Encoding]::new($false)
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new('git')
    foreach ($argument in @('-C', $script:CompzeRoot, 'cat-file', '--batch')) { $startInfo.ArgumentList.Add($argument) }
    $startInfo.RedirectStandardInput = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.StandardInputEncoding = $utf8
    $git = [System.Diagnostics.Process]::Start($startInfo)
    try {
        $errorOutput = $git.StandardError.ReadToEndAsync()
        $output = [System.IO.MemoryStream]::new()
        # Read while the requests are written: git answers each as it reads it, and would block on a full pipe.
        $reading = $git.StandardOutput.BaseStream.CopyToAsync($output)
        foreach ($filePath in $Path) { $git.StandardInput.Write("${Revision}:$filePath`n") }
        $git.StandardInput.Close()
        $reading.Wait()
        $git.WaitForExit()
        if ($git.ExitCode -ne 0) { throw "git cat-file --batch failed in $($script:CompzeRoot):`n$($errorOutput.Result)" }
    } finally {
        $git.Dispose()
    }

    # Each answer is "<object id> <type> <size>", a line feed, the content and a line feed; or, for a name that names no
    # object, the name followed by " missing".
    $bytes = $output.ToArray()
    $position = 0
    $files = @{}
    foreach ($filePath in $Path) {
        $headerEnd = [Array]::IndexOf($bytes, [byte]10, $position)
        $header = $utf8.GetString($bytes, $position, $headerEnd - $position)
        $position = $headerEnd + 1
        if ($header -notmatch '^[0-9a-f]+ (?<type>\S+) (?<size>\d+)$') {
            $files[$filePath] = $null
            continue
        }
        $size = [int]$Matches.size
        $files[$filePath] = if ($Matches.type -eq 'blob') { $utf8.GetString($bytes, $position, $size) -split "\r?\n" } else { $null }
        $position += $size + 1
    }
    return $files
}

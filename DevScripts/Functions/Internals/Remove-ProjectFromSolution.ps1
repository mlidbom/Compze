# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Remove-ProjectFromSolution {
    <#
    .SYNOPSIS
    Removes a project from a solution file

    .DESCRIPTION
    Removes the project's line from the .slnx, and the solution folders it leaves empty, if the solution holds it.
    The project is found by its file name, <ProjectName>.csproj, wherever it lies: src/ or test/ in the flat layout.
    The file is edited as text, so it keeps its own formatting, such as its indentation.

    .PARAMETER ProjectName
    The name of the project to remove from the solution

    .PARAMETER SolutionPath
    Path to the solution file
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectName,

        [Parameter(Mandatory = $true)]
        [string]$SolutionPath
    )

    if (-not (Test-Path $SolutionPath)) {
        Write-Error "Solution file not found: $SolutionPath"
        return
    }

    $text = [System.IO.File]::ReadAllText($SolutionPath)
    $lineEnding = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
    $lines = [System.Collections.Generic.List[string]]::new([string[]]$text.Split($lineEnding))
    $projectLinePattern = '<Project Path="([^"]*/)?' + [regex]::Escape($ProjectName) + '\.csproj"'
    $index = $lines.FindIndex([Predicate[string]] { param($line) $line -match $projectLinePattern })
    if ($index -lt 0) { return } # Project not in solution

    # A project with elements of its own inside it ends at its closing tag.
    $end = $index
    if (-not $lines[$index].TrimEnd().EndsWith('/>')) {
        while ($lines[$end] -notmatch '</Project>') { $end++ }
    }
    $lines.RemoveRange($index, $end - $index + 1)

    # Remove the solution folders the project leaves empty, innermost first.
    while ($index -gt 0 -and $index -lt $lines.Count -and $lines[$index - 1] -match '^\s*<Folder\b[^>]*[^/]>\s*$' -and $lines[$index] -match '^\s*</Folder>\s*$') {
        $lines.RemoveRange($index - 1, 2)
        $index--
    }

    [System.IO.File]::WriteAllText($SolutionPath, ($lines -join $lineEnding), [System.Text.UTF8Encoding]::new($false))
}

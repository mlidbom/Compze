# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function Restore-CombinationsFileCopies {
    <#
    .SYNOPSIS
    Restores the copies of TestUsingPluggableComponentCombinations that Set-CombinationsFileCopies changed to exactly what they were, content and time stamp, if any are recorded

    .DESCRIPTION
    Restores every copy recorded in $script:CombinationsFileCopiesRecordPath, then removes the record. Does nothing when
    there is no record. A copy whose folder is gone, as after a clean, is left gone.

    .PARAMETER WarnIfAnyAreRestored
    Warns when it restores any copies: for a command that finds copies a killed test run left changed.

    .EXAMPLE
    Restore-CombinationsFileCopies -WarnIfAnyAreRestored
    #>
    [CmdletBinding()]
    param(
        [switch]$WarnIfAnyAreRestored
    )

    if (-not (Test-Path -LiteralPath $script:CombinationsFileCopiesRecordPath)) { return }

    $record = @(Get-Content -LiteralPath $script:CombinationsFileCopiesRecordPath -Raw | ConvertFrom-Json)
    foreach ($copy in $record) {
        if (-not (Test-Path -LiteralPath (Split-Path -Parent $copy.Path))) { continue }
        [IO.File]::WriteAllBytes($copy.Path, [Convert]::FromBase64String($copy.ContentBase64))
        [IO.File]::SetLastWriteTimeUtc($copy.Path, [datetime]::new([long]$copy.LastWriteTimeUtcTicks, [DateTimeKind]::Utc))
    }
    Remove-Item -LiteralPath $script:CombinationsFileCopiesRecordPath

    if ($WarnIfAnyAreRestored) {
        Write-Warning "A test run on chosen combinations (C-Test -Combinations) ended without restoring the test projects' copies of TestUsingPluggableComponentCombinations. They are restored now."
    }
}

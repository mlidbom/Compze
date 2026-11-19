# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function Set-CombinationsFileCopies {
    <#
    .SYNOPSIS
    Writes combinations of pluggable components into every test project's copy of TestUsingPluggableComponentCombinations, which is what the tests read, after recording the copies as they are

    .DESCRIPTION
    The build copies src/TestUsingPluggableComponentCombinations into the output folder of each test project, and the
    tests read that copy. So a test run on chosen combinations changes only the copies, after the build: the file itself
    stays as it is, and no build is needed.

    Before it changes any copy, it records each copy's content and time stamp, in the file
    $script:CombinationsFileCopiesRecordPath, from which Restore-CombinationsFileCopies restores them. A run killed before
    it restores them leaves the record, and the next C-Test or C-Build restores from it.

    It gives each changed copy a time stamp far in the past, so that a build outside DevScripts, such as dotnet build,
    copies the file over it again even if nothing restores it: the build copies the file only over a copy older than it.

    Throws when a record is there already, since its copies haven't been restored, and when no test project has a copy,
    since no test project has been built.

    .PARAMETER Combinations
    The combinations, each as a line of TestUsingPluggableComponentCombinations, such as 'PgSql:Microsoft:Newtonsoft:AspNetCore'

    .EXAMPLE
    Set-CombinationsFileCopies -Combinations 'PgSql:Microsoft:Newtonsoft:AspNetCore', 'MySql:Microsoft:Newtonsoft:AspNetCore'
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string[]]$Combinations
    )

    if (Test-Path -LiteralPath $script:CombinationsFileCopiesRecordPath) {
        throw "The copies of TestUsingPluggableComponentCombinations recorded in $script:CombinationsFileCopiesRecordPath have not been restored. Restore them first, with Restore-CombinationsFileCopies."
    }

    $copies = @('src', 'test', 'samples') |
        ForEach-Object { Get-ChildItem -LiteralPath (Join-Path $script:CompzeRoot $_) -Recurse -File -Filter 'TestUsingPluggableComponentCombinations' } |
        Where-Object { $_.FullName -match '\\bin\\' -and $_.FullName -notmatch '\\nCrunchTemp' }
    if (-not $copies) {
        throw "No test project's output folder has a copy of TestUsingPluggableComponentCombinations: build the solution first."
    }

    $record = @($copies | ForEach-Object {
        [pscustomobject]@{
            Path                  = $_.FullName
            ContentBase64         = [Convert]::ToBase64String([IO.File]::ReadAllBytes($_.FullName))
            LastWriteTimeUtcTicks = $_.LastWriteTimeUtc.Ticks.ToString() # As a string: a JSON number may not keep 64 bits exactly.
        }
    })
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $script:CombinationsFileCopiesRecordPath) | Out-Null
    ConvertTo-Json -InputObject $record -Depth 2 | Set-Content -LiteralPath $script:CombinationsFileCopiesRecordPath -Encoding utf8

    $content = (@('# Written by C-Test -Combinations, which restores the copy as it was once the run is over.') + $Combinations) -join "`r`n"
    $farInThePast = [datetime]::new(2000, 1, 1, 0, 0, 0, [DateTimeKind]::Utc)
    foreach ($copy in $copies) {
        [IO.File]::WriteAllText($copy.FullName, $content)
        [IO.File]::SetLastWriteTimeUtc($copy.FullName, $farInThePast)
    }
}

# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Test-Combinations {
    <#
    .SYNOPSIS
    Runs the built tests against the pluggable-component combinations you name, leaving your own combinations file alone

    .DESCRIPTION
    Copies every built test project's output to a temporary folder, writes the given combinations into each copy's
    TestUsingPluggableComponentCombinations, and runs each copy's test executable, one after another. The copies are
    deleted afterwards.

    Neither src/TestUsingPluggableComponentCombinations nor any build output is touched, so what C-Test, the IDE and
    NCrunch test stays what you configured. That is the point of the command: checking a change against another
    combination, such as another transport, without reconfiguring and rebuilding.

    It runs what is built, in the Debug configuration: build first (C-Build). It does not run Compze.Tests.CodePolicies,
    which judges the repository itself and whose verdict no combination changes: C-Test runs it.

    Sets $LASTEXITCODE to 0 on success and 1 on failure, and reports a failure as an error of its own, so that
    `pwsh -Command "...; C-Test-Combinations ..."` exits 1 too.

    .PARAMETER Combinations
    The combinations to test, each as PersistenceLayer:DIContainer:Serializer:Transport

    .PARAMETER Project
    A wildcard that test project names must match. All test projects when omitted.

    .EXAMPLE
    C-Test-Combinations SqliteMemory:Microsoft:Newtonsoft:Tcp
    Runs every test project against that one combination

    .EXAMPLE
    C-Test-Combinations SqliteMemory:Microsoft:Newtonsoft:Tcp, SqliteMemory:Microsoft:Newtonsoft:NamedPipes -Project Compze.Tessaging.*
    Runs the Tessaging test projects against both combinations
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidGlobalVars', '')]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string[]]$Combinations,

        [string]$Project = '*'
    )

    # Default to stress-test-only mode for performance tests, as C-Test does.
    if (-not $env:COMPOSABLE_PERFORMANCE_TESTS_STRESS_TEST_ONLY) {
        $env:COMPOSABLE_PERFORMANCE_TESTS_STRESS_TEST_ONLY = "true"
    }

    # The code policies judge the repository itself - its solution file and its sources - which a copy of their output
    # outside the repository cannot reach, and no combination changes their verdict. C-Test runs them.
    $projectsThatJudgeTheRepositoryItself = @('Compze.Tests.CodePolicies')

    # A project in a test folder is a test project unless it says otherwise: test/Directory.Build.props sets IsTestProject.
    $projectsInTestFolders = @(Get-ChildItem -Path (Join-Path $script:CompzeRoot 'test'), (Join-Path $script:CompzeRoot 'samples') -Recurse -Filter '*.csproj' |
            Where-Object { $_.Name -notlike 'nCrunchTemp_*' -and $_.FullName -match '[\\/]test[\\/]' })
    $isATestProject = { (Get-Content $_.FullName -Raw) -notmatch '<IsTestProject>\s*false\s*</IsTestProject>' }
    $testProjects = @($projectsInTestFolders | Where-Object $isATestProject |
            Where-Object { $_.BaseName -like $Project -and $_.BaseName -notin $projectsThatJudgeTheRepositoryItself })

    if ($testProjects.Count -eq 0) {
        Write-CommandFailure -Command $PSCmdlet -Message "No test project matches '$Project'."
        return
    }

    $temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) "Compze\TestCombinations\$([Guid]::NewGuid().ToString('N'))"
    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    $total = 0; $failed = 0; $skipped = 0
    $failedProjects = @()
    $projectsWithoutATestExecutable = @()

    try {
        # Tests launch or load what the other projects in the test folders build, such as the endpoint host process, and find
        # it as the sibling of their own output: so those outputs are copied alongside, each under its project's name.
        foreach ($supportProject in @($projectsInTestFolders | Where-Object { -not (& $isATestProject) })) {
            $builtOutput = Join-Path $supportProject.DirectoryName 'bin\Debug\net10.0'
            if (Test-Path $builtOutput) {
                robocopy $builtOutput (Join-Path $temporaryRoot $supportProject.BaseName) /E /NFL /NDL /NJH /NJS /NP | Out-Null
                if ($LASTEXITCODE -ge 8) {
                    Write-CommandFailure -Command $PSCmdlet -Message "Copying $builtOutput failed: robocopy exited with $LASTEXITCODE."
                    return
                }
            }
        }

        foreach ($testProject in $testProjects) {
            $builtOutput = Join-Path $testProject.DirectoryName 'bin\Debug\net10.0'
            $executableName = "$($testProject.BaseName).exe"
            if (-not (Test-Path (Join-Path $builtOutput $executableName))) {
                # Built, but not as a test executable this command can run from a copy: say so in the summary instead of passing over it silently.
                if (Test-Path (Join-Path $builtOutput "$($testProject.BaseName).dll")) {
                    $projectsWithoutATestExecutable += $testProject.BaseName
                    continue
                }
                Write-CommandFailure -Command $PSCmdlet -Message "$($testProject.BaseName) is not built: $executableName is missing from $builtOutput. Run C-Build first."
                return
            }

            $copy = Join-Path $temporaryRoot $testProject.BaseName
            robocopy $builtOutput $copy /E /NFL /NDL /NJH /NJS /NP | Out-Null
            if ($LASTEXITCODE -ge 8) {
                Write-CommandFailure -Command $PSCmdlet -Message "Copying $builtOutput to $copy failed: robocopy exited with $LASTEXITCODE."
                return
            }
            Set-Content -Path (Join-Path $copy 'TestUsingPluggableComponentCombinations') -Value $Combinations

            $output = & (Join-Path $copy $executableName) 2>&1 | ForEach-Object { "$_" }
            $exitCode = $LASTEXITCODE

            $summary = $output | Select-String -Pattern 'Total: (\d+), Errors: (\d+), Failed: (\d+), Skipped: (\d+)' | Select-Object -Last 1
            if ($summary) {
                $counts = $summary.Matches[0].Groups
                $total += [int]$counts[1].Value
                $failed += [int]$counts[2].Value + [int]$counts[3].Value
                $skipped += [int]$counts[4].Value
            }

            # A test process that died without reporting a failed test fails the run too, as in C-Test.
            if ($exitCode -ne 0 -or -not $summary) {
                $failedProjects += $testProject.BaseName
                Write-Host "`n=== $($testProject.BaseName) failed (exit code $exitCode) ===" -ForegroundColor Red
                $output | Write-Host
            }
        }
    } finally {
        Remove-Item -Path $temporaryRoot -Recurse -Force -ErrorAction SilentlyContinue
    }

    $elapsedSeconds = [math]::Round($stopwatch.Elapsed.TotalSeconds, 1)
    Write-Host "Combinations: $($Combinations -join ', ')"
    Write-Host "Failed: $failed, Passed: $($total - $failed - $skipped), Skipped: $skipped, Total: $total, Elapsed: $elapsedSeconds seconds"
    if ($projectsWithoutATestExecutable.Count -gt 0) {
        Write-Host "Not run, because they build no test executable: $($projectsWithoutATestExecutable -join ', ')"
    }

    if ($failedProjects.Count -gt 0) {
        Write-CommandFailure -Command $PSCmdlet -Message "$($failedProjects.Count) test project(s) failed: $($failedProjects -join ', ')."
    } else {
        $global:LASTEXITCODE = 0
    }
}

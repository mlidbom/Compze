# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Add-ProjectBesideItsNeighbour {
    <#
    .SYNOPSIS
    Adds a project to every solution that holds its neighbour, in the neighbour's solution folder

    .DESCRIPTION
    The neighbour is the project in Compze.AllProjects.slnx whose name shares the most leading dot-separated segments
    with the project's name, at least two: Compze.Teventive.StateStore.MemoryPack's is Compze.Teventive.StateStore.
    Among equals, a project of the same kind, test or library, wins, then the first in the solution.

    The project is written on the line after its neighbour's, in each .slnx at the repository root that holds the
    neighbour. So it lands in the neighbour's solution folder, in every solution a developer works in the neighbour
    from, and each file keeps its own formatting. A project that no solution holds is restored as a NuGet package
    wherever another project references it, and a new project has no package yet.

    A project with no neighbour goes at the root of Compze.AllProjects.slnx, with a warning to arrange it.

    .PARAMETER ProjectName
    The name of the project, such as Compze.Teventive.StateStore.MemoryPack

    .PARAMETER ProjectPath
    The project file's path relative to the repository root, such as src/Compze.X/Compze.X.csproj
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectName,

        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    $ProjectPath = $ProjectPath -replace '\\', '/'
    $neighbour = Find-NeighbourProject -ProjectName $ProjectName -ProjectPath $ProjectPath

    if (-not $neighbour) {
        Ensure-ProjectIsInSolution -ProjectName $ProjectName -SolutionPath $script:CompzeSolutionPath
        Write-Warning "No project shares more than the first part of $ProjectName's name, so it was added at the root of Compze.AllProjects.slnx. Move it into its solution folder, and into the other solutions it belongs in."
        return
    }

    foreach ($solution in Get-ChildItem -Path $script:CompzeRoot -Filter "*.slnx" -File) {
        Add-ProjectAfterNeighbourInSolution -SolutionPath $solution.FullName -ProjectPath $ProjectPath -NeighbourPath $neighbour
    }
}

function Find-NeighbourProject {
    <#
    .SYNOPSIS
    The path, as Compze.AllProjects.slnx writes it, of the project that shares the most leading name segments with ProjectName
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectName,

        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    $nameSegments = $ProjectName.Split('.')
    $isTest = Test-IsTestProjectName $ProjectName

    $candidates = [regex]::Matches((Get-Content -Raw $script:CompzeSolutionPath), '<Project Path="([^"]+)"') |
        ForEach-Object { $_.Groups[1].Value } |
        Where-Object { $_ -ne $ProjectPath } |
        ForEach-Object {
            $candidateSegments = [System.IO.Path]::GetFileNameWithoutExtension($_).Split('.')
            $shared = 0
            while ($shared -lt $nameSegments.Length -and $shared -lt $candidateSegments.Length -and $nameSegments[$shared] -eq $candidateSegments[$shared]) { $shared++ }
            [pscustomobject]@{
                Path = $_
                SharedSegments = $shared
                SameKind = ((Test-IsTestProjectName ($candidateSegments -join '.')) -eq $isTest)
            }
        } |
        Where-Object { $_.SharedSegments -ge 2 }

    $best = $candidates | Sort-Object -Property @{ Expression = 'SharedSegments'; Descending = $true }, @{ Expression = 'SameKind'; Descending = $true } -Stable | Select-Object -First 1
    if ($best) { return $best.Path }
    return $null
}

function Test-IsTestProjectName {
    param([string]$ProjectName)
    return ($ProjectName -match '\.Tests\.' -or $ProjectName -match '\.Tests$' -or $ProjectName -match 'Specifications$')
}

function Add-ProjectAfterNeighbourInSolution {
    <#
    .SYNOPSIS
    Writes the project's line after the neighbour's in one solution, if the solution holds the neighbour and not yet the project
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$SolutionPath,

        [Parameter(Mandatory = $true)]
        [string]$ProjectPath,

        [Parameter(Mandatory = $true)]
        [string]$NeighbourPath
    )

    $text = [System.IO.File]::ReadAllText($SolutionPath)
    if ($text.Contains("Path=""$ProjectPath""")) { return }

    $lineEnding = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
    $lines = [System.Collections.Generic.List[string]]::new([string[]]$text.Split($lineEnding))
    $neighbourLine = $lines.FindIndex([Predicate[string]] { param($line) $line.Contains("Path=""$NeighbourPath""") })
    if ($neighbourLine -lt 0) { return }

    $indentation = [regex]::Match($lines[$neighbourLine], '^\s*').Value
    # A neighbour with elements of its own inside it is followed by them: the new line then goes before it, still in its folder.
    $insertAt = if ($lines[$neighbourLine].TrimEnd().EndsWith('/>')) { $neighbourLine + 1 } else { $neighbourLine }
    $lines.Insert($insertAt, "$indentation<Project Path=""$ProjectPath"" />")

    [System.IO.File]::WriteAllText($SolutionPath, ($lines -join $lineEnding), [System.Text.UTF8Encoding]::new($false))
}

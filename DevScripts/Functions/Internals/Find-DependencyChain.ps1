# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Find-DependencyChain {
    <#
    .SYNOPSIS
    Finds how a project's restore graph gets a package at the version it resolved: the chain of references from the project to the package, as dotnet nuget why shows one

    .DESCRIPTION
    Of the chains, the shortest whose last link requires the resolved version itself: the link before the package is
    then what holds the project at that version. Without such a chain, as when transitive pinning raised the version,
    the shortest chain.

    .PARAMETER Graph
    A restore graph, as Read-SolutionRestoreGraphs reads it.

    .PARAMETER PackageId
    A package the graph resolves.

    .OUTPUTS
    An object with:
    - Links: from the first package or project the project references, to the package. Each an object with Name,
      IsProject and Version (the version resolved; $null for a project).
    - Holder: the link before the package, which requires it; $null when the project references the package directly.
    - Text: the chain as "<project> -> (project) <name> -> <package> <version> -> ...".
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        $Graph,
        [Parameter(Mandatory = $true)]
        [string]$PackageId
    )

    $resolvedVersion = $Graph.Packages[$PackageId].Version
    $links = @(if ($Graph.DirectPackageReferences.ContainsKey($PackageId)) {
        NewChainLink $Graph $PackageId
    } else {
        $parentByName = FindShortestPathsFromTheProject $Graph
        $holderName = @($parentByName.Keys |
                        Where-Object { (GetDependenciesOf $Graph $_).ContainsKey($PackageId) } |
                        Sort-Object { if ((GetDependenciesOf $Graph $_)[$PackageId] -eq $resolvedVersion) { 0 } else { 1 } }, { @(GetPathTo $parentByName $_).Count }, { $_ }) |
                      Select-Object -First 1
        if (-not $holderName) { throw "$($Graph.ProjectName) resolves $PackageId, but nothing in its restore graph requires it." }
        @(GetPathTo $parentByName $holderName) + @($PackageId) | ForEach-Object { NewChainLink $Graph $_ }
    })
    return [PSCustomObject]@{
        Links  = $links
        Holder = if ($links.Count -gt 1) { $links[-2] } else { $null }
        Text   = (@($Graph.ProjectName) + @($links | ForEach-Object { if ($_.IsProject) { "(project) $($_.Name)" } else { "$($_.Name) $($_.Version)" } })) -join ' -> '
    }
}

function FindShortestPathsFromTheProject($Graph) {
    # Breadth first from the project's direct references: each name reached, to the name it was first reached from
    # ($null for a direct reference).
    $parentByName = @{}
    $queue = [System.Collections.Generic.Queue[string]]::new()
    foreach ($name in $Graph.DirectDependencies) {
        if (-not $parentByName.ContainsKey($name)) { $parentByName[$name] = $null; $queue.Enqueue($name) }
    }
    while ($queue.Count -gt 0) {
        $name = $queue.Dequeue()
        foreach ($dependencyName in @((GetDependenciesOf $Graph $name).Keys)) {
            if (-not $parentByName.ContainsKey($dependencyName)) { $parentByName[$dependencyName] = $name; $queue.Enqueue($dependencyName) }
        }
    }
    return $parentByName
}

function GetPathTo($ParentByName, [string]$Name) {
    $path = [System.Collections.Generic.List[string]]::new()
    for ($current = $Name; $current; $current = $ParentByName[$current]) { $path.Insert(0, $current) }
    return $path.ToArray()
}

function GetDependenciesOf($Graph, [string]$Name) {
    if ($Graph.Packages.ContainsKey($Name)) { return $Graph.Packages[$Name].Dependencies }
    if ($Graph.Projects.ContainsKey($Name)) { return $Graph.Projects[$Name].Dependencies }
    return @{}
}

function NewChainLink($Graph, [string]$Name) {
    $isProject = $Graph.Projects.ContainsKey($Name)
    return [PSCustomObject]@{ Name = $Name; IsProject = $isProject; Version = if ($isProject) { $null } else { $Graph.Packages[$Name].Version } }
}

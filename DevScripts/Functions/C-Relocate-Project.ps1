# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Relocate-Project {
    <#
    .SYNOPSIS
    Relocates a project to its correct flat-layout directory
    
    .DESCRIPTION
    Moves a project to follow the flat layout conventions:
    - Library projects: src/<ProjectName>/<ProjectName>.csproj
    - Test projects: test/<ProjectName>/<ProjectName>.csproj
    Updates all ProjectReference paths and the solution file.
    
    .PARAMETER ProjectName
    The full name of the project to relocate
    
    .PARAMETER SolutionPath
    Path to the solution file (defaults to src\Compze.slnx)
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectName,
        [string]$SolutionPath
    )
    
    if (-not $SolutionPath) { $SolutionPath = $script:CompzeSolutionPath }
    if (-not (Test-Path $SolutionPath)) { Write-Error "Solution file not found: $SolutionPath"; return }
    
    $solutionDir = Split-Path -Parent $SolutionPath
    $repoRoot = $script:CompzeRoot
    
    # Step 1: Find project
    $projectFile = Find-ProjectFile -SolutionPath $SolutionPath -ProjectName $ProjectName
    if (-not $projectFile) { Write-Error "Project file not found"; return }
    
    $currentProjectDir = Split-Path -Parent $projectFile.FullName
    $projectFileName = Split-Path -Leaf $projectFile.FullName
    
    # Step 2: Calculate target using flat layout
    $isTest = ($ProjectName -match '\.Tests\.' -or $ProjectName -match '\.Tests$')
    if ($isTest) {
        $targetProjectDir = Join-Path $repoRoot "test" $ProjectName
    } else {
        $targetProjectDir = Join-Path $solutionDir $ProjectName
    }
    
    if ($currentProjectDir.TrimEnd('\') -eq $targetProjectDir.TrimEnd('\')) { return }
    
    # Step 3: Move directory
    if (Test-Path $targetProjectDir) {
        $targetCsproj = Get-ChildItem -Path $targetProjectDir -Filter "*.csproj" | Select-Object -First 1
        if ($targetCsproj) {
            Write-Error "Target directory already exists with a project file"
            return
        }
        Remove-Item -Path $targetProjectDir -Recurse -Force
    }
    $targetParentDir = Split-Path -Parent $targetProjectDir
    if (-not (Test-Path $targetParentDir)) {
        New-Item -ItemType Directory -Path $targetParentDir -Force | Out-Null
    }
    
    # Check if target is a subdirectory of source (cannot move a folder into itself)
    $normalizedCurrent = $currentProjectDir.TrimEnd('\') + '\'
    $normalizedTarget = $targetProjectDir.TrimEnd('\') + '\'
    if ($normalizedTarget.StartsWith($normalizedCurrent, [StringComparison]::OrdinalIgnoreCase)) {
        $tempDir = Join-Path $solutionDir ("temp_" + [Guid]::NewGuid().ToString())
        Move-Item -Path $currentProjectDir -Destination $tempDir -Force
        $targetParentDir = Split-Path -Parent $targetProjectDir
        if (-not (Test-Path $targetParentDir)) {
            New-Item -ItemType Directory -Path $targetParentDir -Force | Out-Null
        }
        Move-Item -Path $tempDir -Destination $targetProjectDir -Force
    } else {
        Move-Item -Path $currentProjectDir -Destination $targetProjectDir -Force
    }
    
    # Step 4: Update the moved project's own references
    $movedProjectFile = Join-Path $targetProjectDir $projectFileName
    $movedProjectContent = Get-Content $movedProjectFile -Raw
    $movedProjectDir = Split-Path -Parent $movedProjectFile
    
    $referencePattern = '<ProjectReference\s+Include="([^"]+)"'
    $updatedContent = $movedProjectContent
    
    if ($movedProjectContent -match $referencePattern) {
        $matches = [regex]::Matches($movedProjectContent, $referencePattern)
        foreach ($match in $matches) {
            $oldRelativePath = $match.Groups[1].Value
            $absolutePath = [System.IO.Path]::GetFullPath((Join-Path $currentProjectDir $oldRelativePath))
            $newRelativePath = [System.IO.Path]::GetRelativePath($movedProjectDir, $absolutePath)
            $updatedContent = $updatedContent -replace [regex]::Escape($oldRelativePath), $newRelativePath
        }
        if ($updatedContent -ne $movedProjectContent) {
            Set-Content -Path $movedProjectFile -Value $updatedContent -NoNewline -Encoding UTF8
        }
    }
    
    # Step 5: Update csproj references in other projects
    $allProjects = Get-AllProjectFiles -SolutionPath $SolutionPath
    $allProjects | ForEach-Object {
        $content = Get-Content $_.FullName -Raw
        $pattern = '(<ProjectReference\s+Include=")([^"]*[/\\]' + [regex]::Escape($ProjectName) + '\.csproj)(")'
        if ($content -match $pattern) {
            $csprojDir = Split-Path -Parent $_.FullName
            $newPath = [System.IO.Path]::GetRelativePath($csprojDir, (Join-Path $targetProjectDir $projectFileName))
            $newContent = $content -replace $pattern, ('$1' + $newPath + '$3')
            if ($content -ne $newContent) {
                Set-Content -Path $_.FullName -Value $newContent -NoNewline -Encoding UTF8
            }
        }
    }
    
    # Step 6: Update solution paths
    Get-ChildItem -Path $solutionDir -Filter "*.slnx" -Recurse | ForEach-Object {
        $content = Get-Content $_.FullName -Raw
        $pattern = '(<Project\s+Path=")([^"]*[/\\]' + [regex]::Escape($ProjectName) + '\.csproj)(")'
        if ($content -match $pattern) {
            $slnDir = Split-Path -Parent $_.FullName
            $newPath = [System.IO.Path]::GetRelativePath($slnDir, (Join-Path $targetProjectDir $projectFileName)) -replace '\\', '/'
            $newContent = $content -replace $pattern, ('$1' + $newPath + '$3')
            if ($content -ne $newContent) {
                Set-Content -Path $_.FullName -Value $newContent -NoNewline -Encoding UTF8
            }
        }
    }
    
    # Step 7: Update solution folder structure
    C-Place-ProjectInSolution -ProjectName $ProjectName -SolutionPath $SolutionPath
}

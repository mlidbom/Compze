function C-Relocate-Project {
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
    
    # Step 1: Find project
    $projectFileName = "$ProjectName.csproj"
    $projectFile = Get-ChildItem -Path $solutionDir -Filter $projectFileName -Recurse | Select-Object -First 1
    if (-not $projectFile) { Write-Error "Project file not found"; return }
    
    $currentProjectDir = Split-Path -Parent $projectFile.FullName
    
    # Step 2: Calculate target
    $targetRelativePath = $ProjectName -replace '\.', '\'
    $targetProjectDir = Join-Path $solutionDir $targetRelativePath
    $targetSolutionPath = $ProjectName -replace '\.', '/'
    
    if ($currentProjectDir -eq $targetProjectDir) { return }
    
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
    Move-Item -Path $currentProjectDir -Destination $targetProjectDir
    
    # Step 4: Update csproj references
    Get-ChildItem -Path $solutionDir -Filter "*.csproj" -Recurse | ForEach-Object {
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
    
    # Step 5: Update solution paths
    Get-ChildItem -Path $solutionDir -Filter "*.slnx" -Recurse | ForEach-Object {
        $content = Get-Content $_.FullName -Raw
        $pattern = '(<Project\s+Path=")([^"]*[/\\]' + [regex]::Escape($ProjectName) + '\.csproj)(")'
        if ($content -match $pattern) {
            $newPath = $targetSolutionPath + '/' + $projectFileName
            $newContent = $content -replace $pattern, ('$1' + $newPath + '$3')
            if ($content -ne $newContent) {
                Set-Content -Path $_.FullName -Value $newContent -NoNewline -Encoding UTF8
            }
        }
    }
    
    # Step 6: Update solution folder structure
    C-Relocate-ProjectInSolution -ProjectName $ProjectName -SolutionPath $SolutionPath
    
    # Step 7: Ensure csproj files are correct
    C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders
}

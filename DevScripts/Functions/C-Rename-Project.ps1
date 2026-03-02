# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Rename-Project {
    <#
    .SYNOPSIS
    Renames a project and updates all references
    
    .DESCRIPTION
    Renames a project file and updates all references throughout the solution:
    - Renames the project file (.csproj)
    - Renames any associated .ncrunchproject files
    - Updates ProjectReference elements in all .csproj files
    - Updates Project Path references in all solution files (.slnx and .sln)
    - Updates .ncrunchproject references in .ncrunchsolution files
    
    The script will search for and update ALL solution files found in the solution
    directory, not just a single file.
    
    .PARAMETER Old
    The current name of the project to rename (e.g., "Compze.Tessaging.Hosting.Configuration")
    
    .PARAMETER New
    The new name for the project (e.g., "Compze.Common.Configuration")
    
    .PARAMETER SolutionPath
    Path to the solution file (defaults to src\Compze.AllProjects.slnx)
    
    .EXAMPLE
    C-Rename-Project -Old Compze.Tessaging.Hosting.Configuration -New Compze.Common.Configuration
    Renames the project and all references to it
    
    .EXAMPLE
    C-Rename-Project -Old Compze.Old.Name -New Compze.New.Name -SolutionPath "src\MySolution.slnx"
    Renames the project using a custom solution path
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Old,
        
        [Parameter(Mandatory = $true)]
        [string]$New,
        
        [string]$SolutionPath
    )
    
    # Set default solution path if not provided
    if (-not $SolutionPath) {
        $SolutionPath = $script:CompzeSolutionPath
    }
    
    if (-not (Test-Path $SolutionPath)) {
        Write-Error "Solution file not found: $SolutionPath"
        return
    }
    
    $solutionDir = Split-Path -Parent $SolutionPath
    
    # Step 1: Find the project file
    $oldProjectFileName = "$Old.csproj"
    $newProjectFileName = "$New.csproj"
    
    # Search for the project file
    $projectFile = Get-ChildItem -Path $solutionDir -Filter $oldProjectFileName -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    
    if (-not $projectFile) {
        Write-Error "Project file '$oldProjectFileName' not found in solution directory"
        return
    }
    
    $projectDir = Split-Path -Parent $projectFile.FullName
    $newProjectPath = Join-Path $projectDir $newProjectFileName
    
    # Step 2: Rename the project file and any associated .ncrunchproject files
    if (Test-Path $newProjectPath) {
        Write-Error "Target project file already exists: $newProjectPath"
        return
    }
    
    Rename-Item -Path $projectFile.FullName -NewName $newProjectFileName
    
    # Check for and rename any .ncrunchproject files
    $ncrunchFiles = Get-ChildItem -Path $projectDir -Filter "$Old.*.ncrunchproject" -ErrorAction SilentlyContinue
    $ncrunchFilesRenamed = 0
    
    foreach ($ncrunchFile in $ncrunchFiles) {
        $ncrunchExtension = $ncrunchFile.Name.Substring($Old.Length) # e.g., ".v3.ncrunchproject"
        $newNcrunchFileName = "$New$ncrunchExtension"
        Rename-Item -Path $ncrunchFile.FullName -NewName $newNcrunchFileName
        $ncrunchFilesRenamed++
    }
    
    # Step 3: Update ProjectReference elements in all .csproj files
    $allCsprojFiles = Get-AllProjectFiles -SolutionPath $SolutionPath
    $projectReferencesUpdated = 0
    
    foreach ($csproj in $allCsprojFiles) {
        $content = Get-Content $csproj.FullName -Raw
        
        # Match ProjectReference with the old project name
        # Pattern: <ProjectReference Include="...path...\OldName.csproj" />
        $pattern = '(<ProjectReference\s+Include="[^"]*\\)(' + [regex]::Escape($Old) + ')(\.csproj")'
        
        if ($content -match $pattern) {
            $content = $content -replace $pattern, ('$1' + $New + '$3')
            Set-Content -Path $csproj.FullName -Value $content -NoNewline -Encoding UTF8
            $projectReferencesUpdated++
        }
    }
    
    # Step 4: Update solution files (.slnx and .sln)
    $slnxFiles = Get-ChildItem -Path $solutionDir -Filter "*.slnx" -Recurse
    $slnFiles = Get-ChildItem -Path $solutionDir -Filter "*.sln" -Recurse
    $allSolutionFiles = @($slnxFiles) + @($slnFiles)
    $solutionFilesUpdated = 0
    
    foreach ($solutionFile in $allSolutionFiles) {
        $content = Get-Content $solutionFile.FullName -Raw
        
        # Match Project Path with the old project name
        # Pattern: <Project Path="...path.../OldName.csproj" /> (for .slnx files)
        # Note: Solution files use forward slashes, so we match both / and \
        $pattern = '(<Project\s+Path="[^"]*[/\\])(' + [regex]::Escape($Old) + ')(\.csproj")'
        
        if ($content -match $pattern) {
            $content = $content -replace $pattern, ('$1' + $New + '$3')
            Set-Content -Path $solutionFile.FullName -Value $content -NoNewline -Encoding UTF8
            $solutionFilesUpdated++
        }
    }
    
    # Step 5: Run C-Clean to avoid build errors from leftover artifacts
    C-Clean
}

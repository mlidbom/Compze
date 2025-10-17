function C-Create-Project {
    <#
    .SYNOPSIS
    Creates a new project with the proper directory structure and adds it to the solution
    
    .DESCRIPTION
    Creates a new C# project following the Compze conventions:
    - Creates the project directory based on namespace (e.g., Compze.Wiring.Testing -> src/Compze/Wiring/Testing/)
    - Creates a basic .csproj file
    - Adds the project to the .slnx solution file in the appropriate folder structure
    - Runs C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders to update parent projects
    
    .PARAMETER ProjectName
    The full name of the project to create (e.g., "Compze.Wiring.Testing")
    
    .EXAMPLE
    C-Create-Project -ProjectName Compze.Wiring.Testing
    Creates a new project at src/Compze/Wiring/Testing/Compze.Wiring.Testing.csproj
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectName
    )
    
    $SolutionPath = $script:CompzeSolutionPath
    
    if (-not (Test-Path $SolutionPath)) {
        Write-Error "Solution file not found: $SolutionPath"
        return
    }
    
    $solutionDir = Split-Path -Parent $SolutionPath
    
    # Step 1: Calculate directory path from project name
    # Compze.Wiring.Testing -> Compze/Wiring/Testing
    $relativePath = $ProjectName -replace '\.', '\'
    $projectDir = Join-Path $solutionDir $relativePath
    
    # Check if directory already exists
    if (Test-Path $projectDir) {
        $existingCsproj = Get-ChildItem -Path $projectDir -Filter "*.csproj" | Select-Object -First 1
        if ($existingCsproj) {
            Write-Error "Project directory already exists with a .csproj file: $projectDir"
            return
        }
    }
    
    # Step 2: Create the directory
    if (-not (Test-Path $projectDir)) {
        New-Item -ItemType Directory -Path $projectDir -Force | Out-Null
    }
    
    # Step 3: Create the .csproj file
    $csprojPath = Join-Path $projectDir "$ProjectName.csproj"
    if (Test-Path $csprojPath) {
        Write-Error "Project file already exists: $csprojPath"
        return
    }
    
    $csprojContent = @"
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFramework>net9.0</TargetFramework>
		<ImplicitUsings>enable</ImplicitUsings>
		<Nullable>enable</Nullable>
	</PropertyGroup>

</Project>
"@
    
    Set-Content -Path $csprojPath -Value $csprojContent -NoNewline -Encoding UTF8
    
    # Step 4: Add project to solution and organize in correct folder
    C-Place-ProjectInSolution -ProjectName $ProjectName
    
    # Step 5: Update parent projects to exclude this directory if needed
    C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders
}

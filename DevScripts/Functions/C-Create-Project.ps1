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
    
    # Step 4: Add to solution file
    # Calculate the solution path format (uses forward slashes)
    $solutionProjectPath = $ProjectName -replace '\.', '/'
    $solutionProjectPath = "$solutionProjectPath/$ProjectName.csproj"
    
    # Read the solution file
    $solutionContent = Get-Content $SolutionPath -Raw
    
    # Check if project already exists in solution
    if ($solutionContent -match [regex]::Escape($solutionProjectPath)) {
        Write-Warning "Project already exists in solution file"
    } else {
        # Calculate the folder structure for the solution
        # For Compze.Wiring.Testing, the folder should be /Compze/Wiring/
        $namespaceSegments = $ProjectName.Split('.')
        
        # Determine the folder path
        if ($namespaceSegments.Count -gt 1) {
            # Remove the last segment (the project name itself)
            $folderSegments = $namespaceSegments[0..($namespaceSegments.Count - 2)]
            $folderPath = "/" + ($folderSegments -join "/") + "/"
        } else {
            # Root level project
            $folderPath = "/"
        }
        
        # Find the folder in the solution file
        $folderPattern = '<Folder Name="' + [regex]::Escape($folderPath) + '"'
        if ($solutionContent -match $folderPattern) {
            # Find the position to insert the project
            # We want to insert it after the Folder opening tag, at the beginning of the folder's content
            $folderMatch = [regex]::Match($solutionContent, "(<Folder Name=`"$([regex]::Escape($folderPath))`"[^>]*>)(.*?)(</Folder>)", [System.Text.RegularExpressions.RegexOptions]::Singleline)
            
            if ($folderMatch.Success) {
                $folderStart = $folderMatch.Groups[1].Value
                $folderContent = $folderMatch.Groups[2].Value
                $folderEnd = $folderMatch.Groups[3].Value
                
                # Add the new project at the beginning of the folder's content
                $projectEntry = "`r`n    <Project Path=`"$solutionProjectPath`" />"
                $newFolderContent = $folderStart + $projectEntry + $folderContent + $folderEnd
                
                # Replace in the solution content
                $solutionContent = $solutionContent.Replace($folderMatch.Value, $newFolderContent)
                
                Set-Content -Path $SolutionPath -Value $solutionContent -NoNewline -Encoding UTF8
            } else {
                Write-Error "Could not parse folder structure in solution file"
                return
            }
        } else {
            # Folder doesn't exist, create it
            # Find the last Folder tag before </Solution>
            $lastFolderMatch = [regex]::Matches($solutionContent, '</Folder>') | Select-Object -Last 1
            
            if ($lastFolderMatch) {
                $insertPosition = $lastFolderMatch.Index + $lastFolderMatch.Length
                
                # Create the new folder with the project
                $newFolder = @"
`r`n  <Folder Name="$folderPath">
    <Project Path="$solutionProjectPath" />
  </Folder>
"@
                
                $solutionContent = $solutionContent.Insert($insertPosition, $newFolder)
                Set-Content -Path $SolutionPath -Value $solutionContent -NoNewline -Encoding UTF8
            } else {
                Write-Error "Could not find insertion point in solution file"
                return
            }
        }
    }
    
    # Step 5: Update parent projects to exclude this directory if needed
    C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders
}

# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function Place-ProjectInCorrectSolutionFolder {
    <#
    .SYNOPSIS
    Moves a project to the correct folder within the solution file
    
    .DESCRIPTION
    Calculates the correct solution folder based on the project name and places it there.
    - Library projects (src/): grouped by first component after "Compze." (e.g., /Compze/, /Compze/Utilities/)
    - Test projects (test/): placed in /_Tests/
    - Sample projects: placed in /_Samples/ subfolders
    - Solution structure projects: placed in /~Solution Structure/
    This is purely a solution file organization update - it doesn't move any files.
    
    .PARAMETER ProjectName
    The name of the project to place in the correct folder
    
    .PARAMETER SolutionPath
    Path to the solution file
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectName,
        
        [Parameter(Mandatory = $true)]
        [string]$SolutionPath
    )
    
    [xml]$xml = Get-Content $SolutionPath
    $solutionDir = Split-Path -Parent $SolutionPath
    
    # Find the project element in the solution file (match by project name in the path)
    $projectElement = $xml.SelectNodes("//Project[@Path]") | Where-Object { 
        $path = $_.Path
        $fileName = ($path -split '[/\\]')[-1]
        $fileName -eq "$ProjectName.csproj"
    } | Select-Object -First 1
    
    if (-not $projectElement) {
        Write-Error "Project not found in solution: $ProjectName"
        return
    }
    
    $currentPath = $projectElement.GetAttribute("Path")
    
    # Determine the target solution folder based on path and name
    $targetSolutionFolder = Get-TargetSolutionFolder -ProjectName $ProjectName -ProjectPath $currentPath
    
    if (-not $targetSolutionFolder) {
        # Project should be at solution root (no folder)
        $currentParent = $projectElement.ParentNode
        if ($currentParent.Name -ne "Solution") {
            $currentParent.RemoveChild($projectElement) | Out-Null
            $xml.DocumentElement.AppendChild($projectElement) | Out-Null
            $xml.Save($SolutionPath)
        }
        return
    }
    
    # Check if already in correct folder
    $currentFolder = $projectElement.ParentNode
    if ($currentFolder.Name -eq "Folder" -and $currentFolder.GetAttribute("Name") -eq $targetSolutionFolder) {
        return
    }
    
    # Remove from current location
    $currentFolder.RemoveChild($projectElement) | Out-Null
    
    # Find or create target folder
    $targetFolder = $xml.SelectSingleNode("//Folder[@Name='$targetSolutionFolder']")
    
    if (-not $targetFolder) {
        $targetFolder = $xml.CreateElement("Folder")
        $targetFolder.SetAttribute("Name", $targetSolutionFolder)
        $xml.DocumentElement.AppendChild($targetFolder) | Out-Null
    }
    
    # Move project to target folder
    $targetFolder.AppendChild($projectElement) | Out-Null
    $xml.Save($SolutionPath)
}

function Get-TargetSolutionFolder {
    <#
    .SYNOPSIS
    Determines the solution folder for a project based on its name and path.
    #>
    param(
        [string]$ProjectName,
        [string]$ProjectPath
    )

    # Test projects go to /_Tests/
    if ($ProjectPath -match '^\.\./test/' -or $ProjectPath -match '^\.\.\\/test\\/' -or $ProjectName -match '^Compze\.Tests\.' -or $ProjectName -match '\.Tests$') {
        return "/_Tests/"
    }

    # Sample projects
    if ($ProjectPath -match 'Samples') {
        if ($ProjectPath -match 'Tests|UnitTests|PerformanceTests') {
            return "/_Samples/AccountManagement/Tests/"
        }
        return "/_Samples/AccountManagement/"
    }

    # Website
    if ($ProjectPath -match 'Websites') {
        return "/_Websites/"
    }

    # Solution structure projects
    if ($ProjectPath -match 'msbuild|SolutionStructure') {
        return "/~Solution Structure/"
    }

    # DevScripts — no folder (solution root)
    if ($ProjectPath -match 'DevScripts') {
        return $null
    }

    # Library projects: group by first component after "Compze."
    $parts = $ProjectName -split '\.'
    if ($parts.Count -le 2) {
        return "/Compze/"
    }

    # Use first two segments as folder: /Compze/Utilities/, /Compze/Tessaging/, etc.
    return "/Compze/$($parts[1])/"
}

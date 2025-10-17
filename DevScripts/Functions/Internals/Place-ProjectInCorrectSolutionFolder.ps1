function Place-ProjectInCorrectSolutionFolder {
    <#
    .SYNOPSIS
    Moves a project to the correct folder within the solution file
    
    .DESCRIPTION
    Calculates the correct solution folder based on the project's path
    and moves the project element to that folder. Creates the folder if needed.
    Assumes the project already exists in the solution file.
    
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
    
    # Calculate the expected project path
    $solutionProjectPath = $ProjectName -replace '\.', '/'
    $solutionProjectPath = "$solutionProjectPath/$ProjectName.csproj"
    
    # Find the project
    $projectElement = $xml.SelectNodes("//Project[@Path]") | Where-Object { $_.Path -eq $solutionProjectPath } | Select-Object -First 1
    
    if (-not $projectElement) {
        Write-Error "Project not found in solution: $ProjectName"
        return
    }
    
    $currentPath = $projectElement.GetAttribute("Path")
    
    # Calculate target folder: "Compze/Common/Configuration/Compze.Common.Configuration.csproj" -> "/Compze/Common/"
    $pathParts = $currentPath -split '/'
    if ($pathParts.Length -lt 2) { 
        Write-Error "Unexpected path format: $currentPath"
        return 
    }
    
    $directoryPath = $pathParts[0..($pathParts.Length - 2)] -join '/'
    $targetFolderParts = $directoryPath -split '/'
    $targetSolutionFolder = if ($targetFolderParts.Length -lt 2) {
        "/" + $targetFolderParts[0] + "/"
    } else {
        "/" + ($targetFolderParts[0..($targetFolderParts.Length - 2)] -join '/') + "/"
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

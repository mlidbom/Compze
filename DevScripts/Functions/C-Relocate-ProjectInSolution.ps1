# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Relocate-ProjectInSolution {
    <#
    .SYNOPSIS
    Updates solution file to place project in correct folder structure
    
    .DESCRIPTION
    Moves a project within the solution folder structure to match its path.
    This is purely a solution file organization update - it doesn't move any files.
    
    .PARAMETER ProjectName
    The name of the project to relocate in the solution
    
    .PARAMETER SolutionPath
    Path to the solution file (defaults to src\Compze.AllProjects.slnx)
    
    .EXAMPLE
    C-Relocate-ProjectInSolution Compze.Common.Configuration
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
    if (-not $SolutionPath.EndsWith('.slnx')) { Write-Error "Only works with .slnx files"; return }
    
    [xml]$xml = Get-Content $SolutionPath
    $projectElement = $xml.SelectNodes("//Project[@Path]") | Where-Object { $_.Path -like "*$ProjectName.csproj" } | Select-Object -First 1
    
    if (-not $projectElement) {
        Write-Error "Project not found in solution"
        return
    }
    
    $currentPath = $projectElement.GetAttribute("Path")
    
    # Calculate target folder: "Compze/Common/Configuration/Compze.Common.Configuration.csproj" -> "/Compze/Common/"
    $pathParts = $currentPath -split '/'
    if ($pathParts.Length -lt 2) { Write-Error "Unexpected path format"; return }
    
    $directoryPath = $pathParts[0..($pathParts.Length - 2)] -join '/'
    $targetFolderParts = $directoryPath -split '/'
    $targetSolutionFolder = if ($targetFolderParts.Length -lt 2) {
        "/" + $targetFolderParts[0] + "/"
    } else {
        "/" + ($targetFolderParts[0..($targetFolderParts.Length - 2)] -join '/') + "/"
    }
    
    $currentFolder = $projectElement.ParentNode
    if ($currentFolder.Name -eq "Folder" -and $currentFolder.GetAttribute("Name") -eq $targetSolutionFolder) {
        return
    }
    
    $currentFolder.RemoveChild($projectElement) | Out-Null
    $targetFolder = $xml.SelectSingleNode("//Folder[@Name='$targetSolutionFolder']")
    
    if (-not $targetFolder) {
        $targetFolder = $xml.CreateElement("Folder")
        $targetFolder.SetAttribute("Name", $targetSolutionFolder)
        $xml.DocumentElement.AppendChild($targetFolder) | Out-Null
    }
    
    $targetFolder.AppendChild($projectElement) | Out-Null
    $xml.Save($SolutionPath)
}

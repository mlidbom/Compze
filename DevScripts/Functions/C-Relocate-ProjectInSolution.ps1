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
    Path to the solution file (defaults to src\Compze.slnx)
    
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
    
    Write-Host "[Step 6/6] Updating solution folder structure..." -ForegroundColor Cyan
    
    [xml]$xml = Get-Content $SolutionPath
    $projectElement = $xml.SelectNodes("//Project[@Path]") | Where-Object { $_.Path -like "*$ProjectName.csproj" } | Select-Object -First 1
    
    if (-not $projectElement) {
        Write-Host "  Project not found in solution" -ForegroundColor Yellow
        return
    }
    
    $currentPath = $projectElement.GetAttribute("Path")
    Write-Host "  Project path: $currentPath" -ForegroundColor Cyan
    
    # Calculate target folder: "Compze/Common/Configuration/Compze.Common.Configuration.csproj" -> "/Compze/Common/"
    $pathParts = $currentPath -split '/'
    if ($pathParts.Length -lt 2) { Write-Host "  Unexpected path format" -ForegroundColor Yellow; return }
    
    $directoryPath = $pathParts[0..($pathParts.Length - 2)] -join '/'
    $targetFolderParts = $directoryPath -split '/'
    $targetSolutionFolder = if ($targetFolderParts.Length -lt 2) {
        "/" + $targetFolderParts[0] + "/"
    } else {
        "/" + ($targetFolderParts[0..($targetFolderParts.Length - 2)] -join '/') + "/"
    }
    
    Write-Host "  Target folder: $targetSolutionFolder" -ForegroundColor Cyan
    
    $currentFolder = $projectElement.ParentNode
    if ($currentFolder.Name -eq "Folder" -and $currentFolder.GetAttribute("Name") -eq $targetSolutionFolder) {
        Write-Host "  ✓ Already in correct folder!" -ForegroundColor Green
        return
    }
    
    $currentFolder.RemoveChild($projectElement) | Out-Null
    $targetFolder = $xml.SelectSingleNode("//Folder[@Name='$targetSolutionFolder']")
    
    if (-not $targetFolder) {
        $targetFolder = $xml.CreateElement("Folder")
        $targetFolder.SetAttribute("Name", $targetSolutionFolder)
        $xml.DocumentElement.AppendChild($targetFolder) | Out-Null
        Write-Host "  Created folder: $targetSolutionFolder" -ForegroundColor Green
    }
    
    $targetFolder.AppendChild($projectElement) | Out-Null
    $xml.Save($SolutionPath)
    Write-Host "  ✓ Moved to: $targetSolutionFolder" -ForegroundColor Green
    Write-Host ""
}

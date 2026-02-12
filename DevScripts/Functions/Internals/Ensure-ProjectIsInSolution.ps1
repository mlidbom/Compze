# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function Ensure-ProjectIsInSolution {
    <#
    .SYNOPSIS
    Ensures a project exists in the solution file
    
    .DESCRIPTION
    Adds a project to the solution file if it doesn't already exist.
    The project will be added to the root Solution element.
    
    .PARAMETER ProjectName
    The name of the project to ensure exists in the solution
    
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
    
    # Find the actual project file on disk to determine the correct path
    $projectFile = Get-CsprojFiles -Path (Split-Path -Parent (Split-Path -Parent $SolutionPath)) -Filter "$ProjectName.csproj" | Select-Object -First 1
    
    if ($projectFile) {
        # Use the actual location on disk
        $solutionProjectPath = [System.IO.Path]::GetRelativePath($solutionDir, $projectFile.FullName) -replace '\\', '/'
    } else {
        # Project doesn't exist yet — use flat convention: ProjectName/ProjectName.csproj
        $solutionProjectPath = "$ProjectName/$ProjectName.csproj"
    }
    
    # Try to find the project
    $projectElement = $xml.SelectNodes("//Project[@Path]") | Where-Object { $_.Path -eq $solutionProjectPath } | Select-Object -First 1
    
    # If project doesn't exist, create it
    if (-not $projectElement) {
        $projectElement = $xml.CreateElement("Project")
        $projectElement.SetAttribute("Path", $solutionProjectPath)
        $xml.DocumentElement.AppendChild($projectElement) | Out-Null
        $xml.Save($SolutionPath)
    }
}

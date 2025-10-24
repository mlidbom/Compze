# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Get-ProjectReferences {
    <#
    .SYNOPSIS
    Gets all ProjectReference elements from a .csproj file
    
    .DESCRIPTION
    Parses a .csproj file and extracts all ProjectReference Include paths
    
    .PARAMETER CsprojPath
    Path to the .csproj file
    
    .RETURNS
    Array of ProjectReference Include attribute values
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$CsprojPath
    )
    
    if (-not (Test-Path $CsprojPath)) {
        Write-Error "Project file not found: $CsprojPath"
        return @()
    }
    
    # Load XML content as string to avoid file locking issues
    $xmlContent = Get-Content $CsprojPath -Raw
    [xml]$xml = $xmlContent
    $projectReferences = $xml.SelectNodes("//ProjectReference[@Include]")
    
    $results = if ($projectReferences) {
        $projectReferences | ForEach-Object { $_.GetAttribute("Include") }
    } else {
        @()
    }
    
    # Explicitly clear the XML object to release any file handles
    $xml = $null
    
    return $results
}

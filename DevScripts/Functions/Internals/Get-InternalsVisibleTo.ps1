# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Get-InternalsVisibleTo {
    <#
    .SYNOPSIS
    Gets all InternalsVisibleTo entries from a .csproj file
    
    .DESCRIPTION
    Parses a .csproj file and extracts all InternalsVisibleTo Include values
    
    .PARAMETER CsprojPath
    Path to the .csproj file
    
    .RETURNS
    Array of assembly names that have InternalsVisibleTo access
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
    $internalsVisibleTo = $xml.SelectNodes("//InternalsVisibleTo[@Include]")
    
    $results = if ($internalsVisibleTo) {
        $internalsVisibleTo | ForEach-Object { $_.GetAttribute("Include") }
    } else {
        @()
    }
    
    # Explicitly clear the XML object to release any file handles
    $xml = $null
    
    return $results
}

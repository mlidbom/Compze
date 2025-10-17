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
    
    [xml]$xml = Get-Content $CsprojPath
    $internalsVisibleTo = $xml.SelectNodes("//InternalsVisibleTo[@Include]")
    
    if ($internalsVisibleTo) {
        return $internalsVisibleTo | ForEach-Object { $_.GetAttribute("Include") }
    }
    
    return @()
}
